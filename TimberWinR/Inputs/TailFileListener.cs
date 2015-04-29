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
        private long _errorCount;      
        private CodecArguments _codecArguments;
        private ICodec _codec;

        public bool Stop { get; set; }

        public TailFileListener(TimberWinR.Parser.TailFileArguments arguments,
            CancellationToken cancelToken)
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
            LogManager.GetCurrentClassLogger()
                .Info("{0}: Shutting Down {1} for {2}", Thread.CurrentThread.ManagedThreadId, InputType,
                    _arguments.Location);
            Stop = true;
            base.Shutdown();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("taillog",
                    new JObject(
                        new JProperty("messages", _receivedMessages),
                        new JProperty("errors", _errorCount),
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
            const int bufSize = 16535;
            long prevLen = offset;

            FileInfo fi = new FileInfo(fileName);
            if (!fi.Exists)
                return;

            LogManager.GetCurrentClassLogger().Trace(":{0} Tailing File: {1} as Pos: {2}", Thread.CurrentThread.ManagedThreadId, fileName, prevLen);

            using (var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                stream.Seek(prevLen, SeekOrigin.Begin);

                char[] buffer = new char[bufSize];
                StringBuilder current = new StringBuilder();
                using (StreamReader sr = new StreamReader(stream))
                {
                    int nRead;
                    do
                    {
                        // Read a buffered amount
                        nRead = sr.ReadBlock(buffer, 0, bufSize);
                        for (int i = 0; i < nRead; ++i)
                        {
                            // We need the terminator!
                            if (buffer[i] == '\n' || buffer[i] == '\r')
                            {
                                if (current.Length > 0)
                                {
                                    string line = string.Concat(dbe.Previous, current);
                                    var json = new JObject();

                                    if (json["logSource"] == null)
                                    {
                                        if (string.IsNullOrEmpty(_arguments.LogSource))
                                            json.Add(new JProperty("logSource", fileName));
                                        else
                                            json.Add(new JProperty("logSource", _arguments.LogSource));
                                    }

                                    //LogManager.GetCurrentClassLogger().Debug(":{0} File: {1}:{2}  {3}", Thread.CurrentThread.ManagedThreadId, fileName, dbe.LinesProcessed, line);

                                    // We've processed the partial input
                                    dbe.Previous = "";
                                    json["Text"] = line;
                                    json["Index"] = dbe.LinesProcessed;
                                    json["LogFileName"] = fileName;
                                    if (_codecArguments != null && _codecArguments.Type == CodecArguments.CodecType.multiline)
                                    {
                                        try
                                        {                                         
                                            _codec.Apply(line, this);
                                            Interlocked.Increment(ref _receivedMessages);
                                            dbe.IncrementLineCount();
                                        }
                                        catch (Exception ex)
                                        {
                                            Interlocked.Increment(ref _errorCount);
                                            LogManager.GetCurrentClassLogger().ErrorException("Filter Error", ex);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            ProcessJson(json);
                                            dbe.IncrementLineCount();                                                                                      
                                            Interlocked.Increment(ref _receivedMessages);
                                            LogsFileDatabase.Update(dbe, true, sr.BaseStream.Position);
                                        }
                                        catch (Exception ex)
                                        {
                                            Interlocked.Increment(ref _errorCount);
                                            LogManager.GetCurrentClassLogger().ErrorException("Process Error", ex);
                                        }
                                    }

                                }
                                current = new StringBuilder();
                            }
                            else // Copy character into the buffer
                            {
                                current.Append(buffer[i]);
                            }
                        }
                    } while (nRead > 0);

                    // We didn't encounter the newline, so save it.
                    if (current.Length > 0)
                    {
                        dbe.Previous = current.ToString();
                    }
                }
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

                                    Task.Factory.StartNew(() => TailFileWatcher(fileName));
                                }
                                else if (!isWildcardPattern)
                                {
                                    FileInfo fi = new FileInfo(dbe.FileName);
                                    SaveVisitedFileName(fileName);

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
                                    bool processWholeFile = logHasRolled || !dbe.ProcessedFile || dbe.NewFile;
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

