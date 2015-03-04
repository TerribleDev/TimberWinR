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
using System.Threading;


namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class TestDynamicBatchCount
    {
        [Test]
        public void TestDynamicBatch()
        {
            var mgr = new Manager();
            mgr.LogfileDir = ".";

            mgr.Config = new Configuration();

            CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

            var cancelToken = cancelTokenSource.Token;

            FakeRediServer fr = new FakeRediServer(cancelToken);

            var redisParams = new RedisOutput();
            redisParams.BatchCount = 10;
            redisParams.MaxBatchCount = 40;
            redisParams.Interval = 100;

            var redisOutput = new Outputs.RedisOutput(mgr, redisParams, cancelToken);


            // Message is irrelavant
            JObject jsonMessage = new JObject
            {               
                {"type", "Win32-FileLog"},
                {"ComputerName", "dev.vistaprint.net"},
                {"Text", "{\"Email\":\"james@example.com\",\"Active\":true,\"CreatedDate\":\"2013-01-20T00:00:00Z\",\"Roles\":[\"User\",\"Admin\"]}"}
            };
            
                     
            // Send 1000 messages at max throttle
            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);
                redisOutput.Startup(jsonMessage);             
            }

            while (redisOutput.SentMessages < 1000)
            {
                System.Diagnostics.Debug.WriteLine(redisOutput.SentMessages);
                Thread.Sleep(1000);
            }

            fr.Shutdown();

            cancelTokenSource.Cancel();

            System.Diagnostics.Debug.WriteLine(redisOutput.ToJson());
            System.Diagnostics.Debug.WriteLine(redisOutput.QueueDepth);

            JObject json = redisOutput.ToJson();
            var mbc = json["redis"]["reachedMaxBatchCount"].Value<int>();
            var sm = json["redis"]["sent_messages"].Value<int>();
            var errs = json["redis"]["errors"].Value<int>();
            var cbc = json["redis"]["currentBatchCount"].Value<int>();
            
            // No errors
            Assert.AreEqual(0, errs);          
              
            // Should have reached max at least 1 time
            Assert.GreaterOrEqual(mbc, 1);

            // Should have sent 1000 messages
            Assert.AreEqual(1000, sm);

            // Should reset back down to original
            Assert.AreEqual(cbc, 10);
        }
    }
}
