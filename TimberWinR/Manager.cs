using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimberWinR
{
    /// <summary>
    /// The Manager class for TimberWinR
    /// </summary>
    public class Manager
    {
        public Configuration Config { get; set; }
        
        public Manager(string xmlConfigFile, string jsonConfigFile)
        { 
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
