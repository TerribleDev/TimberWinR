using System.Threading;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TimberWinR.TestGenerator
{
    class UdpTestParameters
    {
        public int Port { get; set; }
        public string Host { get; set; }
        public int NumMessages { get; set; }
        public int SleepTimeMilliseconds { get; set; }
        public UdpTestParameters()
        {
            NumMessages = 100;
            Port = 6379;
            Host = "localhost";
            SleepTimeMilliseconds = 10;
        }
    }

    class UdpTestGenerator
    {
        public static int Generate(UdpTestParameters parms)
        {
            var hostName = System.Environment.MachineName + "." +
               Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                   "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters").GetValue("Domain", "").ToString();
     
            IPAddress broadcast;
            if (!IPAddress.TryParse(parms.Host, out broadcast))
                broadcast = Dns.GetHostEntry(parms.Host).AddressList[0];

            Socket s = new Socket(broadcast.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            LogManager.GetCurrentClassLogger().Info("Start UDP Generation");

            for (int i = 0; i < parms.NumMessages; i++)
            {
                JObject o = new JObject
                {
                    {"Application", "udp-generator"},               
                    {"Host", hostName},
                    {"UtcTimestamp", DateTime.UtcNow.ToString("o")},
                    {"Type", "udp"},                
                    {"Message", "Testgenerator udp message " + DateTime.UtcNow.ToString("o")},
                    {"Index", "logstash"}
                };
                byte[] sendbuf = Encoding.UTF8.GetBytes(o.ToString());
                IPEndPoint ep = new IPEndPoint(broadcast, parms.Port);
                s.SendTo(sendbuf, ep);
                Thread.Sleep(parms.SleepTimeMilliseconds);
            }

            LogManager.GetCurrentClassLogger().Info("Finished UDP Generation");

            return parms.NumMessages;
        }

    }
}
