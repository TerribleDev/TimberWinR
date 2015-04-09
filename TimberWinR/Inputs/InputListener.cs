using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
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
        private object _locker = new object();
        public List<string> Files { get; set; }

        public string InputType
        {
            get { return _typeName; }
        }

        public abstract JObject ToJson();

        public InputListener(CancellationToken token, string typeName)
        {
            Files = new List<string>();
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

        public bool HaveSeenFile(string fileName)
        {
            return Files.Contains(fileName);
        }

        protected void SaveVisitedFileName(string fileName)
        {
            lock (_locker)
            {
                if (!HaveSeenFile(fileName))
                    Files.Add(fileName);
            }
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
            LogManager.GetCurrentClassLogger().Info("{0}: Signalling Event Shutdown {1}", Thread.CurrentThread.ManagedThreadId, InputType);
            FinishedEvent.Set();
            LogManager.GetCurrentClassLogger().Info("{0}: Finished signalling Shutdown {1}", Thread.CurrentThread.ManagedThreadId, InputType);
        }

        public virtual void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("{0}: Shutting Down {1}", Thread.CurrentThread.ManagedThreadId, InputType);

            FinishedEvent.WaitOne();
          
            try
            {
                if (File.Exists(CheckpointFileName))
                    File.Delete(CheckpointFileName);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }          
        }

        protected void EnsureRollingCaught()
        {
            try
            {
                const string mteKey = @"SYSTEM\CurrentControlSet\Control\FileSystem";

                var mte = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(mteKey).GetValue("MaximumTunnelEntries");
                if (mte == null || (int)mte != 0)
                {
                    LogManager.GetCurrentClassLogger()
                        .Error(
                            "HKLM\\{0}\\MaximumTunnelEntries is not set to accurately detect log rolling, a DWORD value of 0 is required.",
                            mteKey);
                    Microsoft.Win32.Registry.LocalMachine.CreateSubKey(mteKey).SetValue("MaximumTunnelEntries", 0, RegistryValueKind.DWord);
                    LogManager.GetCurrentClassLogger()
                      .Error(
                          "HKLM\\{0}\\MaximumTunnelEntries is now set to 0, A reboot is now required to fix this issue.  See http://support.microsoft.com/en-us/kb/172190 for details",
                          mteKey);
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }           
        }

        public virtual void AddDefaultFields(JObject json)
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

        public void ProcessJson(JObject json)
        {
            if (OnMessageRecieved != null)
            {
                AddDefaultFields(json);
                OnMessageRecieved(json);               
            }
        }
    }
}
