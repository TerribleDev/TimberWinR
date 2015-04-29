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
    class JsonLogFileTestParameters
    {
        public int NumMessages { get; set; }
        public string LogFileDir { get; set; }
        public string LogFileName { get; set; }
        public int SleepTimeMilliseconds { get; set; }
        public JsonLogFileTestParameters()
        {
            SleepTimeMilliseconds = 30;
            LogFileDir = ".";
            NumMessages = 10;
        }
    }

    class JsonLogFileGenerator
    {
        public static int Generate(JsonLogFileTestParameters parms)
        {
            LogManager.GetCurrentClassLogger().Info("Start JSON LogFile Generation for: {0} on Thread: {1}", Path.GetFullPath(parms.LogFileName), Thread.CurrentThread.ManagedThreadId);

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
                for (int i = 0; i < parms.NumMessages; i++)
                {
                    JObject o = new JObject
                    {
                        {"LineNumber", i+1},
                        {"Application", "jsonlogfile-generator"},
                        {"Host", hostName},
                        {"UtcTimestamp", DateTime.UtcNow.ToString("o")},
                        {"Type", "jsonlog"},
                        {"Message", string.Format("{0}: Testgenerator jsonlogfile message {1}", i+1, DateTime.UtcNow.ToString("o"))},
                        {"Index", "logstash"}
                    };
                    sw.WriteLine(o.ToString(Formatting.None));

                    Thread.Sleep(parms.SleepTimeMilliseconds);
                }
                LogManager.GetCurrentClassLogger().Info("Elapsed Time for {0} was {1} seconds", Path.GetFullPath(parms.LogFileName), watch.Elapsed);
                watch.Reset();
            }

            LogManager.GetCurrentClassLogger().Info("Finished JSON Log File Generation for: {0} elapsed: {1}", Path.GetFullPath(parms.LogFileName), watch.Elapsed);

            return parms.NumMessages;
        }
    }

    class JsonRollingLogFileGenerator
    {
        public static int Generate(JsonLogFileTestParameters parms)
        {
            LogManager.GetCurrentClassLogger().Info("Start JSON RollingLogFile Generation for: {0} on Thread: {1}", Path.GetFullPath(parms.LogFileName), Thread.CurrentThread.ManagedThreadId);

            var logFilePath = Path.Combine(parms.LogFileDir, parms.LogFileName);

            try
            {
                if (File.Exists(logFilePath))
                    File.Delete(logFilePath);

                if (File.Exists(logFilePath + ".rolled"))
                    File.Delete(logFilePath + ".rolled");
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }


            var hostName = System.Environment.MachineName + "." +
                   Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                       "SYSTEM\\CurrentControlSet\\services\\Tcpip\\Parameters").GetValue("Domain", "").ToString();

         
            int quarters = parms.NumMessages/4;

            int[] segments = new int[] {quarters, quarters, quarters, quarters + parms.NumMessages%4};
            var watch = Stopwatch.StartNew();


            int recordNumber = 0;
            int currentTotal = 0;
            for (int segment = 0; segment < 4; segment++)
            {
                currentTotal += segments[segment];
             
                // This text is always added, making the file longer over time 
                // if it is not deleted. 
                using (StreamWriter sw = File.AppendText(logFilePath))
                {
                    sw.AutoFlush = true;
   
                    var lwatch = Stopwatch.StartNew();

                    // The Rolling Generator will roll 1/2 way through the log             
                    for (int i = 0; i < segments[segment]; i++)
                    {
                        JObject o = new JObject
                        {
                            {"LineNumber", recordNumber + 1},
                            {"Application", "jsonrollinglogfile-generator"},
                            {"Host", hostName},
                            {"UtcTimestamp", DateTime.UtcNow.ToString("o")},
                            {"Type", "jsonrollinglog"},
                            {
                                "Message",
                                string.Format("{0}: Testgenerator jsonrollinglogfile message {1}", recordNumber + 1,
                                    DateTime.UtcNow.ToString("o"))
                            },
                            {"Index", "logstash"}
                        };
                        sw.WriteLine(o.ToString(Formatting.None));
                        recordNumber++;
                        Thread.Sleep(parms.SleepTimeMilliseconds);
                    }
                    LogManager.GetCurrentClassLogger().Info("Elapsed Time for {0} was {1} seconds for {2} logs", Path.GetFullPath(parms.LogFileName), lwatch.Elapsed, segments[segment]);
                   
                }
               
                // 
                // We might not have yet processed all the lines from the first file, so wait till
                // we catch up before rolling the log file.
                //
                LogManager.GetCurrentClassLogger().Info("{0}: Waiting for output to catch up: {1} {2}", Thread.CurrentThread.ManagedThreadId, logFilePath, currentTotal);
                WaitOutputToCatchUp(logFilePath, currentTotal);

                //
                // Roll the log + wait for the reader to catch up.
                //  
                
                LogManager.GetCurrentClassLogger().Info("{0}: Rolling Log File: {1} {2}", Thread.CurrentThread.ManagedThreadId, logFilePath, File.GetCreationTimeUtc(logFilePath));

                RollLogFile(logFilePath); 
                
                LogManager.GetCurrentClassLogger().Info("{0}: Finished Rolling Log File: {1}", Thread.CurrentThread.ManagedThreadId, logFilePath);           
            }
            
            watch.Stop();
           
            LogManager.GetCurrentClassLogger().Info("Finished JSON RollingLogFile File Generation for: {0} elapsed: {1}", Path.GetFullPath(parms.LogFileName), watch.Elapsed);

            return parms.NumMessages;
        }

        private static void WaitOutputToCatchUp(string logFilePath, int firstPart)
        {
            bool caughtUp = false;
            do
            {
                var json = Program.Diagnostics.DiagnosticsOutput();

                IList<JToken> inputs = json["timberwinr"]["inputs"].Children().ToList();
                foreach (JToken t in inputs)
                {
                    JProperty inputProp = t.First as JProperty;
                    if (inputProp.Name == "taillog" || inputProp.Name == "log")
                    {
                        var files = inputProp.Value["filedb"].Children().ToList();
                        foreach (var file in files)
                        {
                            var fileName = file["FileName"].ToString();
                            FileInfo fi1 = new FileInfo(fileName);
                            FileInfo fi2 = new FileInfo(logFilePath);
                            if (fi1.FullName == fi2.FullName)
                            {
                                var linesProcessed = file["LinesProcessed"].Value<int>();
                                if (linesProcessed >= firstPart)
                                {
                                    caughtUp = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                Thread.Sleep(300);
            } while (!caughtUp);

            LogManager.GetCurrentClassLogger().Info("{0}:  Finished Waiting for output to catch up: {1} {2}", Thread.CurrentThread.ManagedThreadId, logFilePath, firstPart);            

        }

        private static void RollLogFile(string logFilePath)
        {
            bool moved = false;           
            do
            {
                try
                {
                    if (File.Exists(logFilePath + ".rolled"))
                        File.Delete(logFilePath + ".rolled");

                    File.Move(logFilePath, logFilePath + ".rolled");
                    moved = true;                   
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            } while (!moved);
            Thread.Sleep(1000);
        }
    }

}
