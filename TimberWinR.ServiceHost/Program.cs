using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TimberWinR.Outputs;
using TimberWinR.ServiceHost;
using TimberWinR.Inputs;

using Topshelf;
using Topshelf.HostConfigurators;
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
                hostConfigurator.AddCommandLineDefinition("jsonFile", c => arguments.JsonFile = c);

                hostConfigurator.ApplyCommandLine();
                hostConfigurator.RunAsLocalSystem();
                hostConfigurator.StartAutomatically();
                hostConfigurator.EnableShutdown();
                hostConfigurator.SetDisplayName("TimberWinR");
                hostConfigurator.SetDescription("TimberWinR using Topshelf");
                hostConfigurator.SetServiceName("TimberWinR");
            });
        }
    }

    internal class Arguments
    {
        public string ConfigFile { get; set; }
        public string JsonFile { get; set; }

        public Arguments()
        {
            ConfigFile = string.Empty;
        }
    }


    internal class TimberWinRService
    {
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly CancellationToken _cancellationToken;
        readonly Task _serviceTask;
        private readonly Arguments _args;
      
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
            
            if (_manager != null)
                _manager.Shutdown();
        }

        /// <summary>
        /// The Main body of the Service Worker Thread
        /// </summary>
        private void RunService()
        {
            _manager = new TimberWinR.Manager(_args.ConfigFile, _args.JsonFile, _cancellationToken);
        }
    }
}


