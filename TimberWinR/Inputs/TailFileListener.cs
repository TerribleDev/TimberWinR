using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Interop.MSUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using NLog;
using NLog.LayoutRenderers;
using TimberWinR.Codecs;
using TimberWinR.Parser;

namespace TimberWinR.Inputs
{
    /// <summary>
    /// Tail a file.
    /// </summary>
    public class TailFileListener : InputListener
    {
        private object _locker = new object();
        private int _pollingIntervalInSeconds;
        private TimberWinR.Parser.TailFileArguments _arguments;
        private long _receivedMessages;

        private CodecArguments _codecArguments;
        private ICodec _codec;
       
        public bool Stop { get; set; }

        public TailFileListener(TimberWinR.Parser.TailFileArguments arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-TailLog")
        {
            Stop = false;
         
            EnsureRollingCaught();    

            _codecArguments = arguments.CodecArguments;
            if (_codecArguments != null && _codecArguments.Type == CodecArguments.CodecType.multiline)
                _codec = new Multiline(_codecArguments);

            _receivedMessages = 0;
            _arguments = arguments;
            _pollingIntervalInSeconds = arguments.Interval;

            foreach (string srcFile in _arguments.Location.Split(','))
            {
                string file = srcFile.Trim();
                Task.Factory.StartNew(() => TailFileWatcher(file));
            }
        }

        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("{0}: Shutting Down {1} for {2}", Thread.CurrentThread.ManagedThreadId, InputType, _arguments.Location);
            Stop = true;
            base.Shutdown();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("taillog",
                    new JObject(
                        new JProperty("messages", _receivedMessages),
                        new JProperty("type", InputType),
                        new JProperty("location", _arguments.Location),
                        new JProperty("logSource", _arguments.LogSource),
                        new JProperty("recurse", _arguments.Recurse),
                        new JProperty("files",
                            new JArray(from f in Files
                                       select new JValue(f))),
                        new JProperty("filedb",
                            new JArray(from f in Files
                                       select JObject.FromObject(LogsFileDatabase.LookupLogFile(f))))
                        )));


            if (_codecArguments != null)
            {
                var cp = new JProperty("codec",
                    new JArray(
                        new JObject(
                            new JProperty("type", _codecArguments.Type.ToString()),
                            new JProperty("what", _codecArguments.What.ToString()),
                            new JProperty("negate", _codecArguments.Negate),
                            new JProperty("multilineTag", _codecArguments.MultilineTag),
                            new JProperty("pattern", _codecArguments.Pattern))));
                json.Add(cp);
            }


            return json;
        }

