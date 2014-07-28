using System.IO;
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
        public AutoResetEvent FinishedEvent { get; set; }
        public string CheckpointFileName { get; set; }
       
        public InputListener(CancellationToken token, string typeName)
        {
            CheckpointFileName = Path.Combine(System.IO.Path.GetTempPath(), string.Format("{0}.lpc", Guid.NewGuid().ToString()));          
          
            this.FinishedEvent = new AutoResetEvent(false);
            this.CancelToken = token;
            this._typeName = typeName;
            this._computerName = System.Environment.MachineName + "." +
                         Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                             @"SYSTEM\CurrentControlSet\services\Tcpip\Parameters")
                             .GetValue("Domain", "")
                             .ToString();    
        }

        public void Finished()
        {
            FinishedEvent.Set();
        }
        public virtual void Shutdown()
        {
            FinishedEvent.WaitOne();
            try
            {
                if (File.Exists(CheckpointFileName))
                    File.Delete(CheckpointFileName);
            }
            catch (Exception)
            {               
            }          
        }

        private void AddDefaultFields(JObject json)
        {
            if (json["type"] == null)
                json.Add(new JProperty("type", _typeName));

            if (json["host"] == null)
                json.Add(new JProperty("host", _computerName));

            if (json["@timestamp"] == null)
                json.Add(new JProperty("@timestamp", DateTime.UtcNow));
        }

        protected void ProcessJson(JObject json)
        {
            if (OnMessageRecieved != null)
            {
                AddDefaultFields(json);
                OnMessageRecieved(json);               
            }
        }
    }
}
