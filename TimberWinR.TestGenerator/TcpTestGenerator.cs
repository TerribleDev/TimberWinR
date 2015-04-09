using System.Threading;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ServiceStack.Text;

namespace TimberWinR.TestGenerator
{
    class TcpTestParameters
    {
        public int Port { get; set; }
        public string Host { get; set; }
        public int NumMessages { get; set; }
        public int SleepTimeMilliseconds { get; set; }
        public TcpTestParameters()
        {
            NumMessages = 100;
            Port = 5140;
            Host = "localhost";
            SleepTimeMilliseconds = 10;
        }
    }

    class TcpTestGenerator
    {
        public static int Generate(TcpTestParameters parms)
        {
            TcpClient server = new TcpClient(parms.Host, parms.Port);
            
            var hostName = System.Environment.MachineName + "." +
                       Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                           "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters").GetValue("Domain", "").ToString();


            using (NetworkStream stream = server.GetStream())
            {              
                for (int i = 0; i < parms.NumMessages; i++)
                {
                    JObject o = new JObject
                    {
                        {"Application", "tcp-generator"},
                        {"Host", hostName},
                        {"UtcTimestamp", DateTime.UtcNow.ToString("o")},
                        {"Type", "tcp"},
                        {"Message", "tcp message " + DateTime.UtcNow.ToString("o")},
                        {"Index", "logstash"}
                    };
                    byte[] data = Encoding.UTF8.GetBytes(string.Format("{0}\n", o.ToString()));
                    stream.Write(data, 0, data.Length);
                    Thread.Sleep(parms.SleepTimeMilliseconds);
                }
            }

            return parms.NumMessages;
        }

    }
}
