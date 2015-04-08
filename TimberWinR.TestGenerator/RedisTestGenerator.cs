using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ServiceStack.Redis;

namespace TimberWinR.TestGenerator
{
    class RedisTestParameters
    {
        public int Port { get; set; }
        public string Host { get; set; }
        public int NumMessages { get; set; }
        public RedisTestParameters()
        {
            NumMessages = 100;
            Port = 6379;
            Host = "localhost";
        }
    }

    class RedisTestGenerator
    {
        public static void Generate(RedisTestParameters parms)
        {
            var hostName = System.Environment.MachineName + "." +
               Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                   "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters").GetValue("Domain", "").ToString();

            var rc = new RedisClient(parms.Host, parms.Port);

            for (int i = 0; i < parms.NumMessages; i++)
            {
                JObject o = new JObject
                {
                    {"Application", "redis-generator"},               
                    {"Host", hostName},
                    {"UtcTimestamp", DateTime.UtcNow.ToString("o")},
                    {"Type", "redis"},                
                    {"Message", "redis message " + DateTime.UtcNow.ToString("o")},
                    {"Index", "logstash"}
                };
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(o.ToString());
                var restult = rc.RPush("logstash", bytes);
            }
        }
    }
}
