using System.Runtime.InteropServices;
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
        private string _computerName;
        private string _typeName;
       
        public InputListener(CancellationToken token, string typeName)
        {
            this.CancelToken = token;
            this._typeName = typeName;
            this._computerName = System.Environment.MachineName + "." +
                         Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                             @"SYSTEM\CurrentControlSet\services\Tcpip\Parameters")
                             .GetValue("Domain", "")
                             .ToString();    
        }

        private void AddDefaultFileds(JObject json)
        {
            if (json["type"] == null)
                json.Add(new JProperty("type", _typeName));

            if (json["host"] == null)
                json.Add(new JProperty("host", _computerName));
        }

        protected void ProcessJson(JObject json)
        {
            if (OnMessageRecieved != null)
            {
                AddDefaultFileds(json);
                OnMessageRecieved(json);               
            }
        }
    }
}
