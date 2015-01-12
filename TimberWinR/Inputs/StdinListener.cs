using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using TimberWinR.Parser;

namespace TimberWinR.Inputs
{
    public class StdinListener : InputListener
    {
        private Thread _listenThread;
        private Codec _codec;
        private List<string> _multiline { get; set; }

        public StdinListener(TimberWinR.Parser.Stdin arguments, CancellationToken cancelToken)
            : base(cancelToken, "Win32-Console")
        {
            _codec = arguments.Codec;
            _listenThread = new Thread(new ThreadStart(ListenToStdin));
            _listenThread.Start();
        }

        public override JObject ToJson()
        {
            JObject json = new JObject(
                new JProperty("stdin", "enabled"));

            
            if (_codec != null)
            {
                var cp = new JProperty("codec",
                    new JArray(
                        new JObject(
                            new JProperty("type", _codec.Type.ToString()),
                            new JProperty("what", _codec.What.ToString()),
                            new JProperty("negate", _codec.Negate),
                            new JProperty("multilineTag", _codec.MultilineTag),
                            new JProperty("pattern", _codec.Pattern))));
                json.Add(cp);              
            }

            return json;
        }

        public override void Shutdown()
        {
            LogManager.GetCurrentClassLogger().Info("Shutting Down {0}", InputType);
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

                    if (_codec != null && _codec.Type == Codec.CodecType.multiline)
                        applyMultilineCodec(msg);
                    else
                    {
                        JObject jo = new JObject();
                        jo["message"] = msg;
                        AddDefaultFields(jo);
                        ProcessJson(jo);
                    }
                }              
            }
            Finished();
        }

        // return true to cancel codec
        private void applyMultilineCodec(string msg)
        {
            if (_codec.Re == null)
                _codec.Re = new Regex(_codec.Pattern);

            Match match = _codec.Re.Match(msg);

            bool isMatch = (match.Success && !_codec.Negate) || (!match.Success && _codec.Negate);

            switch (_codec.What)
            {
                case Codec.WhatType.previous:
                    if (isMatch)
                    {
                        if (_multiline == null)
                            _multiline = new List<string>();

                        _multiline.Add(msg);
                    }
                    else // No Match
                    {
                        if (_multiline != null)
                        {
                            string single = string.Join("\n", _multiline.ToArray());
                            _multiline = null;
                            JObject jo = new JObject();
                            jo["message"] = single;
                            jo.Add("tags", new JArray(_codec.MultilineTag));
                            AddDefaultFields(jo);
                            ProcessJson(jo);                          
                        }
                        _multiline = new List<string>();
                        _multiline.Add(msg);
                    }
                    break;
                case Codec.WhatType.next:
                    if (isMatch)
                    {
                        if (_multiline == null)
                            _multiline = new List<string>();
                        _multiline.Add(msg);
                    }
                    else // No match
                    {
                        if (_multiline != null)
                        {
                            _multiline.Add(msg);
                            string single = string.Join("\n", _multiline.ToArray());
                            _multiline = null;
                            JObject jo = new JObject();
                            jo["message"] = single;
                            jo.Add("tags", new JArray(_codec.MultilineTag));
                            AddDefaultFields(jo);
                            ProcessJson(jo);                          
                        }
                        else
                        {
                            JObject jo = new JObject();
                            jo["message"] = msg;
                            AddDefaultFields(jo);
                            ProcessJson(jo);                          
                        }
                    }
                    break;
            }
        }
    }
}
