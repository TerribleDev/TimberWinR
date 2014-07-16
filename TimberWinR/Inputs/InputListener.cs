using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TimberWinR.Inputs
{
    public abstract class InputListener
    {
        public CancellationToken CancelToken { get; set; }
        public event Action<string> OnMessageRecieved;

        public InputListener(CancellationToken token)
        {
            this.CancelToken = token;
        }

        protected void ProcessMessage(string message)
        {
            if (OnMessageRecieved != null)
                OnMessageRecieved(message);
        }
    }
}
