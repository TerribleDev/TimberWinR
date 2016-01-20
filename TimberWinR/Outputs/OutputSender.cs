using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using TimberWinR.Diagnostics;
using TimberWinR.Inputs;

namespace TimberWinR.Outputs
{
    public abstract class OutputSender : IDiagnosable
    {
        public CancellationToken CancelToken { get; private set; }        
        private List<InputListener> _inputs;
        public string Name { get; set; }
      
        public OutputSender(CancellationToken cancelToken, string name)
        {
            CancelToken = cancelToken;
            Name = name;
            _inputs = new List<InputListener>();
        }

        public void Connect(InputListener listener)
        {
            listener.OnMessageRecieved += MessageReceivedHandler;            
        }

        public void Startup(JObject json)
        {
            MessageReceivedHandler(json);
        }

        public abstract JObject ToJson();
        protected abstract void MessageReceivedHandler(JObject jsonMessage);
    }
}
