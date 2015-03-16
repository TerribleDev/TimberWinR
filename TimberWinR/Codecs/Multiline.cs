using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TimberWinR.Inputs;
using TimberWinR.Parser;

namespace TimberWinR.Codecs
{
    public class Multiline : ICodec
    {
        private CodecArguments _codecArguments;
        private List<string> _multiline { get; set; }

        // return true to cancel codec
        public Multiline(CodecArguments args)
        {
            _codecArguments = args;
        }

        public void Apply(string msg, InputListener listener)
        {
            if (_codecArguments.Re == null)
                _codecArguments.Re = new Regex(_codecArguments.Pattern);

            Match match = _codecArguments.Re.Match(msg);

            bool isMatch = (match.Success && !_codecArguments.Negate) || (!match.Success && _codecArguments.Negate);

            switch (_codecArguments.What)
            {
                case CodecArguments.WhatType.previous:
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
                            jo.Add("tags", new JArray(_codecArguments.MultilineTag));
                            listener.AddDefaultFields(jo);
                            listener.ProcessJson(jo);
                        }
                        _multiline = new List<string>();
                        _multiline.Add(msg);
                    }
                    break;
                case CodecArguments.WhatType.next:
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
                            jo.Add("tags", new JArray(_codecArguments.MultilineTag));
                            listener.AddDefaultFields(jo);
                            listener.ProcessJson(jo);
                        }
                        else
                        {
                            JObject jo = new JObject();
                            jo["message"] = msg;
                            listener.AddDefaultFields(jo);
                            listener.ProcessJson(jo);
                        }
                    }
                    break;
            }
        }
    }
}
