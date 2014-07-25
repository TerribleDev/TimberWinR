using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using TimberWinR.Parser;
using Newtonsoft.Json.Linq;

namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class GrokFilterTests
    {
        [Test]
        public void TestMatch()
        {
            JObject json = new JObject
            {
                {"LogFilename", @"C:\\Logs1\\test1.log"},
                {"Index", 7},
                {"Text", null},
                {"type", "Win32-FileLog"},
                {"ComputerName", "dev.vistaprint.net"}
            };

           string grokJson = @"{  
                ""TimberWinR"":{        
                    ""Filters"":[  
	                    {  
		                ""grok"":{  
		                    ""condition"": ""[type] == \""Win32-FileLog\"""",
		                    ""match"":[  
			                    ""Text"",
			                    """"
		                    ],
		                    ""add_field"":[  
			                    ""host"",
			                    ""%{ComputerName}""
		                    ]
		                }
	                    }]
                    }
                }";

            Configuration c = Configuration.FromString(grokJson);

            Grok grok = c.Filters.First() as Grok;

            Assert.IsTrue(grok.Apply(json));

            Assert.AreEqual(json["host"].ToString(), "dev.vistaprint.net");
        }
    }
}
