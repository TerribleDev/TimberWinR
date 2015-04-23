using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.CodeDom.Compiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;
using NLog.Config;
using NLog.Targets;
using ServiceStack.Text.Jsv;
using TimberWinR.Parser;


namespace TimberWinR.TestGenerator
{
    public class Program
    {
        private static List<Task> _tasks = new List<Task>();
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static Manager _timberWinR;

        public static Diagnostics.Diagnostics Diagnostics { get; set; }

        private static PerformanceCounter cpuCounter = new PerformanceCounter();
        private static PerformanceCounter ramCounter = new PerformanceCounter();
        private static Task _monitorTask;

        private static int _totalMessagesToSend;
        private static int _cpuSampleCount;
        private static double _avgCpuUsage;
        private static double _totalCpuUsage;
        private static double _maxCpuUsage;

        private static int _memSampleCount;
        private static double _avgMemUsage;
        private static double _totalMemUsage;
        private static double _maxMemUsage;
         
        private static CommandLineOptions Options;

        static int Main(string[] args)
        {
            _totalMessagesToSend = 0;

            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            ramCounter.CategoryName = "Memory";
            ramCounter.CounterName = "% Committed Bytes In Use";

            Options = new CommandLineOptions();

            if (CommandLine.Parser.Default.ParseArguments(args, Options))
            {
                var testFile = Options.TestFile;
                if (!string.IsNullOrEmpty(testFile))
                {
                    if (!File.Exists(Options.TestFile))
                        throw new Exception(string.Format("No such test file: {0} found", Options.TestFile));

                    var fargs = ParseTestArguments(testFile, ref Options);
                    if (!CommandLine.Parser.Default.ParseArguments(fargs, Options))
                        return 2;
                }

                SetupTestDirectory(Options);

                var swOverall = Stopwatch.StartNew();
                swOverall.Start();

                InitializeLogging(Options.LogLevel);

                LogManager.GetCurrentClassLogger().Info("Starting CPU Usage: {0}, RAM Usage: {1}", getCurrentCpuUsage(), getAvailableRAM());

                // Reset the tests.
                ResetTests(Options);

                var sw = Stopwatch.StartNew();

                // Startup TimberWinR
                if (Options.StartTimberWinR)
                    StartTimberWinR(Options.TimberWinRConfigFile, Options.LogLevel, ".", false);

                // Run the Generators
                var arrayOfTasks = RunGenerators(Options);

                // Wait for all Generators to finish
                try
                {
                    Task.WaitAll(arrayOfTasks);
                }
                catch (AggregateException aex)
                {
                    LogManager.GetCurrentClassLogger().Error(aex);
                }


                LogManager.GetCurrentClassLogger().Info("Generation Finished: " + sw.Elapsed);
                sw.Reset();
                sw.Start();

                // All generators are finished, wait till senders are done.
                WaitForOutputTransmission();

                LogManager.GetCurrentClassLogger().Info("Finished Transmission: " + sw.Elapsed);
                sw.Reset();
                sw.Start();

                // Get all the stats
                JObject jsonTimberWinr;

                if (Options.StartTimberWinR)
                    jsonTimberWinr = ShutdownTimberWinR();
                else
                {
                    jsonTimberWinr = GetDiagnosticsOutput();
                    if (jsonTimberWinr == null)
                        return 3;
                }

                LogManager.GetCurrentClassLogger().Info("Finished Shutdown: " + sw.Elapsed);
                sw.Stop();

                swOverall.Stop();
                LogManager.GetCurrentClassLogger().Info("Total Elapsed Time: {0}", swOverall.Elapsed);

                int results = VerifyResults(Options, jsonTimberWinr);

                Console.ReadKey();
                return results;
            }

            return 1;
        }

        public static string GET(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);

                string data = reader.ReadToEnd();

                reader.Close();
                stream.Close();

                return data;
            }
            catch (Exception e)
            {
                LogManager.GetCurrentClassLogger().ErrorException("Error in GET", e);
            }

