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
            foreach (var evt in c.Events)
                Console.WriteLine(evt);
        }

        public void OutputLogs()
        {
            foreach (var log in c.Logs)
                Console.WriteLine(log);
        }

        public void OutputIIS()
        {
            foreach (var iis in c.IIS)
                Console.WriteLine(iis);
        }

        public void OutputIISW3C()
        {
            foreach (var iisw3c in c.IISW3C)
                Console.WriteLine(iisw3c);
        }

        public void OutputFilters()
        {
            foreach (var filter in c.Filters)
                Console.WriteLine(filter);           
        }

        [Test]
        public void Output()
        {
            OutputEvents();
            OutputLogs();
            OutputIIS();
            OutputIISW3C();
            OutputFilters();
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
        public void NumOfFilters()
        {
            Assert.AreEqual(3, c.Filters.ToArray().Length);
        }       

        [Test]
        public void FieldsOfEvents()
        {
            List<FieldDefinition> fields = new List<FieldDefinition>()
            {
                new FieldDefinition("EventLog", typeof(string)),
                new FieldDefinition("RecordNumber", typeof(int)),
                new FieldDefinition("TimeGenerated", typeof(DateTime)),
                new FieldDefinition("TimeWritten", typeof(DateTime)),
                new FieldDefinition("EventID", typeof(int)),
                new FieldDefinition("EventType", typeof(int)),
                new FieldDefinition("EventTypeName", typeof(string)),
                new FieldDefinition("EventCategory", typeof(int)),
                new FieldDefinition("EventCategoryName", typeof(string)),
                new FieldDefinition("SourceName", typeof(string)),
                new FieldDefinition("Strings", typeof(string)),
                new FieldDefinition("ComputerName", typeof(string)),
                new FieldDefinition("SID", typeof(string)),
                new FieldDefinition("Message", typeof(string)),
                new FieldDefinition("Data", typeof(string))
            };

            CollectionAssert.AreEqual(fields, c.Events.ToArray()[0].Fields);
        }

        [Test]
        public void FieldsOfLogs()
        {
            List<FieldDefinition> fields = new List<FieldDefinition>()
            {
                new FieldDefinition("LogFilename", typeof(string)),
                new FieldDefinition("Index", typeof(int)),
                new FieldDefinition("Text", typeof(string))
            };

            CollectionAssert.AreEqual(fields, c.Logs.ToArray()[0].Fields);
            CollectionAssert.AreEqual(fields, c.Logs.ToArray()[1].Fields);
            CollectionAssert.AreEqual(fields, c.Logs.ToArray()[2].Fields);
        }

        [Test]
        public void FieldsOfIIS()
        {
            List<FieldDefinition> fields = new List<FieldDefinition>()
            {
                new FieldDefinition("LogFilename", typeof(string)),
                new FieldDefinition("LogRow", typeof(int)),
                new FieldDefinition("UserIP", typeof(string)),
                new FieldDefinition("UserName", typeof(string)),
                new FieldDefinition("Date", typeof(DateTime)),
                new FieldDefinition("Time", typeof(DateTime)),
                new FieldDefinition("ServiceInstance", typeof(string)),
                new FieldDefinition("HostName", typeof(string)),
                new FieldDefinition("ServerIP", typeof(string)),
                new FieldDefinition("TimeTaken", typeof(int)),
                new FieldDefinition("BytesSent", typeof(int)),
                new FieldDefinition("BytesReceived", typeof(int)),
                new FieldDefinition("StatusCode", typeof(int)),
                new FieldDefinition("Win32StatusCode", typeof(int)),
                new FieldDefinition("RequestType", typeof(string)),
                new FieldDefinition("Target", typeof(string)),
                new FieldDefinition("Parameters", typeof(string))
            };

            foreach (var iis in c.IIS.ToArray())
            {
                CollectionAssert.AreEquivalent(fields, c.IIS.ToArray()[0].Fields);
            }
        }

        [Test]
        public void FieldsOfIISW3C()
        {
            List<FieldDefinition> fields = new List<FieldDefinition>()
            {
                new FieldDefinition("LogFilename", typeof(string)),
                new FieldDefinition("LogRow", typeof(int)),
                new FieldDefinition("date", typeof(DateTime)),
                new FieldDefinition("time", typeof(DateTime)),
                new FieldDefinition("c-ip", typeof(string)),
                new FieldDefinition("cs-username", typeof(string)),
                new FieldDefinition("s-sitename", typeof(string)),
                new FieldDefinition("s-computername", typeof(int)),
                new FieldDefinition("s-ip", typeof(string)),
                new FieldDefinition("s-port", typeof(int)),
                new FieldDefinition("cs-method", typeof(string)),
                new FieldDefinition("cs-uri-stem", typeof(string)),
                new FieldDefinition("cs-uri-query", typeof(string)),
                new FieldDefinition("sc-status", typeof(int)),
                new FieldDefinition("sc-substatus", typeof(int)),
                new FieldDefinition("sc-win32-status", typeof(int)),
                new FieldDefinition("sc-bytes", typeof(int)),
                new FieldDefinition("cs-bytes", typeof(int)),
                new FieldDefinition("time-taken", typeof(int)),
                new FieldDefinition("cs-version", typeof(string)),
                new FieldDefinition("cs-host", typeof(string)),
                new FieldDefinition("cs(User-Agent)", typeof(string)),
                new FieldDefinition("cs(Cookie)", typeof(string)),
                new FieldDefinition("cs(Referer)", typeof(string)),
                new FieldDefinition("s-event", typeof(string)),
                new FieldDefinition("s-process-type", typeof(string)),
                new FieldDefinition("s-user-time", typeof(double)),
                new FieldDefinition("s-kernel-time", typeof(double)),
                new FieldDefinition("s-page-faults", typeof(int)),
                new FieldDefinition("s-total-procs", typeof(int)),
                new FieldDefinition("s-active-procs", typeof(int)),
                new FieldDefinition("s-stopped-procs", typeof(int))
            };

            CollectionAssert.AreEquivalent(fields, c.IISW3C.ToArray()[0].Fields);
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

            TimberWinR.Inputs.TailFileInput log = c.Logs.ToArray()[0];

            Assert.AreEqual(name, log.Name);
            Assert.AreEqual(location, log.Location);
            Assert.AreEqual(iCodepage, log.ICodepage);
            Assert.AreEqual(recurse, log.Recurse);
            Assert.AreEqual(splitLongLines, log.SplitLongLines);


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
        }

        [Test]
        public void ParametersOfIISW3C()
        {
            string name = "Default site";
            string location = @"c:\inetpub\logs\LogFiles\W3SVC1\*";
            int iCodepage = -2;
            int recurse = 0;
            bool dQuotes = false;
            bool dirTime = false;
            bool consolidateLogs = false;

            TimberWinR.Inputs.IISW3CLog iisw3c = c.IISW3C.ToArray()[0];

            Assert.AreEqual(name, iisw3c.Name);
            Assert.AreEqual(location, iisw3c.Location);
            Assert.AreEqual(iCodepage, iisw3c.ICodepage);
            Assert.AreEqual(recurse, iisw3c.Recurse);
            Assert.IsNull(iisw3c.MinDateMod);
            Assert.AreEqual(dQuotes, iisw3c.DQuotes);
            Assert.AreEqual(dirTime, iisw3c.DirTime);
            Assert.AreEqual(consolidateLogs, iisw3c.ConsolidateLogs);
            Assert.IsEmpty(iisw3c.ICheckpoint);
        }

        [Test]
        public void ParametersOfGrokFilters()
        {
            List<TimberWinR.Filters.GrokFilter.FieldValuePair> addFields = new List<TimberWinR.Filters.GrokFilter.FieldValuePair>();
            List<string> removeFields = new List<string>();

            string field = "Text";
            string match = "%{IPAddress:ip1} %{IPAddress:ip2}";
            addFields.Add(new GrokFilter.FieldValuePair("field1", "%{foo}"));
            bool dropIfMatch = true;
            removeFields.Add("ip1");
            foreach (var filter in c.Filters)
            {
                if (filter.GetType() == typeof(GrokFilter))
                {
                    Assert.AreEqual(field, ((GrokFilter)filter).Field);
                    Assert.AreEqual(match, ((GrokFilter)filter).Match);
                    CollectionAssert.AreEqual(addFields, ((GrokFilter)filter).AddFields);
                    Assert.AreEqual(dropIfMatch, ((GrokFilter)filter).DropIfMatch);
                    Assert.AreEqual(removeFields, ((GrokFilter)filter).RemoveFields);
                }
            }
        }

        [Test]
        public void ParametersOfDateFilters()
        {
            List<string> patterns = new List<string>();

            string field = "timestamp";
            string target = "@timestamp";
            bool convertToUTC = true;
            patterns.Add("MMM  d HH:mm:ss");
            patterns.Add("MMM dd HH:mm:ss");
            patterns.Add("ISO8601");

            foreach (var filter in c.Filters)
            {
                if (filter.GetType() == typeof(DateFilter))
                {
                    Assert.AreEqual(field, ((DateFilter)filter).Field);
                    Assert.AreEqual(target, ((DateFilter)filter).Target);
                    Assert.AreEqual(convertToUTC, ((DateFilter)filter).ConvertToUTC);
                    CollectionAssert.AreEquivalent(patterns, ((DateFilter)filter).Patterns);
                }
            }
        }
    }
}