        private void TailFileContents(string fileName, long offset, LogsFileDatabaseEntry dbe)
        {
            using (StreamReader reader = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                //start at the end of the file
                long lastMaxOffset = offset;

                //if the file size has not changed, idle
                if (reader.BaseStream.Length == lastMaxOffset)
                    return;

                //seek to the last max offset
                LogManager.GetCurrentClassLogger().Trace("{0}: File: {1} Seek to: {2}", Thread.CurrentThread.ManagedThreadId, fileName, lastMaxOffset);

                reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                //read out of the file until the EOF
                string line = "";
                long lineOffset = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    long index = lastMaxOffset + lineOffset;
                    string text = line;
                    string logFileName = fileName;
                    var json = new JObject();

                    if (json["logSource"] == null)
                    {
                        if (string.IsNullOrEmpty(_arguments.LogSource))
                            json.Add(new JProperty("logSource", fileName));
                        else
                            json.Add(new JProperty("logSource", _arguments.LogSource));
                    }

                    json["Text"] = line;
                    json["Index"] = index;
                    json["LogFileName"] = fileName;


                    if (_codecArguments != null && _codecArguments.Type == CodecArguments.CodecType.multiline)
                    {
                        _codec.Apply(line, this);
                        Interlocked.Increment(ref _receivedMessages);
                        dbe.IncrementLineCount();
                    }
                    else
                    {
                        ProcessJson(json);
                        Interlocked.Increment(ref _receivedMessages);
                        dbe.IncrementLineCount();
                        //LogManager.GetCurrentClassLogger().Info("{0}: File: {1} {2} {3}", Thread.CurrentThread.ManagedThreadId, fileName, dbe.LinesProcessed, line);
                    }                 

                    lineOffset += line.Length;
                }
                //update the last max offset
                lastMaxOffset = reader.BaseStream.Position;
                LogsFileDatabase.Update(dbe, true, lastMaxOffset);               
            }
        }
        // One thread for each kind of file to watch, i.e. "*.log,*.txt" would be two separate
        // threads.
        private void TailFileWatcher(string fileToWatch)
        {
            Dictionary<string, string> _fnfmap = new Dictionary<string, string>();

            using (var syncHandle = new ManualResetEventSlim())
            {
                // Execute the query
                while (!Stop && !CancelToken.IsCancellationRequested)
                {
                    try
                    {
                        if (!CancelToken.IsCancellationRequested)
                        {
                            var isWildcardPattern = fileToWatch.Contains('*');
                            string path = Path.GetDirectoryName(fileToWatch);
                            string name = Path.GetFileName(fileToWatch);
                            if (string.IsNullOrEmpty(path))
                                path = ".";

                            LogManager.GetCurrentClassLogger().Trace(":{0} Tailing File: {1}", Thread.CurrentThread.ManagedThreadId, Path.Combine(path, name));

                            // Ok, we have a potential file filter here as 'fileToWatch' could be foo.log or *.log

                            SearchOption so = SearchOption.TopDirectoryOnly;
                            if (_arguments.Recurse == -1)
                                so = SearchOption.AllDirectories;

                            foreach (string fileName in Directory.GetFiles(path, name, so))
                            {
                                var dbe = LogsFileDatabase.LookupLogFile(fileName);

                                // We only spin up 1 thread for a file we haven't yet seen.                               
                                if (isWildcardPattern && !HaveSeenFile(fileName) && dbe.NewFile)
                                {
                                    LogManager.GetCurrentClassLogger().Debug(":{0} Starting Thread Tailing File: {1}", Thread.CurrentThread.ManagedThreadId, dbe.FileName);
                                    LogsFileDatabase.Update(dbe, false, dbe.LastPosition);
                                    SaveVisitedFileName(fileName);
                                    Task.Factory.StartNew(() => TailFileWatcher(fileName));
                                }
                                else if (!isWildcardPattern)
                                {
                                    FileInfo fi = new FileInfo(dbe.FileName);

                                    //LogManager.GetCurrentClassLogger().Info("Located File: {0}, New: {1}", dbe.FileName, dbe.NewFile);                                
                                    long length = fi.Length;
                                    bool logHasRolled = false;
                                    if (fi.Length < dbe.LastPosition || fi.CreationTimeUtc != dbe.CreationTimeUtc)
                                    {
                                        LogManager.GetCurrentClassLogger().Info("{0}: Log has Rolled: {1}", Thread.CurrentThread.ManagedThreadId, dbe.FileName);
                                        logHasRolled = true;
                                        LogsFileDatabase.Roll(dbe);
                                    }
                                    // Log has rolled or this is a file we are seeing for the first time.
                                    bool processWholeFile = logHasRolled || !dbe.ProcessedFile;
                                    if (processWholeFile)
                                    {
                                        LogsFileDatabase.Update(dbe, true, 0);
                                        LogManager.GetCurrentClassLogger().Debug("{0}: Process Whole File: {1}", Thread.CurrentThread.ManagedThreadId, dbe.FileName);
                                        TailFileContents(dbe.FileName, 0, dbe);
                                    }
                                    else
                                    {
                                        TailFileContents(dbe.FileName, dbe.LastPosition, dbe);
                                    }                                  
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException fnfex)
                    {
                        string fn = fnfex.FileName;

                        if (!_fnfmap.ContainsKey(fn))
                            LogManager.GetCurrentClassLogger().Warn(fnfex.Message);
                        _fnfmap[fn] = fn;
                    }
                    catch (IOException ioex)
                    {
                        LogManager.GetCurrentClassLogger().Debug("Log has rolled: {0}", ioex.Message);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Error(ex);
                    }
                    finally
                    {
                        try
                        {
                            if (!Stop)
                                syncHandle.Wait(TimeSpan.FromSeconds(_pollingIntervalInSeconds), CancelToken);
                        }
                        catch (OperationCanceledException)
                        {
                            Stop = true;
                        }
                        catch (Exception ex1)
                        {
                            LogManager.GetCurrentClassLogger().Warn(ex1);
                        }
                    }
                }
            }
            Finished();
        }       
    }
}

