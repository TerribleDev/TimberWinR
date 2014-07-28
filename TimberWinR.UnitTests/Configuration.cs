using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimberWinR;
using TimberWinR.Inputs;
using TimberWinR.Filters;
using Newtonsoft.Json.Linq;

namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class ConfigurationTest
    {
        [Test]
        public void TestInvalidMatchConfig()
        {           
            string grokJson = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""grok"":{  
		                ""condition"": ""[type] == \""Win32-FileLog\"""",
		                ""match"":[  
			                ""Text""			               
		                ]                       
		              }
	                }]
                }
            }";

            try
            {
                Configuration c = Configuration.FromString(grokJson);
                Assert.IsTrue(false, "Should have thrown an exception");
            }
            catch (TimberWinR.Parser.Grok.GrokFilterException ex)
            {               
            }          
        }

        [Test]
        public void TestInvalidAddTagConfig()
        {
            string grokJson = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""grok"":{  
		                ""condition"": ""[type] == \""Win32-FileLog\"""",
		                ""match"":[  
			                ""Text"", """"			               
		                ],
                      ""add_tag"": [
                           ""rn_%{Index}"",
                         ],                   
		              }
	                }]
                }
            }";

            try
            {
                Configuration c = Configuration.FromString(grokJson);
                Assert.IsTrue(false, "Should have thrown an exception");
            }
            catch (TimberWinR.Parser.Grok.GrokAddTagException ex)
            {
            }
        }        

    }
}
