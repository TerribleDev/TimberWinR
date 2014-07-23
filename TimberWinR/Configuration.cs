using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Globalization;
using System.Xml.Schema;

using TimberWinR.Inputs;
using TimberWinR.Filters;

using NLog;

namespace TimberWinR
{
    public class Configuration
    {

        private static List<WindowsEvent> _events = new List<WindowsEvent>();

        public IEnumerable<WindowsEvent> Events
        {
            get { return _events; }
        }

        private static List<TailFileInput> _logs = new List<TailFileInput>();

        public IEnumerable<TailFileInput> Logs
        {
            get { return _logs; }
        }

        private static List<IISLog> _iislogs = new List<IISLog>();

        public IEnumerable<IISLog> IIS
        {
            get { return _iislogs; }
        }

        private static List<IISW3CLog> _iisw3clogs = new List<IISW3CLog>();

        public IEnumerable<IISW3CLog> IISW3C
        {
            get { return _iisw3clogs; }
        }

        private static List<FilterBase> _filters = new List<FilterBase>();

        public IEnumerable<FilterBase> Filters
        {
            get { return _filters; }
        }

        public Configuration(string xmlConfFile)
        {
            validateWithSchema(xmlConfFile, Properties.Resources.configSchema);

            try
            {
                parseConfInput(xmlConfFile);
                parseConfFilter(xmlConfFile);
            }
            catch(Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }
        }

        private static void validateWithSchema(string xmlConfFile, string xsdSchema)
        {
            XDocument config = XDocument.Load(xmlConfFile, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);

            // Ensure that the xml configuration file provided obeys the xsd schema.
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(new StringReader(xsdSchema)));
#if true
            bool errorsFound = false;
            config.Validate(schemas, (o, e) =>
            {
                errorsFound = true;
                LogManager.GetCurrentClassLogger().Error(e.Message);
            }, true);

            if (errorsFound)
                DumpInvalidNodes(config.Root);
#endif
        }

        static void DumpInvalidNodes(XElement el)
        {
            if (el.GetSchemaInfo().Validity != XmlSchemaValidity.Valid)
                LogManager.GetCurrentClassLogger().Error("Invalid Element {0}",
                    el.AncestorsAndSelf()
                    .InDocumentOrder()
                    .Aggregate("", (s, i) => s + "/" + i.Name.ToString()));
            foreach (XAttribute att in el.Attributes())
                if (att.GetSchemaInfo().Validity != XmlSchemaValidity.Valid)
                    LogManager.GetCurrentClassLogger().Error("Invalid Attribute {0}",
                        att
                        .Parent
                        .AncestorsAndSelf()
                        .InDocumentOrder()
                        .Aggregate("",
                            (s, i) => s + "/" + i.Name.ToString()) + "/@" + att.Name.ToString()
                        );
            foreach (XElement child in el.Elements())
                DumpInvalidNodes(child);
        }


        static void parseConfInput(string xmlConfFile)
        {
            XDocument config = XDocument.Load(xmlConfFile, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);

            // Begin parsing the xml configuration file.
            IEnumerable<XElement> inputs =
                from el in config.Root.Elements("Inputs")
                select el;

            string tagName = "Inputs";
            if (inputs.Count() == 0)          
                throw new TimberWinR.ConfigurationErrors.MissingRequiredTagException(tagName);
           
            // WINDOWS EVENTS
            IEnumerable<XElement> xml_events =
                from el in inputs.Elements("WindowsEvents").Elements("Event")
                select el;

            foreach (XElement e in xml_events)           
                WindowsEvent.Parse(_events, e);          

            // TEXT LOGS
            IEnumerable<XElement> xml_logs =
                from el in inputs.Elements("Logs").Elements("Log")
                select el;

            foreach (XElement e in xml_logs)          
                TailFileInput.Parse(_logs, e);         

            // IIS LOGS
            IEnumerable<XElement> xml_iis =
                from el in inputs.Elements("IISLogs").Elements("IISLog")
                select el;

            foreach (XElement e in xml_iis)           
                IISLog.Parse(_iislogs, e);            

            // IISW3C LOGS
            IEnumerable<XElement> xml_iisw3c =
                from el in inputs.Elements("IISW3CLogs").Elements("IISW3CLog")
                select el;

            foreach (XElement e in xml_iisw3c)      
                IISW3CLog.Parse(_iisw3clogs, e);           
        }

        static void parseConfFilter(string xmlConfFile)
        {
            XDocument config = XDocument.Load(xmlConfFile, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);

            IEnumerable<XElement> filters =
                from el in config.Root.Elements("Filters")
                select el;                   

            foreach (XElement e in filters.Elements())
            {
                switch (e.Name.ToString())
                {
                    case DateFilter.TagName:
                        DateFilter.Parse(_filters, e);
                        break;
                    case GrokFilter.TagName:
                        GrokFilter.Parse(_filters, e);
                        break;
                    case MutateFilter.TagName:
                        MutateFilter.Parse(_filters, e);   
                        break;
                    default:
                        throw new Exception(string.Format("Unknown tag: {0}", e.Name.ToString()));
                }
            }
        }
    }
}