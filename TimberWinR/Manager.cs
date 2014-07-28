using System.IO;
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

namespace TimberWinR
{
    /// <summary>
    /// The Manager class for TimberWinR
    /// </summary>
    public class Manager
    {
        public Configuration Config { get; set; }
        public List<OutputSender> Outputs { get; set; }

        public Manager(string xmlConfigFile, string jsonConfigFile, CancellationToken cancelToken)
        {
            Outputs = new List<OutputSender>();

            var loggingConfiguration = new LoggingConfiguration();

            // Create our default targets
            var coloredConsoleTarget = new ColoredConsoleTarget();

            Target fileTarget = CreateDefaultFileTarget("c:\\logs");

            loggingConfiguration.AddTarget("Console", coloredConsoleTarget);
            loggingConfiguration.AddTarget("DailyFile", fileTarget);

            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, coloredConsoleTarget));
            loggingConfiguration.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, fileTarget));
          
            LogManager.Configuration = loggingConfiguration;
            LogManager.EnableLogging();  
            
            LogManager.GetCurrentClassLogger().Info("Initialized");
        
            // Read the Configuration file
            Config = Configuration.FromFile(jsonConfigFile);

            if (Config.RedisOutputs != null)
            {
                foreach (var ro in Config.RedisOutputs)
                {
                    var redis = new RedisOutput(this, ro.Host, cancelToken, ro.Index, ro.Port, ro.Timeout);
                    Outputs.Add(redis);
                }
            }

            foreach (Parser.IISW3CLog iisw3cConfig in Config.IISW3C)
            {
                var elistner = new IISW3CInputListener(iisw3cConfig, cancelToken);
                foreach(var output in Outputs)
                    output.Connect(elistner);
            }

            foreach (Parser.WindowsEvent eventConfig in Config.Events)
            {
                var elistner = new WindowsEvtInputListener(eventConfig, cancelToken);
                foreach (var output in Outputs)
                    output.Connect(elistner);
            }

            foreach (var logConfig in Config.Logs)
            {
                var elistner = new TailFileInputListener(logConfig, cancelToken);
                foreach (var output in Outputs)
                    output.Connect(elistner);
            }

            foreach (var tcp in Config.Tcps)
            {
                var elistner = new TcpInputListener(cancelToken, tcp.Port);
                foreach (var output in Outputs)
                    output.Connect(elistner);
            }
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
                ArchiveAboveSize = 10 * 1024 * 1024,
                MaxArchiveFiles = 5,
                BufferSize = 10,
                FileName = Path.Combine(logPath, "TimberWinR", "TimberWinR.txt"),
                ArchiveFileName = Path.Combine(logPath, "log-{#######}.txt"),
            };
        }

    }
}
