using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using TimberWinR.Parser;

namespace TimberWinR.Codecs
{
    class JsonCodec : ICodec
    {
        private CodecArguments _codecArguments;

        public void Apply(string msg, Inputs.InputListener listener)
        {
            JObject jobject = JObject.Parse(msg);
            listener.AddDefaultFields(jobject);
            listener.ProcessJson(jobject);
        }

        public JsonCodec(CodecArguments args)
        {
            _codecArguments = args;
        }
    }
}
