using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Newtonsoft.Json;
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
using System.IO;


namespace TimberWinR.TestGenerator
{
    class LogFileTestParameters
    {
        public int NumMessages { get; set; }
        public string LogFileDir { get; set; }
        public string LogFileName { get; set; }
        public int SleepTimeMilliseconds { get; set; }
        public LogFileTestParameters()
        {
            SleepTimeMilliseconds = 30;
            LogFileDir = ".";
            NumMessages = 10;
        }
    }

    class LogFileGenerator
    {
        public static int Generate(JsonLogFileTestParameters parms)
        {
            LogManager.GetCurrentClassLogger().Info("Start LogFile Generation for: {0} on Thread: {1}", Path.GetFullPath(parms.LogFileName), Thread.CurrentThread.ManagedThreadId);

            var logFilePath = Path.Combine(parms.LogFileDir, parms.LogFileName);

            try
            {
                if (File.Exists(logFilePath))
                {
                    LogManager.GetCurrentClassLogger().Info("Deleting file: {0}", logFilePath);
                    File.Delete(logFilePath);
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }


            var hostName = System.Environment.MachineName + "." +
                   Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                       "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters").GetValue("Domain", "").ToString();

            var watch = Stopwatch.StartNew();

            // This text is always added, making the file longer over time 
            // if it is not deleted. 
            using (StreamWriter sw = File.AppendText(logFilePath))
            {
                sw.AutoFlush = true;
                for (int i = 0; i < parms.NumMessages; i++)
                {
                    JObject o = new JObject
                    {
                        {"LineNumber", i+1},
                        {"Application", "logfile-generator"},
                        {"Host", hostName},
                        {"UtcTimestamp", DateTime.UtcNow.ToString("o")},
                        {"Type", "log"},
                        {"Message", string.Format("{0}: Testgenerator logfile message {1}", i+1, DateTime.UtcNow.ToString("o"))},
                        {"Index", "logstash"}
                    };
                    sw.WriteLine(o.ToString(Formatting.None));

                    Thread.Sleep(parms.SleepTimeMilliseconds);
                }
                LogManager.GetCurrentClassLogger().Info("Elapsed Time for {0} was {1} seconds", Path.GetFullPath(parms.LogFileName), watch.Elapsed);
                watch.Reset();
            }

            LogManager.GetCurrentClassLogger().Info("Finished LogFile Generation for: {0} elapsed: {1}", Path.GetFullPath(parms.LogFileName), watch.Elapsed);

            return parms.NumMessages;
        }
    }
}
