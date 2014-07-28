using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using NLog;

namespace TimberWinR.Filters
{
    class EvaluatingFilter
    {
        protected bool EvaluateCondition(JObject json, string condition)
        {
            // Create a new instance of the C# compiler
            var cond = condition;

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
                {
                    LogManager.GetCurrentClassLogger().Error(e);
                    LogManager.GetCurrentClassLogger().Error("Bad Code: {0}", code);
                }
            }

            return false;
        }
    }
}
