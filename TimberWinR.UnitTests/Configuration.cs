using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimberWinR;
using TimberWinR.Inputs;
using TimberWinR.Filters;

namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class ConfigurationTest
    {
        Configuration c = new Configuration("testconf.xml");

        public void OutputEvents()
        {
            foreach (var evt in c.Events.ToArray())
            {
                Console.WriteLine(evt);
            }
        }

        public void OutputLogs()
        {
            foreach (var log in c.Logs.ToArray())
            {
                Console.WriteLine(log);
            }
        }

        public void OutputIIS()
        {
            foreach (var iis in c.IIS.ToArray())
            {
                Console.WriteLine(iis);
            }
        }

        public void OutputIISW3C()
        {
            foreach (var iisw3c in c.IISW3C.ToArray())
            {
                Console.WriteLine(iisw3c);
            }
        }

        public void OutputGroks()
        {

            //IEnumerable<FilterBase> filters = c.Filters;

            //foreach (var grok in c.Filters)
            //    Console.WriteLine(grok);           
        }

        [Test]
        public void Test1()
        {
            Assert.AreEqual(c.Logs.ToArray()[1].Name, "Second Set");
        }

        [Test]
        public void NumOfEvents()
        {
            Assert.AreEqual(1, c.Events.ToArray().Length);
        }

        [Test]
        public void NumOfLogs()
        {
            Assert.AreEqual(3, c.Logs.ToArray().Length);
        }

        [Test]
        public void NumOfIIS()
        {
            Assert.AreEqual(0, c.IIS.ToArray().Length);
        }

        [Test]
        public void NumOfIISW3C()
        {
            Assert.AreEqual(1, c.IISW3C.ToArray().Length);
        }       

        [Test]
        public void FieldsOfEvents()
        {
            Dictionary<string, Type> fields = new Dictionary<string, Type>()
            {
                { "EventLog", typeof(string) },
                { "RecordNumber", typeof(int) },
                { "TimeGenerated", typeof(DateTime) },
                { "TimeWritten", typeof(DateTime) },
                { "EventID", typeof(int) },
                { "EventType", typeof(int) },
                { "EventTypeName", typeof(string) },
                { "EventCategory", typeof(int) },
                { "EventCategoryName", typeof(string) },
                { "SourceName", typeof(string) },
                { "Strings", typeof(string) },
                { "ComputerName", typeof(string) },
                { "SID", typeof(string) },
                { "Message", typeof(string) },
                { "Data", typeof(string) }
            };
            foreach (FieldDefinition field in c.Events.ToArray()[0].Fields) 
            {
                Assert.Contains(field.Name, fields.Keys);
            }
        }

        [Test]
        public void FieldsOfLogs()
        {
            Dictionary<string, Type> fields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "Index", typeof(int) },
                { "Text", typeof(string) }
            };
            foreach (FieldDefinition field in c.Logs.ToArray()[0].Fields)
            {
                Assert.Contains(field.Name, fields.Keys);
            }
            foreach (FieldDefinition field in c.Logs.ToArray()[1].Fields)
            {
                Assert.Contains(field.Name, fields.Keys);
            }
            foreach (FieldDefinition field in c.Logs.ToArray()[2].Fields)
            {
                Assert.Contains(field.Name, fields.Keys);
            }

        }

        [Test]
        public void FieldsOfIIS()
        {
            Dictionary<string, Type> fields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "LogRow", typeof(int) },
                { "UserIP", typeof(string) },
                { "UserName", typeof(string) },
                { "Date", typeof(DateTime) },
                { "Time", typeof(DateTime) },
                { "ServiceInstance", typeof(string) },
                { "HostName", typeof(string) },
                { "ServerIP", typeof(string) },
                { "TimeTaken", typeof(int) },
                { "BytesSent", typeof(int) },
                { "BytesReceived", typeof(int) },
                { "StatusCode", typeof(int) },
                { "Win32StatusCode", typeof(int) },
                { "RequestType", typeof(string) },
                { "Target", typeof(string) },
                { "Parameters", typeof(string) }
            };

            foreach (var iis in c.IIS.ToArray())
            {
                foreach (FieldDefinition field in iis.Fields)
                {
                    Assert.Contains(field.Name, fields.Keys);
                }
            }

        }

        [Test]
        public void FieldsOfIISW3C()
        {
            Dictionary<string, Type> fields = new Dictionary<string, Type>()
            {
                { "LogFilename", typeof(string) },
                { "LogRow", typeof(int) },
                { "date", typeof(DateTime) },
                { "time", typeof(DateTime) },
                { "c-ip", typeof(string) },
                { "cs-username", typeof(string) },
                { "s-sitename", typeof(string) },
                { "s-computername", typeof(int) },
                { "s-ip", typeof(string) },
                { "s-port", typeof(int) },
                { "cs-method", typeof(string) },
                { "cs-uri-stem", typeof(string) },
                { "cs-uri-query", typeof(string) },
                { "sc-status", typeof(int) },
                { "sc-substatus", typeof(int) },
                { "sc-win32-status", typeof(int) },
                { "sc-bytes", typeof(int) },
                { "cs-bytes", typeof(int) },
                { "time-taken", typeof(int) },
                { "cs-version", typeof(string) },
                { "cs-host", typeof(string) },
                { "cs(User-Agent)", typeof(string) },
                { "cs(Cookie)", typeof(string) },
                { "cs(Referer)", typeof(string) },
                { "s-event", typeof(string) },
                { "s-process-type", typeof(string) },
                { "s-user-time", typeof(double) },
                { "s-kernel-time", typeof(double) },
                { "s-page-faults", typeof(int) },
                { "s-total-procs", typeof(int) },
                { "s-active-procs", typeof(int) },
                { "s-stopped-procs", typeof(int) }
            };
            foreach (FieldDefinition field in c.IISW3C.ToArray()[0].Fields)
            {
                Assert.Contains(field.Name, fields.Keys);
            }
        }

        [Test]
        public void ParametersOfEvents()
        {
            string source = "System,Application";
            bool fullText = true;
            bool resolveSIDS = true;
            bool formatMsg = true;
            string msgErrorMode = "MSG";
            bool fullEventCode = false;
            string direction = "FW";
            string stringsSep = "|";
            string iCheckpoint;
            string binaryFormat = "PRINT";

            TimberWinR.Inputs.WindowsEvent evt = c.Events.ToArray()[0];

            Assert.AreEqual(source, evt.Source);
            Assert.AreEqual(fullText, evt.FullText);
            Assert.AreEqual(resolveSIDS, evt.ResolveSIDS);
            Assert.AreEqual(formatMsg, evt.FormatMsg);
            Assert.AreEqual(msgErrorMode, evt.MsgErrorMode);
            Assert.AreEqual(fullEventCode, evt.FullEventCode);
            Assert.AreEqual(direction, evt.Direction);
            Assert.AreEqual(stringsSep, evt.StringsSep);
            Assert.IsNull(evt.ICheckpoint);
            Assert.AreEqual(binaryFormat, evt.BinaryFormat);
        }

        [Test]
        public void ParametersOfLogs()
        {
            string name = "First Set";
            string location = @"C:\Logs1\*.log";
            int iCodepage = 0;
            int recurse = 0;
            bool splitLongLines = false;
            string iCheckpoint;

            TimberWinR.Inputs.TailFileInput log = c.Logs.ToArray()[0];

            Assert.AreEqual(name, log.Name);
            Assert.AreEqual(location, log.Location);
            Assert.AreEqual(iCodepage, log.ICodepage);
            Assert.AreEqual(recurse, log.Recurse);
            Assert.AreEqual(splitLongLines, log.SplitLongLines);
            Assert.IsNull(log.ICheckpoint);

            name = "Second Set";
            location = @"C:\Logs2\*.log";
            iCodepage = 0;
            recurse = 0;
            splitLongLines = false;

            log = c.Logs.ToArray()[1];

            Assert.AreEqual(name, log.Name);
            Assert.AreEqual(location, log.Location);
            Assert.AreEqual(iCodepage, log.ICodepage);
            Assert.AreEqual(recurse, log.Recurse);
            Assert.AreEqual(splitLongLines, log.SplitLongLines);
            Assert.IsNull(log.ICheckpoint);


            name = "Third Set";
            location = @"C:\Logs2\1.log,C:\Logs2\2.log";
            iCodepage = 0;
            recurse = 0;
            splitLongLines = false;

            log = c.Logs.ToArray()[2];

            Assert.AreEqual(name, log.Name);
            Assert.AreEqual(location, log.Location);
            Assert.AreEqual(iCodepage, log.ICodepage);
            Assert.AreEqual(recurse, log.Recurse);
            Assert.AreEqual(splitLongLines, log.SplitLongLines);
            Assert.IsNull(log.ICheckpoint);
        }

        [Test]
        public void ParametersOfIISW3C()
        {
            string name = "Default site";
            string location = @"c:\inetpub\logs\LogFiles\W3SVC1\*";
            int iCodepage = -2;
            int recurse = 0;
            string minDateMod;
            bool dQuotes = false;
            bool dirTime = false;
            bool consolidateLogs = false;
            string iCheckpoint;

            TimberWinR.Inputs.IISW3CLog iisw3c = c.IISW3C.ToArray()[0];

            Assert.AreEqual(name, iisw3c.Name);
            Assert.AreEqual(location, iisw3c.Location);
            Assert.AreEqual(iCodepage, iisw3c.ICodepage);
            Assert.AreEqual(recurse, iisw3c.Recurse);
            Assert.IsNull(iisw3c.MinDateMod);
            Assert.AreEqual(dQuotes, iisw3c.DQuotes);
            Assert.AreEqual(dirTime, iisw3c.DirTime);
            Assert.AreEqual(consolidateLogs, iisw3c.ConsolidateLogs);
            Assert.IsNull(iisw3c.ICheckpoint);
        }       
    }
}
