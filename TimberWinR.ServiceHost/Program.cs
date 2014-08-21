using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TimberWinR.Outputs;
using TimberWinR.ServiceHost;
using TimberWinR.Inputs;

using Topshelf;
using Topshelf.HostConfigurators;
using Topshelf.Logging;
using Topshelf.ServiceConfigurators;

namespace TimberWinR.ServiceHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Arguments arguments = new Arguments();
           
            HostFactory.Run(hostConfigurator =>
            {
                string cmdLine = Environment.CommandLine;

                hostConfigurator.Service<TimberWinRService>(serviceConfigurator =>
                {
                    serviceConfigurator.ConstructUsing(() => new TimberWinRService(arguments));
                    serviceConfigurator.WhenStarted(myService => myService.Start());
                    serviceConfigurator.WhenStopped(myService => myService.Stop());
                });
               
                hostConfigurator.AddCommandLineDefinition("configFile", c => arguments.ConfigFile = c);
                hostConfigurator.AddCommandLineDefinition("logLevel", c => arguments.LogLevel = c);
                hostConfigurator.AddCommandLineDefinition("logDir", c => arguments.LogfileDir = c);
                hostConfigurator.AddCommandLineDefinition("diagnosticPort", c => arguments.DiagnosticPort = int.Parse(c));    

                hostConfigurator.ApplyCommandLine();
                hostConfigurator.RunAsLocalSystem();
                hostConfigurator.StartAutomatically();
                hostConfigurator.EnableShutdown();
                hostConfigurator.SetDisplayName("TimberWinR");
                hostConfigurator.SetDescription("TimberWinR using Topshelf");
                hostConfigurator.SetServiceName("TimberWinR");

                hostConfigurator.AfterInstall(() =>
                {
                    const string keyPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TimberWinR";
                    const string keyName = "ImagePath";

                    var currentValue = Registry.GetValue(keyPath, keyName, "").ToString();
                    if (!string.IsNullOrEmpty(currentValue))
                    {
                        AddServiceParameter("-configFile", arguments.ConfigFile);
                        AddServiceParameter("-logLevel", arguments.LogLevel);
                        AddServiceParameter("-logDir", arguments.LogfileDir);
                        if (arguments.DiagnosticPort > 0)
                            AddServiceParameter("-diagnosticPort", arguments.DiagnosticPort);
                    }
                });
            });
        }

        private static void AddServiceParameter(string paramName, string value)
        {
            string keyPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TimberWinR";
            string keyName = "ImagePath";

            string currentValue = Registry.GetValue(keyPath, keyName, "").ToString();

            if (!string.IsNullOrEmpty(paramName) && !currentValue.Contains(string.Format("{0} ", paramName)))           
            {
                currentValue += string.Format(" {0} \"{1}\"", paramName, value.Replace("\\\\", "\\"));               
                Registry.SetValue(keyPath, keyName, currentValue);               
            }
        }

        private static void AddServiceParameter(string paramName, int value)
        {
            string keyPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TimberWinR";
            string keyName = "ImagePath";

            string currentValue = Registry.GetValue(keyPath, keyName, "").ToString();

            if (!string.IsNullOrEmpty(paramName) && !currentValue.Contains(string.Format("{0}:", paramName)))
            {
                currentValue += string.Format(" {0}:{1}", paramName, value);
                Registry.SetValue(keyPath, keyName, currentValue);
            }
        }

    }

    internal class Arguments
    {
        public string ConfigFile { get; set; }
        public string LogLevel { get; set; }
        public string LogfileDir { get; set; }
        public int DiagnosticPort { get; set; }

        public Arguments()
        {
            DiagnosticPort = 5141;
            ConfigFile = "default.json";
            LogLevel = "Info";
            LogfileDir = @"C:\logs";
        }
    }


    internal class TimberWinRService
    {
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly CancellationToken _cancellationToken;
        readonly Task _serviceTask;
        private readonly Arguments _args;
        private TimberWinR.Diagnostics.Diagnostics _diags;
        private TimberWinR.Manager _manager;

        public TimberWinRService(Arguments args)
        {
            _args = args;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _serviceTask = new Task(RunService, _cancellationToken);           
        }

        public void Start()
        {
            _serviceTask.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            if (_diags != null)
             _diags.Shutdown();

            if (_manager != null)
                _manager.Shutdown();
        }

        /// <summary>
        /// The Main body of the Service Worker Thread
        /// </summary>
        private void RunService()
        {
            _manager = new TimberWinR.Manager(_args.ConfigFile, _args.LogLevel, _args.LogfileDir, _cancellationToken);
            if (_args.DiagnosticPort > 0)
                _diags = new Diagnostics.Diagnostics(_manager, _cancellationToken, _args.DiagnosticPort);
        }
    }
}


