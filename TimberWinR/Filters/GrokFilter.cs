using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using NLog;
using RapidRegex.Core;

namespace TimberWinR.Parser
{
    public class Fields
    {
        private Dictionary<string, string> fields { get; set; }

        public string this[string i]
        {
            get { return fields[i]; }
            set { fields[i] = value; }
        }

        public Fields(JObject json)
        {
            fields = new Dictionary<string, string>();
            IList<string> keys = json.Properties().Select(p => p.Name).ToList();
            foreach (string key in keys)
                fields[key] = json[key].ToString();
        }
    }

    public partial class Grok : LogstashFilter
    {
        public override bool Apply(JObject json)
        {
            if (Condition != null && !EvaluateCondition(json))
                return false;

            if (Matches(json))
            {
                AddFields(json);
                AddTags(json);
                RemoveFields(json);
                RemoveTags(json);
                return true;
            }
            return false;
        }

        private bool EvaluateCondition(JObject json)
        {
            // Create a new instance of the C# compiler
            var cond = Condition;

            IList<string> keys = json.Properties().Select(p => p.Name).ToList();
            foreach (string key in keys)
                cond = cond.Replace(string.Format("[{0}]", key), string.Format("\"{0}\"", json[key].ToString()));

            var compiler = new CSharpCodeProvider();

            // Create some parameters for the compiler
            var parms = new System.CodeDom.Compiler.CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true
            };
            parms.ReferencedAssemblies.Add("System.dll");
            var code = string.Format(@" using System;
                                        class EvaluatorClass
                                        {{
                                            public bool Evaluate()
                                            {{
                                                return {0};
                                            }}               
                                        }}", cond);

            // Try to compile the string into an assembly
            var results = compiler.CompileAssemblyFromSource(parms, new string[] { code });

            // If there weren't any errors get an instance of "MyClass" and invoke
            // the "Message" method on it
            if (results.Errors.Count == 0)
            {
                var evClass = results.CompiledAssembly.CreateInstance("EvaluatorClass");
                var result = evClass.GetType().
                    GetMethod("Evaluate").
                    Invoke(evClass, null);
                return bool.Parse(result.ToString());
            }
            else
            {
                foreach (var e in results.Errors)
                    LogManager.GetCurrentClassLogger().Error(e);
            }

            return false;
        }

        private bool Matches(Newtonsoft.Json.Linq.JObject json)
        {
            string field = Match[0];
            string expr = Match[1];

            JToken token = null;
            if (json.TryGetValue(field, out token))
            {
                string text = token.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    var resolver = new RegexGrokResolver();
                    var pattern = resolver.ResolveToRegex(expr);
                    var match = Regex.Match(text, pattern);
                    if (match.Success)
                    {
                        var regex = new Regex(pattern);
                        var namedCaptures = regex.MatchNamedCaptures(text);
                        foreach (string fieldName in namedCaptures.Keys)
                        {
                            AddOrModify(json, fieldName, namedCaptures[fieldName]);
                        }
                        return true; // Yes!
                    }
                }
                return true; // Empty field is no match
            }
            return false; // Not specified is failure
        }

        private void AddFields(Newtonsoft.Json.Linq.JObject json)
        {
            if (AddField != null && AddField.Length > 0)
            {
                for (int i = 0; i < AddField.Length; i += 2)
                {
                    string fieldName = ExpandField(AddField[i], json);
                    string fieldValue = ExpandField(AddField[i + 1], json);
                    AddOrModify(json, fieldName, fieldValue);
                }
            }
        }

        private void RemoveFields(Newtonsoft.Json.Linq.JObject json)
        {
            if (RemoveField != null && RemoveField.Length > 0)
            {
                for (int i = 0; i < RemoveField.Length; i++)
                {
                    string fieldName = ExpandField(RemoveField[i], json);
                    RemoveProperties(json, new string[] { fieldName });
                }
            }
        }

        private void AddTags(Newtonsoft.Json.Linq.JObject json)
        {
            if (AddTag != null && AddTag.Length > 0)
            {
                for (int i = 0; i < AddTag.Length; i++)
                {
                    string value = ExpandField(AddTag[i], json);

                    JToken tags = json["tags"];
                    if (tags == null)
                        json.Add("tags", new JArray(value));
                    else
                    {
                        JArray a = tags as JArray;
                        a.Add(value);
                    }
                }
            }
        }

        private void RemoveTags(Newtonsoft.Json.Linq.JObject json)
        {
            if (RemoveTag != null && RemoveTag.Length > 0)
            {
                JToken tags = json["tags"];
                if (tags != null)
                {
                    List<JToken> children = tags.Children().ToList();                                      
                    for (int i = 0; i < RemoveTag.Length; i++)
                    {
                        string tagName = ExpandField(RemoveTag[i], json);       
                        foreach(JToken token in children)
                        {
                            if (token.ToString() == tagName)
                                token.Remove();
                        }
                    }                   
                }               
            }
        }
    }
}
