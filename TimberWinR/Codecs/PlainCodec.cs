using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using TimberWinR.Parser;

namespace TimberWinR.Codecs
{
    public class PlainCodec : ICodec
    {
        private CodecArguments _codecArguments;

        public void Apply(string msg, Inputs.InputListener listener)
        {
            JObject json = new JObject();
            listener.AddDefaultFields(json);
            json["message"] = ExpandField(msg, json);
            listener.ProcessJson(json);
        }

        protected string ExpandField(string fieldName, JObject json)
        {
            foreach (var token in json.Children())
            {
                string replaceString = "%{" + token.Path + "}";
                fieldName = fieldName.Replace(replaceString, json[token.Path].ToString());
            }
            return fieldName;
        }


        public PlainCodec(CodecArguments args)
        {
            _codecArguments = args;
        }

    }
}