            return null;
        }

        private static void CopySourceFile(string fileName, string outputDir)
        {
            FileInfo fi = new FileInfo(fileName);
            if (fi.Exists)
                File.Copy(fileName, Path.Combine(outputDir, fi.Name));
        }

        private static void SetupTestDirectory(CommandLineOptions options)
        {
            if (options.TestDir != "." && Directory.Exists(options.TestDir))
                Directory.Delete(options.TestDir, true);

            if (!Directory.Exists(options.TestDir))
                Directory.CreateDirectory(options.TestDir);

            CopySourceFile(options.TestFile, options.TestDir);
            CopySourceFile(options.TimberWinRConfigFile, options.TestDir);
            CopySourceFile(options.ExpectedResultsFile, options.TestDir);

            Directory.SetCurrentDirectory(options.TestDir);
        }

        private static string[] ParseTestArguments(string testFile, ref CommandLineOptions options)
        {
            options = new CommandLineOptions();
            JObject jtest = JObject.Parse(File.ReadAllText(testFile));
            IList<JToken> inputs = jtest["arguments"].Children().ToList();
            List<string> testargs = new List<string>();
            foreach (JProperty it in inputs)
            {
                testargs.Add(it.Name);

                var cc = it.Value.Children().Count();
                if (cc > 0)
                {
                    for (int i = 0; i < cc; i++)
                    {
                        testargs.Add(it.Value[i].ToString());
                    }
                }
                else
                {
                    testargs.Add(it.Value.ToString());
                }
            }
            var fargs = testargs.ToArray();
            return fargs;
        }

        private static int VerifyResults(CommandLineOptions options, JObject json)
        {
            var jresult = JObject.Parse(File.ReadAllText(options.ExpectedResultsFile));

            json["maxCpuUsage"] = _maxCpuUsage;
            json["avgCpuUsage"] = _avgCpuUsage;

            json["maxMemUsage"] = _maxMemUsage;
            json["avgMemUsage"] = _avgMemUsage;

            // TailLogs

            IList<JToken> inputs = json["timberwinr"]["inputs"].Children().ToList();
            foreach (JToken t in inputs)
            {
                JProperty inputProp = t.First as JProperty;
                switch (inputProp.Name)
                {
                    case "udp":
                        if (VerifyConditions(json, new string[] { "udp" }, inputProp, jresult) != 0)
                            return 1;
                        break;
                    case "tcp":
                        if (VerifyConditions(json, new string[] { "tcp" }, inputProp, jresult) != 0)
                            return 1;
                        break;
                    case "log":
                    case "taillog":
                        if (VerifyConditions(json, new string[] { "log", "taillog" }, inputProp, jresult) != 0)
                            return 1;
                        break;
                }
            }

            return 0;
        }

        private static int VerifyConditions(JObject json, string[] logTypes, JProperty inputProp, JObject jresult)
        {
            var ttail = inputProp.Value as JObject;
            foreach (var resultInput in jresult["Results"]["Inputs"].Children().ToList())
            {
                JProperty rinputProp = resultInput.First as JProperty;
                if (logTypes.Contains(rinputProp.Name))
                {
                    foreach (JProperty testProp in rinputProp.Value)
                    {
                        try
                        {
                            var cond1 = testProp.Value.ToString();
                            IList<string> tkeys = ttail.Properties().Select(pn => pn.Name).ToList();
                            foreach (string tkey in tkeys)
                                cond1 = cond1.Replace(string.Format("[{0}]", tkey), string.Format("{0}", ttail[tkey].ToString()));

                            // Add builtins
                            cond1 = cond1.Replace("[avgCpuUsage]", json["avgCpuUsage"].ToString());
                            cond1 = cond1.Replace("[maxCpuUsage]", json["maxCpuUsage"].ToString());
                            cond1 = cond1.Replace("[avgMemUsage]", json["avgMemUsage"].ToString());
                            cond1 = cond1.Replace("[maxMemUsage]", json["maxMemUsage"].ToString());

                            var p1 = Expression.Parameter(typeof(JObject), "json");
                            var e1 = System.Linq.Dynamic.DynamicExpression.ParseLambda(new[] { p1 },
                                typeof(bool), cond1);
                            bool r1 = (bool)e1.Compile().DynamicInvoke(ttail);
                            if (!r1)
                            {
                                LogManager.GetCurrentClassLogger().Error("Test Failed: '{0}: ({1})'", testProp.Name, cond1);
                                return 1;

                            }
                            else
                            {
                                LogManager.GetCurrentClassLogger()
                                    .Info("PASSED({0}): '{1}: ({2})'", inputProp.Name, testProp.Name, cond1);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger()
                                .Error("Error parsing expression '{0}': {1}", testProp.Value.ToString(),
                                    ex.Message);
                            return 2;
                        }
                    }
                }
            }
            return 0;
        }


        private static JObject GetDiagnosticsOutput()
        {
            if (Diagnostics != null)
                return Diagnostics.DiagnosticsOutput();
            else
            {
                var jsonDiag = GET("http://localhost:5141");
                if (jsonDiag == null)
                {
                    LogManager.GetCurrentClassLogger().Error("TimberWinR diagnostics port not responding.");
                    return null;
                }
                return JObject.Parse(jsonDiag);
            }        
        }

        // Wait till all output has been transmitted.
        private static void WaitForOutputTransmission()
        {
            bool completed = false;
            do
            {
                var json = GetDiagnosticsOutput();
                if (json == null)
                    return;

                //Console.WriteLine(json.ToString(Formatting.Indented));

                IList<JToken> inputs = json["timberwinr"]["inputs"].Children().ToList();
                foreach (var so in inputs.Children())
                {
                    var token = so.First;
                    var messages = token["messages"].Value<int>();
                    //  Console.WriteLine("{0} messages", messages);
                }


                IList<JToken> outputs = json["timberwinr"]["outputs"].Children().ToList();
                foreach (var so in outputs.Children())
                {
                    var outputToken = so.First;

                    var mbc = outputToken["queuedMessageCount"].Value<int>();
                    var smc = outputToken["sentMessageCount"].Value<int>();

                    // LogManager.GetCurrentClassLogger().Info("Queued: {0}, Sent: {1}", mbc, smc);

                    completed = mbc == 0 && smc >= _totalMessagesToSend;
                }
                Thread.Sleep(250);
            } while (!completed);
        }

        private static void sampleUsages()
        {
            getCurrentCpuUsage();
            getAvailableRAM();
        }

        private static string getCurrentCpuUsage()
        {
            _cpuSampleCount++;
            var v = cpuCounter.NextValue();
            if (v > _maxCpuUsage)
                _maxCpuUsage = v;

            _totalCpuUsage += v;
            _avgCpuUsage = _totalCpuUsage / _cpuSampleCount;

            return v + "%";
        }

        private static string getAvailableRAM()
        {
            _memSampleCount++;
            var v = ramCounter.NextValue();
            if (v > _maxMemUsage)
                _maxMemUsage = v;

            _totalMemUsage += v;
            _avgMemUsage = _totalMemUsage / _memSampleCount;
            return v + "MB";
        }

        private static JObject ShutdownTimberWinR()
        {
            if (_timberWinR != null)
            {
                // Cancel any/all other threads
                _cancellationTokenSource.Cancel();

                _timberWinR.Shutdown();

                var json = Diagnostics.DiagnosticsOutput();

                LogManager.GetCurrentClassLogger()
                    .Info("Average CPU Usage: {0}%, Average RAM Usage: {1}MB, Max CPU: {2}%, Max Mem: {3}MB",
                        _avgCpuUsage, _avgMemUsage, _maxCpuUsage, _maxMemUsage);

                LogManager.GetCurrentClassLogger().Info(json.ToString());

                Diagnostics.Shutdown();

                return json;
            }

            return new JObject();
        }

        static void StartTimberWinR(string configFile, string logLevel, string logFileDir, bool enableLiveMonitor)
        {
            _timberWinR = new TimberWinR.Manager(configFile, logLevel, logFileDir, enableLiveMonitor, _cancellationTokenSource.Token, false);
            _timberWinR.OnConfigurationProcessed += TimberWinROnOnConfigurationProcessed;
            _timberWinR.Start(_cancellationTokenSource.Token);
            Diagnostics = new Diagnostics.Diagnostics(_timberWinR, _cancellationTokenSource.Token, 5141);
        }

        private static void TimberWinROnOnConfigurationProcessed(Configuration configuration)
        {
            if (!string.IsNullOrEmpty(Options.RedisHost) && configuration.RedisOutputs != null && configuration.RedisOutputs.Count() > 0)
            {
                foreach (var ro in configuration.RedisOutputs)
                {
                    ro.Host = new string[] { Options.RedisHost };
                }
            }

        }

        static void InitializeLogging(string logLevel)
        {
            var loggingConfiguration = new LoggingConfiguration();

            // Create our default targets
            var coloredConsoleTarget = new ColoredConsoleTarget();

            var logFileDir = ".";

            Target fileTarget = CreateDefaultFileTarget(logFileDir);

            loggingConfiguration.AddTarget("Console", coloredConsoleTarget);
            loggingConfiguration.AddTarget("DailyFile", fileTarget);

            // The LogLevel.Trace means has to be at least Trace to show up on console
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, coloredConsoleTarget));
            // LogLevel.Debug means has to be at least Debug to show up in logfile
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));

            LogManager.Configuration = loggingConfiguration;
            LogManager.EnableLogging();

            LogManager.GlobalThreshold = LogLevel.FromString(logLevel);
        }

        static FileTarget CreateDefaultFileTarget(string logPath)
        {
            return new FileTarget
            {
                ArchiveEvery = FileArchivePeriod.None,
                ArchiveAboveSize = 5 * 1024 * 1024,
                MaxArchiveFiles = 5,
                BufferSize = 10,
                FileName = Path.Combine(logPath, "TimberWinR.TestGenerator", "TimberWinRTestGen.log"),
                ArchiveFileName = Path.Combine(logPath, "TimberWinR-TestGenerator_log-{#######}.log"),
            };
        }

        static void ResetTests(CommandLineOptions options)
        {
            if (File.Exists(".timberwinrdb"))
                File.Delete(".timberwinrdb");

            if (File.Exists("TimberWinR.TestGenerator\\TimberWinRTestGen.log"))
                File.Delete("TimberWinR.TestGenerator\\TimberWinRTestGen.log");

            if (File.Exists("TimberWinR\\TimberWinR.log"))
                File.Delete("TimberWinR\\TimberWinR.log");

            if (options.JsonLogFiles.Length > 0)
            {
                foreach (var logFile in options.JsonLogFiles)
                {
                    if (File.Exists(logFile))
                        File.Delete(logFile);
                }
            }

            if (options.JsonRollingLogFiles.Length > 0)
            {
                foreach (var logFile in options.JsonRollingLogFiles)
                {
                    if (File.Exists(logFile))
                        File.Delete(logFile);
                }
            }
        }

        static Task[] RunGenerators(CommandLineOptions options)
        {
            _monitorTask = Task.Factory.StartNew(() =>
                {
                    using (var syncHandle = new ManualResetEventSlim())
                    {
                        try
                        {
                            // Execute the query
                            while (!_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                sampleUsages();
                                // LogManager.GetCurrentClassLogger().Info("Starting CPU Usage: {0}, RAM Usage: {1}", getCurrentCpuUsage(), getAvailableRAM());
                                syncHandle.Wait(TimeSpan.FromMilliseconds(options.JsonRate), _cancellationTokenSource.Token);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex);
                        }
                    }
                }, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            StartJson(options);
            StartJsonRolling(options);
            StartUdp(options);
            StartTcp(options);

            return _tasks.ToArray();
        }

        static void StartJson(CommandLineOptions options)
        {
            if (options.JsonLogFiles.Length > 0)
            {
                foreach (var logFile in options.JsonLogFiles)
                {
                    _totalMessagesToSend += options.NumMessages;

                    if (options.Verbose)
                        LogManager.GetCurrentClassLogger()
                            .Info("Starting LogFile Generator for {0}",
                                Path.GetFullPath(Path.Combine(options.JsonLogDir, logFile)));
                    _tasks.Add(Task.Factory.StartNew(() =>
                    {
                        var p = new JsonLogFileTestParameters()
                        {
                            NumMessages = options.NumMessages,
                            LogFileDir = options.JsonLogDir,
                            LogFileName = logFile,
                            SleepTimeMilliseconds = options.JsonRate
                        };
                        JsonLogFileGenerator.Generate(p);
                        Thread.Sleep(250);
                    }));

                }
            }
        }

        private static void StartJsonRolling(CommandLineOptions options)
        {
            if (options.JsonRollingLogFiles.Length > 0)
            {
                foreach (var logFile in options.JsonRollingLogFiles)
                {
                    _totalMessagesToSend += options.NumMessages;

                    if (options.Verbose)
                        LogManager.GetCurrentClassLogger()
                            .Info("Starting RollingLogFile Generator for {0}",
                                Path.GetFullPath(Path.Combine(options.JsonLogDir, logFile)));
                    _tasks.Add(Task.Factory.StartNew(() =>
                    {
                        var p = new JsonLogFileTestParameters()
                        {
                            NumMessages = options.NumMessages,
                            LogFileDir = options.JsonLogDir,
                            LogFileName = logFile,
                            SleepTimeMilliseconds = options.JsonRate
                        };
                        JsonRollingLogFileGenerator.Generate(p);
                        Thread.Sleep(250);
                    }));

                }
            }
        }

        static void StartUdp(CommandLineOptions options)
        {
            if (options.Udp > 0)
            {
                if (options.Verbose)
                    LogManager.GetCurrentClassLogger()
                        .Info("Starting UDP Generator for {0}:{1}", options.UdpHost, options.Udp);

                _tasks.Add(Task.Factory.StartNew(() =>
                {
                    var p = new UdpTestParameters()
                    {
                        Port = options.Udp,
                        Host = options.UdpHost,
                        NumMessages = options.NumMessages,
                        SleepTimeMilliseconds = options.UdpRate
                    };
                    _totalMessagesToSend += UdpTestGenerator.Generate(p);
                }));
            }
        }

        static void StartTcp(CommandLineOptions options)
        {
            if (options.Tcp > 0)
            {
                if (options.Verbose)
                    LogManager.GetCurrentClassLogger()
                        .Info("Starting Tcp Generator for {0}:{1}", options.TcpHost, options.Tcp);

                _totalMessagesToSend += options.NumMessages;

                _tasks.Add(Task.Factory.StartNew(() =>
                {
                    var p = new TcpTestParameters()
                    {
                        Port = options.Tcp,
                        Host = options.TcpHost,
                        NumMessages = options.NumMessages,
                        SleepTimeMilliseconds = options.TcpRate
                    };
                    TcpTestGenerator.Generate(p);
                }));
            }
        }

    }
}
