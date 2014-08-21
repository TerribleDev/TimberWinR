using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using TimberWinR.Inputs;

namespace TimberWinR.Outputs
{
    public abstract class OutputSender
    {
        public CancellationToken CancelToken { get; private set; }        
        private List<InputListener> _inputs;

        public OutputSender(CancellationToken cancelToken)
        {
            CancelToken = cancelToken;
            _inputs = new List<InputListener>();
        }

        public void Connect(InputListener listener)
        {
            listener.OnMessageRecieved += MessageReceivedHandler;            
        }

        public abstract JObject ToJson();
        protected abstract void MessageReceivedHandler(JObject jsonMessage);
    }
}
