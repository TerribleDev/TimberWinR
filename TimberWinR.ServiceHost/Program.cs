using System;
using System.Collections.Generic;
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
          
        private readonly TcpInputListener _nlogListener;

        public TimberWinRService(Arguments args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _serviceTask = new Task(RunService, _cancellationToken);

            var elistner = new WindowsEvtInputListener(_cancellationToken);

            _nlogListener = new TcpInputListener(_cancellationToken, 5140);
            var outputRedis = new RedisOutput(new string[] { "tstlexiceapp006.vistaprint.svc", "tstlexiceapp007.vistaprint.svc" }, _cancellationToken);
            outputRedis.Connect(_nlogListener);
            outputRedis.Connect(elistner);        
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
            TimberWinR.Manager manager = new TimberWinR.Manager();

            //while (!_cancellationTokenSource.IsCancellationRequested)
            //{               
            //    System.Threading.Thread.Sleep(1000);               
            //}
        }
    }
}


