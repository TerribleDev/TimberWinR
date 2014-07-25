using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using System.Text;
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


            Type x = Type.GetType("string");
            Type x1 = Type.GetType("System.string");

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
        private TcpInputListener _nlogListener;

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
            _nlogListener.Shutdown();
        }

        /// <summary>
        /// The Main body of the Service Worker Thread
        /// </summary>
        private void RunService()
        {
            TimberWinR.Manager manager = new TimberWinR.Manager(_args.ConfigFile, _args.JsonFile);

            var outputRedis = new RedisOutput(manager, new string[] { "logaggregator.vistaprint.svc" }, _cancellationToken);

            _nlogListener = new TcpInputListener(_cancellationToken, 5140);
            outputRedis.Connect(_nlogListener);

            foreach (Parser.IISW3CLog iisw3cConfig in manager.Config.IISW3C)
            {
                var elistner = new IISW3CInputListener(iisw3cConfig, _cancellationToken);
                outputRedis.Connect(elistner);
            }

            foreach (Parser.WindowsEvent eventConfig in manager.Config.Events)
            {
                var elistner = new WindowsEvtInputListener(eventConfig, _cancellationToken);
                outputRedis.Connect(elistner);
            }

            foreach (var logConfig in manager.Config.Logs)
            {
                var elistner = new TailFileInputListener(logConfig, _cancellationToken);
                outputRedis.Connect(elistner);
            }
        }
    }
}


