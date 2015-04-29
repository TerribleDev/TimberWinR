using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp.Extensions;
using TimberWinR.Codecs;
using TimberWinR.Parser;


namespace TimberWinR.Inputs
{
    public class GeneratorInput : InputListener
    {
        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("message", _params.Message),
                new JProperty("messages", _sentMessages),
                new JProperty("generator", "enabled"));
            return json;
        }

        private TimberWinR.Parser.GeneratorParameters _params;
        private Thread _listenThread;
        private ICodec _codec;
        private int _sentMessages;

        public GeneratorInput(TimberWinR.Parser.GeneratorParameters parameters, CancellationToken cancelToken)
            : base(cancelToken, "Win32-InputGen")
        {
            _params = parameters;

            if (_params.CodecArguments != null)
            {
                switch (_params.CodecArguments.Type)
                {
                    case CodecArguments.CodecType.json:
                        _codec = new JsonCodec(_params.CodecArguments);
                        break;
                    case CodecArguments.CodecType.multiline:
                        _codec = new Multiline(_params.CodecArguments);
                        break;
                    case CodecArguments.CodecType.plain:
                        _codec = new PlainCodec(_params.CodecArguments);
                        break;
                }
            }

            _listenThread = new Thread(new ThreadStart(GenerateData));
            _listenThread.Start();
        }

        private void GenerateData()
        {
            LogManager.GetCurrentClassLogger().Info("Generator Creating {0} Lines", _params.Count);

            int numMessages = _params.Count;
            if (numMessages == 0)
                numMessages = int.MaxValue;

            for (int i = 0; i < numMessages; i++)
            {
                if (CancelToken.IsCancellationRequested)
                    break;

                string msg = ToPrintable(_params.Message);

                if (_codec != null)
                    _codec.Apply(msg, this);
                else
                {
                    JObject jo = new JObject();
                    jo["Message"] = msg;
                    AddDefaultFields(jo);
                    ProcessJson(jo);
                }

                Thread.Sleep(_params.Rate);
            }

            Finished();
        }
    }
}
