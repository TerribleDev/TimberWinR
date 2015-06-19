using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;

using NLog;
using TimberWinR.Parser;

namespace TimberWinR.Diagnostics
{
    public class Diagnostics
    {
        private CancellationToken CancelToken { get; set; }
        public int Port { get; set; }
        public Manager Manager { get; set; }
        public bool Stop { get; set; }
        private HttpListener web;

        public Diagnostics(Manager manager, CancellationToken cancelToken, int port = 5141)
        {
            Port = port;
            CancelToken = cancelToken;
            Manager = manager;

            LogManager.GetCurrentClassLogger().Info("Diagnostic(v4/v6) on Port {0} Ready", Port);

            var hl = new Thread(new ParameterizedThreadStart(HttpListen));
            hl.Start(null);
        }

        void processRequest()
        {
            var result = web.BeginGetContext(DiagnosticCallback, web);
            result.AsyncWaitHandle.WaitOne();
        }

        private Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                   SingleOrDefault(assembly => assembly.GetName().Name == name);
        }


        public JObject DiagnosticsOutput()
        {
            JObject json = new JObject(
                new JProperty("timberwinr",
                    new JObject(
                        new JProperty("version", Assembly.GetEntryAssembly().GetName().Version.ToString()),
                        new JProperty("messages", Manager.NumMessages),
                        new JProperty("startedon", Manager.StartedOn),
                        new JProperty("configfile", Manager.JsonConfig),
                        new JProperty("logdir", Manager.LogfileDir),
                        new JProperty("logginglevel", LogManager.GlobalThreshold.ToString())
                        )));
            AddDiagnosis(json);
            return json;
        }

        protected void AddDiagnosis(JObject wrapper)
        {
            wrapper.Add("inputs", GetDiagnosisByType("inputs", Manager.Listeners.ToList<IDiagnosable>()));
            wrapper.Add("filters", GetDiagnosisByType("inputs", Manager.Config.Filters.ToList<IDiagnosable>()));
            wrapper.Add("outputs", GetDiagnosisByType("inputs", Manager.Outputs.ToList<IDiagnosable>()));
        }

        protected JObject GetDiagnosisByType(String type, List<IDiagnosable> diags)
        {
            JObject category = new JObject();
            foreach(IDiagnosable diag in diags)
            {
                JArray array = GetTypeArray(diag.GetType().Name.ToString(), category);
                array.Add(diag.ToJson());
            }
            return category;
        }

        protected JArray GetTypeArray(String name, JObject category)
        {
            JArray ret = (JArray)category.GetValue(name);
            if (ret == null)
            {
                ret = new JArray();
                category.Add(new JProperty(name, ret));
            }
            return ret;
        }

        private void DiagnosticCallback(IAsyncResult result)
        {
            if (web == null)
                return;

            try
            {
                var context = web.EndGetContext(result);
                var response = context.Response;
                var json = DiagnosticsOutput();

                response.StatusCode = (int) HttpStatusCode.OK;
                response.StatusDescription = HttpStatusCode.OK.ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(json.ToString());
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (SocketException)
            {
                // Shutdown
            }
            catch (Exception ex)
            {
                if (!Stop)
                    LogManager.GetCurrentClassLogger().Error(ex);
            }
        }

        private void HttpListen(object o)
        {
            web = new HttpListener();
            try
            {
                web.Prefixes.Add(string.Format("http://*:{0}/", Port));
                web.Start();

                while (web != null && web.IsListening)
                {
                    processRequest();
                }
            }
            catch (SocketException)
            {
                // Shutdown
            }
            catch (Exception ex)
            {
                if (!Stop)
                LogManager.GetCurrentClassLogger().Error("Diagnostic Listener Error: {0}", ex.ToString());
            }
        }

        private void ListenForClients(object olistener)
        {
            var listener = olistener as System.Net.Sockets.TcpListener;

            listener.Start();

            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    //blocks until a client has connected to the server
                    TcpClient client = listener.AcceptTcpClient();

                    // Wait for a client, spin up a thread.
                    var clientThread = new Thread(new ParameterizedThreadStart(HandleNewClient));
                    clientThread.Start(client);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        break;
                    else
                        LogManager.GetCurrentClassLogger().Error(ex);
                }
            }
        }


        private void HandleNewClient(object client)
        {
            var tcpClient = (TcpClient)client;
            NetworkStream clientStream = null;

            //     Console.WriteLine("Handle new diag client: {0}, {1}", tcpClient.Connected, tcpClient.Client.RemoteEndPoint.ToString());
            try
            {
                using (clientStream = tcpClient.GetStream())
                {
                    StreamWriter sw = new StreamWriter(clientStream);
                    JObject json = new JObject(
                        new JProperty("timberwinr",
                            new JObject(
                                new JProperty("messages", Manager.NumMessages),
                                new JProperty("startedon", Manager.StartedOn),
                                new JProperty("configfile", Manager.JsonConfig),
                                new JProperty("logdir", Manager.LogfileDir),
                                new JProperty("logginglevel", LogManager.GlobalThreshold.ToString()),
                                new JProperty("inputs",
                                    new JArray(
                                        from i in Manager.Listeners
                                        select new JObject(i.ToJson()))),
                                new JProperty("filters",
                                    new JArray(
                                        from f in Manager.Config.Filters
                                        select new JObject(f.ToJson()))),
                                new JProperty("outputs",
                                    new JArray(
                                        from o in Manager.Outputs
                                        select new JObject(o.ToJson()))))));



                    sw.WriteLine(json.ToString());
                    sw.Flush();
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex);
            }

        }

        public void Shutdown()
        {
            Stop = true;
            try
            {
                if (web != null && web.IsListening)
                {
                    LogManager.GetCurrentClassLogger().Info("Shutting down diagnostics listener");
                    web.Close();
                    web = null;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
