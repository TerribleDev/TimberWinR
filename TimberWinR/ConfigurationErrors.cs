using System;
using System.Xml;
using System.Xml.Linq;

namespace TimberWinR
{
    public class ConfigurationErrors
    {
        public class MissingRequiredTagException : Exception
        {
            public MissingRequiredTagException(string tagName)
                : base(
                    string.Format("Missing required tag \"{0}\"", tagName))
            {
            }
        }

        public class MissingRequiredAttributeException : Exception
        {
            public MissingRequiredAttributeException(XElement e, string attributeName)
                : base(
                    string.Format("{0}:{1} Missing required attribute \"{2}\" for element <{3}>", e.Document.BaseUri,
                        ((IXmlLineInfo)e).LineNumber, attributeName, e.Name.ToString()))
            {
            }
        }

        public class InvalidAttributeNameException : Exception
        {
            public InvalidAttributeNameException(XAttribute a)
                : base(
                    string.Format("{0}:{1} Invalid Attribute Name <{2} {3}>", a.Document.BaseUri,
                        ((IXmlLineInfo)a).LineNumber, a.Parent.Name, a.Name.ToString()))
            {
            }
        }

        public class InvalidAttributeDateValueException : Exception
        {
            public InvalidAttributeDateValueException(XAttribute a)
                : base(
                    string.Format(
                        "{0}:{1} Invalid date format given for attribute. Format must be \"yyyy-MM-dd hh:mm:ss\". <{2} {3}>",
                        a.Document.BaseUri,
                        ((IXmlLineInfo)a).LineNumber, a.Parent.Name, a.ToString()))
            {
            }
        }

        public class InvalidAttributeIntegerValueException : Exception
        {
            public InvalidAttributeIntegerValueException(XAttribute a)
                : base(
                    string.Format("{0}:{1} Integer value not given for attribute. <{2} {3}>", a.Document.BaseUri,
                        ((IXmlLineInfo)a).LineNumber, a.Parent.Name, a.ToString()))
            {
            }
        }

        public class InvalidAttributeValueException : Exception
        {
            public InvalidAttributeValueException(XAttribute a)
                : base(
                    string.Format("{0}:{1} Invalid Attribute Value <{2} {3}>", a.Document.BaseUri,
                        ((IXmlLineInfo)a).LineNumber, a.Parent.Name, a.ToString()))
            {
            }
        }

        public class InvalidElementNameException : Exception
        {
            public InvalidElementNameException(XElement e)
                : base(
                    string.Format("{0}:{1} Invalid Element Name <{2}> <{3}>", e.Document.BaseUri,
                        ((IXmlLineInfo)e).LineNumber, e.Parent.Name, e.ToString()))
            {
            }
        }
    }
}
