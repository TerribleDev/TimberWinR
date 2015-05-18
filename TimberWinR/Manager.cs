using System.IO;
using System.Net.Sockets;
using System.Reflection;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimberWinR.Inputs;
using TimberWinR.Outputs;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace TimberWinR
{
    /// <summary>
    /// The Manager class for TimberWinR
    /// </summary>
    public class Manager
    {
        public Configuration Config { get; set; }
        public List<OutputSender> Outputs { get; set; }       
        public List<InputListener> Listeners { get; set; }
        public bool LiveMonitor { get; set; }      

        public event Action<Configuration> OnConfigurationProcessed;

        public DateTime StartedOn { get; set; }
        public string JsonConfig { get; set; }
        public string LogfileDir { get; set; }

        public int NumConnections
        {
            get { return numConnections; }
        }

        public int NumMessages
        {
            get { return numMessages; }
        }

        private static int numConnections;
        private static int numMessages;


        public void Shutdown()
        {          
            LogManager.GetCurrentClassLogger().Info("Shutting Down");

            foreach (InputListener listener in Listeners)
                listener.Shutdown();

            LogManager.GetCurrentClassLogger().Info("Completed ShutDown");
        }


        public void IncrementMessageCount(int count = 1)
        {
            Interlocked.Add(ref numMessages, count);
        }

        public Manager()
        {
            LogsFileDatabase.Manager = this;
        }

        public Manager(string jsonConfigFile, string logLevel, string logfileDir, bool liveMonitor, CancellationToken cancelToken, bool processConfiguration = true)
        {          
            LogsFileDatabase.Manager = this;

            StartedOn = DateTime.UtcNow;
            LiveMonitor = liveMonitor;

            var vfi = new FileInfo(jsonConfigFile);

            JsonConfig = vfi.FullName;
            LogfileDir = logfileDir;


            numMessages = 0;
            numConnections = 0;

            Outputs = new List<OutputSender>();
            Listeners = new List<InputListener>();

            var loggingConfiguration = new LoggingConfiguration();

            // Create our default targets
            var coloredConsoleTarget = new ColoredConsoleTarget();

            Target fileTarget = CreateDefaultFileTarget(logfileDir);

            loggingConfiguration.AddTarget("Console", coloredConsoleTarget);
            loggingConfiguration.AddTarget("DailyFile", fileTarget);

            // The LogLevel.Trace means has to be at least Trace to show up on console
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, coloredConsoleTarget));
            // LogLevel.Debug means has to be at least Debug to show up in logfile
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));

            LogManager.Configuration = loggingConfiguration;
            LogManager.EnableLogging();

            LogManager.GlobalThreshold = LogLevel.FromString(logLevel);

            //LogManager.GetCurrentClassLogger()
            //    .Info("TimberWinR Version {0}", GetAssemblyByName("TimberWinR.ServiceHost").GetName().Version.ToString());

            LogManager.GetCurrentClassLogger()
                .Info("TimberWinR Version {0}", Assembly.GetEntryAssembly().GetName().Version.ToString());

            LogManager.GetCurrentClassLogger()
                .Info("Database Filename: {0}", LogsFileDatabase.Instance.DatabaseFileName);

            try
            {
                var fi = new FileInfo(jsonConfigFile);
                if (fi.Exists)
                {
                    LogManager.GetCurrentClassLogger().Info("Initialized, Reading Configurations From File: {0}", fi.FullName);

                    if (!fi.Exists)
                        throw new FileNotFoundException("Missing config file", jsonConfigFile);

                    LogManager.GetCurrentClassLogger().Info("Initialized, Reading Config: {0}", fi.FullName);
                    Config = Configuration.FromFile(jsonConfigFile);
                }
                else if (Directory.Exists(jsonConfigFile))
                {
                    DirectoryInfo di = new DirectoryInfo(jsonConfigFile);
                    LogManager.GetCurrentClassLogger().Info("Initialized, Reading Configurations From {0}", di.FullName);
                    Config = Configuration.FromDirectory(jsonConfigFile, cancelToken, this);
                }
            }
            catch (JsonSerializationException jse)
            {
                LogManager.GetCurrentClassLogger().Error(jse);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }
            LogManager.GetCurrentClassLogger().Info("Log Directory {0}", logfileDir);
            LogManager.GetCurrentClassLogger().Info("Logging Level: {0}", LogManager.GlobalThreshold);

            if (processConfiguration)
            {
                ProcessConfiguration(cancelToken, Config);
            }         
        }

        public void Start(CancellationToken cancelToken)
        {
            ProcessConfiguration(cancelToken, Config);
        }

        public void ProcessConfiguration(CancellationToken cancelToken, Configuration config)
        {
            // Read the Configuration file
            if (config != null)
            {                
                if (OnConfigurationProcessed != null)
                    OnConfigurationProcessed(config);

                if (config.StatsDOutputs != null)
                {
                    foreach (var ro in config.StatsDOutputs)
                    {
                        var output = new StatsDOutput(this, ro, cancelToken);
                        Outputs.Add(output);
                    }
                }

                if (config.RedisOutputs != null)
                {
                    foreach (var ro in config.RedisOutputs)
                    {
                        var redis = new RedisOutput(this, ro, cancelToken);
                        Outputs.Add(redis);
                    }
                }
                if (config.ElasticsearchOutputs != null)
                {
                    foreach (var ro in config.ElasticsearchOutputs)
                    {
                        var els = new ElasticsearchOutput(this, ro, cancelToken);
                        Outputs.Add(els);
                    }
                }
                if (config.StdoutOutputs != null)
                {
                    foreach (var ro in config.StdoutOutputs)
                    {
                        var stdout = new StdoutOutput(this, ro, cancelToken);
                        Outputs.Add(stdout);
                    }
                }

                if (config.FileOutputs != null)
                {
                    foreach (var ro in config.FileOutputs)
                    {
                        var output = new FileOutput(this, ro, cancelToken);
                        Outputs.Add(output);
                    }
                }

                foreach (Parser.IISW3CLogParameters iisw3cConfig in config.IISW3C)
                {
                    var elistner = new IISW3CInputListener(iisw3cConfig, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (Parser.W3CLogParameters iisw3cConfig in config.W3C)
                {
                    var elistner = new W3CInputListener(iisw3cConfig, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (Parser.WindowsEvent eventConfig in config.Events)
                {
                    var elistner = new WindowsEvtInputListener(eventConfig, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (var logConfig in config.Logs)
                {
                    var elistner = new LogsListener(logConfig, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (var logConfig in config.TailFiles)
                {
                    var elistner = new TailFileListener(logConfig, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (var tcp in config.Tcps)
                {
                    var elistner = new TcpInputListener(tcp, cancelToken, tcp.Port);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (var udp in config.Udps)
                {
                    var elistner = new UdpInputListener(udp, cancelToken, udp.Port);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (var stdin in config.Stdins)
                {
                    var elistner = new StdinListener(stdin, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }

                foreach (var stdin in config.Generators)
                {
                    var elistner = new GeneratorInput(stdin, cancelToken);
                    Listeners.Add(elistner);
                    foreach (var output in Outputs)
                        output.Connect(elistner);
                }


                var computerName = System.Environment.MachineName + "." +
                                   Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                                       @"SYSTEM\CurrentControlSet\services\Tcpip\Parameters")
                                       .GetValue("Domain", "")
                                       .ToString();

                foreach (var output in Outputs)
                {
                    var name = Assembly.GetExecutingAssembly().GetName();
                    JObject json = new JObject(
                        new JProperty("TimberWinR",
                            new JObject(
                                new JProperty("version",
                                    Assembly.GetEntryAssembly().GetName().Version.ToString()),
                        //GetAssemblyByName("TimberWinR.ServiceHost").GetName().Version.ToString()),
                                new JProperty("host", computerName),
                                new JProperty("output", output.Name),
                                new JProperty("initialized", DateTime.UtcNow)
                                )));
                    json.Add(new JProperty("type", "Win32-TimberWinR"));
                    json.Add(new JProperty("host", computerName));
                    output.Startup(json);
                }               
            }
        }


        private Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                   SingleOrDefault(assembly => assembly.GetName().Name == name);
        }


        /// <summary>
        /// Creates the default <see cref="FileTarget"/>.
        /// </summary>
        /// <param name="logPath"></param>
        /// <returns>
        /// The NLog file target used in the default logging configuration.
        /// </returns>
        public static FileTarget CreateDefaultFileTarget(string logPath)
        {
            return new FileTarget
            {
                ArchiveEvery = FileArchivePeriod.None,
                ArchiveAboveSize = 5 * 1024 * 1024,
                MaxArchiveFiles = 5,
                BufferSize = 10,
                FileName = Path.Combine(logPath, "TimberWinR", "TimberWinR.log"),
                ArchiveFileName = Path.Combine(logPath, "TimberWinR_log-{#######}.log"),
            };
        }

    }
}
