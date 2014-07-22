using Newtonsoft.Json.Linq;
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
        public event Action<JObject> OnMessageRecieved;

       
        public InputListener(CancellationToken token)
        {
            this.CancelToken = token;          
        }


        protected void ProcessJson(JObject json)
        {           
            if (OnMessageRecieved != null)
                OnMessageRecieved(json);
        }
    }
}
