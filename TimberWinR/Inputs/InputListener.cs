using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NLog;

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

        public string InputType
        {
            get { return _typeName; }
        }

        public abstract JObject ToJson();

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

        protected string ToPrintable(string inputString)
        {
            string asAscii = Encoding.ASCII.GetString(
                                   Encoding.Convert(
                                       Encoding.UTF8,
                                       Encoding.GetEncoding(
                                           Encoding.ASCII.EncodingName,
                                           new EncoderReplacementFallback(string.Empty),
                                           new DecoderExceptionFallback()
                                           ),
                                       Encoding.UTF8.GetBytes(inputString)
                                   )
                               );
            return asAscii;
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
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error("Error Deleting Checkpoint File", ex);
            }          
        }

        protected virtual void AddDefaultFields(JObject json)
        {
            if (json["type"] == null)
                json.Add(new JProperty("type", _typeName));

            if (json["host"] == null)
                json.Add(new JProperty("host", _computerName));

            if (json["@version"] == null)
                json.Add(new JProperty("@version", 1));

            DateTime utc = DateTime.UtcNow;

            if (json["@timestamp"] == null)
                json.Add(new JProperty("@timestamp", utc.ToString("o")));

            if (json["UtcTimestamp"] == null)
                json.Add(new JProperty("UtcTimestamp", utc.ToString("o")));
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
