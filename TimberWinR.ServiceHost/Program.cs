using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace TimberWinR.ServiceHost
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(hostConfigurator =>
            {
                hostConfigurator.Service<TimberWinRService>(serviceConfigurator =>
                {
                    serviceConfigurator.ConstructUsing(() => new TimberWinRService());
                    serviceConfigurator.WhenStarted(myService => myService.Start());
                    serviceConfigurator.WhenStopped(myService => myService.Stop());
                });

                hostConfigurator.RunAsLocalSystem();

                hostConfigurator.SetDisplayName("TimberWinR");
                hostConfigurator.SetDescription("TimberWinR using Topshelf");
                hostConfigurator.SetServiceName("TimberWinR");
            });
        }
    }

    internal class TimberWinRService
    {
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly CancellationToken _cancellationToken;
        readonly Task _task;

        public TimberWinRService()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _task = new Task(RunService, _cancellationToken);
           
        }
        
        public void Start()
        {
            _task.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// The Main body of the Service Worker Thread
        /// </summary>
        private void RunService()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                Console.WriteLine("I am working");

                Console.WriteLine("   Step 1");
                System.Threading.Thread.Sleep(1000);

                Console.WriteLine("   Step 2");
                System.Threading.Thread.Sleep(1000);

                Console.WriteLine("   Step 3");
                System.Threading.Thread.Sleep(1000);

                Console.WriteLine("   Step 4");
                System.Threading.Thread.Sleep(1000);

                Console.WriteLine("   Step 5");
                System.Threading.Thread.Sleep(1000);

            }

        }

    }
}


