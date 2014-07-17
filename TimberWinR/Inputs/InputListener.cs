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

        public FieldDefinitions Fields { get; set; }
        public ParameterDefinitions Parameters { get; set; }
      
        public InputListener(CancellationToken token, FieldDefinitions fields, ParameterDefinitions parms)
        {
            this.CancelToken = token;
            Parameters = parms;
            Fields = fields;
        }

        protected void ProcessJson(string message)
        {
            if (OnMessageRecieved != null)
                OnMessageRecieved(message);
        }
    }
}
