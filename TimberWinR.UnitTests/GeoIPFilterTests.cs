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
    public class GeoIPFilterTests
    {       
        [Test]
        public void TestDropConditions()
        {
            JObject jsonInputLine1 = new JObject
            {               
                {"type", "Win32-FileLog"},
                {"IP", "8.8.8.8"}              
            };
           

            string jsonFilter = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""geoip"":{  
		                ""type"": ""Win32-FileLog"",
		                ""target"": ""mygeoip"",
                        ""source"": ""IP""            
		            }
	              }]
                }
            }";
          
            // Positive Tests
            Configuration c = Configuration.FromString(jsonFilter);
            GeoIP jf = c.Filters.First() as GeoIP;
            Assert.IsTrue(jf.Apply(jsonInputLine1));

            JObject stuff = jsonInputLine1["mygeoip"] as JObject;
            Assert.IsNotNull(stuff);
          
            Assert.AreEqual("8.8.8.8", stuff["ip"].ToString());
            Assert.AreEqual("US", stuff["country_code2"].ToString());
            Assert.AreEqual("United States", stuff["country_name"].ToString());
            Assert.AreEqual("CA", stuff["region_name"].ToString());
            Assert.AreEqual("Mountain View", stuff["city_name"].ToString());
            Assert.AreEqual("California", stuff["real_region_name"].ToString());
            Assert.AreEqual(37.386f, (float)stuff["latitude"]);
            Assert.AreEqual(-122.0838f, (float) stuff["longitude"]);
        }
    }
}
