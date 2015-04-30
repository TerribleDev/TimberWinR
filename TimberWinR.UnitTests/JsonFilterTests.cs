using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class JsonFilterTests
    {       
        [Test]
        public void TestDropConditions()
        {
            JObject jsonInputLine1 = new JObject
            {               
                {"type", "Win32-FileLog"},
                {"ComputerName", "dev.mycompany.net"},
                {"Text", "{\"Email\":\"james@example.com\",\"Active\":true,\"CreatedDate\":\"2013-01-20T00:00:00Z\",\"Roles\":[\"User\",\"Admin\"]}"}
            };

            JObject jsonInputLine2 = new JObject
            {               
                {"type", "Win32-FileLog"},
                {"ComputerName", "dev.mycompany.net"},
                {"Text", "{\"Email\":\"james@example.com\",\"Active\":true,\"CreatedDate\":\"2013-01-20T00:00:00Z\",\"Roles\":[\"User\",\"Admin\"]}"}
            };


            string jsonFilter = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""json"":{  
		                ""type"": ""Win32-FileLog"",
		                ""target"": ""stuff"",
                        ""source"": ""Text""            
		            }
	              }]
                }
            }";

            string jsonFilterNoTarget = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""json"":{  
		                ""type"": ""Win32-FileLog"",		               
                        ""source"": ""Text""            
		            }
	              }]
                }
            }";

            // Positive Tests
            Configuration c = Configuration.FromString(jsonFilter);
            Json jf = c.Filters.First() as Json;
            Assert.IsTrue(jf.Apply(jsonInputLine1));

            JObject stuff = jsonInputLine1["stuff"] as JObject;
            Assert.IsNotNull(stuff);

            // 4 fields, Email, Active, CreatedDate, Roles
            Assert.AreEqual(4, stuff.Count);

            // Now, merge it into the root (starts as 3 fields, ends up with 7 fields)
            Assert.AreEqual(3, jsonInputLine2.Count);
            c = Configuration.FromString(jsonFilterNoTarget);
            jf = c.Filters.First() as Json;
            Assert.IsTrue(jf.Apply(jsonInputLine2));
            JObject nostuff = jsonInputLine2["stuff"] as JObject;
            Assert.IsNull(nostuff);
            Assert.AreEqual(6, jsonInputLine2.Count);            
        }

        [Test]
        public void TestRenameAndAdds()
        {
            JObject jsonInputLine1 = new JObject
            {               
                {"type", "Win32-FileLog"},
                {"ComputerName", "dev.mycompany.net"},
                {"Text", "{\"log4net:Username\" : \"NT AUTHORITY\",\"Email\":\"james@example.com\",\"Active\":true,\"CreatedDate\":\"2013-01-20T00:00:00Z\",\"Roles\":[\"User\",\"Admin\"]}"}
            };


            string jsonFilter = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""json"":{  
		                ""type"": ""Win32-FileLog"",		               
                        ""source"": ""Text"",
                        ""promote"": ""Text"",
                        ""add_field"":[  
                            ""test1"",
                            ""value1"",
                            ""test2"",
                            ""value2""
                        ],       
                        ""rename"":[  
                            ""Active"",
                            ""active"",
                            ""log4net:Username"",
                            ""lusername""
                        ]          
		            }
	              }]
                }
            }";
            

            // Positive Tests
            Configuration c = Configuration.FromString(jsonFilter);
            Json jf = c.Filters.First() as Json;
            Assert.IsTrue(jf.Apply(jsonInputLine1));

            Assert.AreEqual("NT AUTHORITY", jsonInputLine1["lusername"].ToString());
            Assert.AreEqual("True", jsonInputLine1["active"].ToString());
            Assert.IsNotNull(jsonInputLine1["test1"]);
            Assert.IsNotNull(jsonInputLine1["test2"]);
            Assert.AreEqual("value1", jsonInputLine1["test1"].ToString());
            Assert.AreEqual("value2", jsonInputLine1["test2"].ToString());
        }
 
    }
}
