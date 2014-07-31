using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TimberWinR.Inputs
{
    public abstract class InputBase
    {
        public const string TagName = "Inputs";
       
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("{0}\n", this.GetType().ToString()));
            sb.Append("Parameters:\n");
            foreach (var prop in this.GetType().GetProperties())
            {
                if (prop != null)
                {
                    if (prop.Name == "Fields")
                    {
                        sb.Append(String.Format("{0}:\n", prop.Name));
                        foreach (FieldDefinition f in (List<FieldDefinition>)prop.GetValue(this, null))
                        {
                            sb.Append(String.Format("\t{0}\n", f.Name));
                        }
                    }
                    else
                    {
                        sb.Append(String.Format("\t{0}: {1}\n", prop.Name, prop.GetValue(this, null)));
                    }
                }
            }
            return sb.ToString();
        }
    }
}
