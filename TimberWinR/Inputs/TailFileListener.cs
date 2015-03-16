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
        private int _pollingIntervalInSeconds;
        private TimberWinR.Parser.TailFile _arguments;
        private long _receivedMessages;
        private Dictionary<string, Int64> _logFileMaxRecords;
        private Dictionary<string, DateTime> _logFileCreationTimes;
        private Dictionary<string, DateTime> _logFileSampleTimes;
        private Dictionary<string, long> _logFileSizes;       
        private CodecArguments _codecArguments;
        private ICodec _codec;     

        public bool Stop { get; set; }

        public TailFileListener(TimberWinR.Parser.TailFile arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-TailLog")
        {
            Stop = false;

            _codecArguments = arguments.CodecArguments;
            if (_codecArguments != null && _codecArguments.Type == CodecArguments.CodecType.multiline)
                _codec = new Multiline(_codecArguments);


            _logFileMaxRecords = new Dictionary<string, Int64>();
            _logFileCreationTimes = new Dictionary<string, DateTime>();
            _logFileSampleTimes = new Dictionary<string, DateTime>();
            _logFileSizes = new Dictionary<string, long>();

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
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);
            Stop = true;
            base.Shutdown();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("log",
                    new JObject(
                        new JProperty("messages", _receivedMessages),
                        new JProperty("type", InputType),
                        new JProperty("location", _arguments.Location),
                        new JProperty("logSource", _arguments.LogSource),
                        new JProperty("recurse", _arguments.Recurse),

                        new JProperty("files",
                            new JArray(from f in _logFileMaxRecords.Keys
                                       select new JValue(f))),
                        new JProperty("fileSampleTimes",
                            new JArray(from f in _logFileSampleTimes.Values
                                       select new JValue(f))),
                        new JProperty("fileSizes",
                            new JArray(from f in _logFileSizes.Values
                                       select new JValue(f))),
                        new JProperty("fileIndices",
                            new JArray(from f in _logFileMaxRecords.Values
                                       select new JValue(f))),
                        new JProperty("fileCreationDates",
                            new JArray(from f in _logFileCreationTimes.Values
                                       select new JValue(f)))
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

        private void TailFileContents(string fileName, long offset)
        {
            using (StreamReader reader = new StreamReader(new FileStream(fileName,
                     FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                //start at the end of the file
                long lastMaxOffset = offset;

                //if the file size has not changed, idle
                if (reader.BaseStream.Length == lastMaxOffset)
                    return;

                //seek to the last max offset
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
                    }
                    else
                    {
                        ProcessJson(json);
                        Interlocked.Increment(ref _receivedMessages);
                    }
                    lineOffset += line.Length;                   
                }
                //update the last max offset
                lastMaxOffset = reader.BaseStream.Position;
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
                            string path = Path.GetDirectoryName(fileToWatch);
                            string name = Path.GetFileName(fileToWatch);
                            if (string.IsNullOrEmpty(path))
                                path = ".";

                            // Ok, we have a potential file filter here as 'fileToWatch' could be foo.log or *.log

                            SearchOption so = SearchOption.TopDirectoryOnly;
                            if (_arguments.Recurse == -1)
                                so = SearchOption.AllDirectories;

                            foreach (string fileName in Directory.GetFiles(path, name, so))
                            {
                                var dbe = LogsFileDatabase.LookupLogFile(fileName);
                                FileInfo fi = new FileInfo(dbe.FileName);
                                //LogManager.GetCurrentClassLogger().Info("Located File: {0}, New: {1}", dbe.FileName, dbe.NewFile);
                                long length = fi.Length;
                                bool logHasRolled = false;
                                if (fi.Length < dbe.Size || fi.CreationTimeUtc != dbe.CreationTimeUtc)
                                {
                                    LogManager.GetCurrentClassLogger().Info("Log has Rolled: {0}", dbe.FileName);
                                    logHasRolled = true;
                                }
                                bool processWholeFile = logHasRolled || dbe.NewFile;
                                if (processWholeFile)
                                {
                                    LogManager.GetCurrentClassLogger().Info("Process Whole File: {0}", dbe.FileName);
                                    TailFileContents(dbe.FileName, 0);
                                }
                                else
                                {
                                    TailFileContents(dbe.FileName, dbe.Size);
                                }
                                LogsFileDatabase.Update(dbe);
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

