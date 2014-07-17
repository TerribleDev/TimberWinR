using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimberWinR;

namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class ConfigurationTest
    {
        [Test]
        public void Test1()
        {
            Configuration c = new Configuration("testconf.xml");
            Console.WriteLine(c.Logs.ToArray());
            Assert.AreEqual(c.Logs.ToArray()[1].Name, "Second Set");
        }
    }
}
