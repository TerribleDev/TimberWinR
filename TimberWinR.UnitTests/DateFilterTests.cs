using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TimberWinR.Parser;
using Newtonsoft.Json.Linq;

namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class DateFilterTests
    {
        [Test]
        public void TestDate1()
        {
            JObject json = new JObject
            {               
                {"message", "2014-01-31 08:23:47,123"}               
            };

            string grokJson = @"{  
            ""TimberWinR"":{        
                ""Filters"":[  
	                {  
		            ""date"":{  		                
		                ""match"":[  
			                ""message"",
			                ""yyyy-MM-dd HH:mm:ss,fff""
		                ]	               
		            }
	                }]
                }
            }";

            Configuration c = Configuration.FromString(grokJson);

            DateFilter date = c.Filters.First() as DateFilter;

            Assert.IsTrue(date.Apply(json));

            var ts = json["@timestamp"].ToString();

            Assert.AreEqual(ts, "1/31/2014 8:23:47 AM");

        }
    }
}
