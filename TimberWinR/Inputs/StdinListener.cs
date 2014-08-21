using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Inputs
{
    public class StdinListener : InputListener
    {
        private Thread _listenThread;

        public StdinListener(CancellationToken cancelToken)
            : base(cancelToken, "Win32-Console")
        {
            _listenThread = new Thread(new ThreadStart(ListenToStdin));
            _listenThread.Start();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("stdin", "enabled"));                 
            return json;
        }

        public override void Shutdown()
        {           
            base.Shutdown();
        }

        private void ListenToStdin()
        {
            LogManager.GetCurrentClassLogger().Info("StdIn Ready");

            while (!CancelToken.IsCancellationRequested)
            {                       
                string line = Console.ReadLine();
                if (line != null)
                {
                    string msg = ToPrintable(line);
                    JObject jo = new JObject();
                    jo["message"] = msg;
                    AddDefaultFields(jo);                                    
                    ProcessJson(jo);
                }
                else              
                    break;              
            }
            Finished();
        }
    }
}
