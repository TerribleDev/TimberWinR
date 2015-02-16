using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using TimberWinR.Inputs;
using TimberWinR.Parser;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace TimberWinR.UnitTests
{
    [TestFixture]
    public class TailFileTests
    {
        [Test]
        public void TestTailFile()
        {
            List<JObject> events = new List<JObject>();

            if (File.Exists(".timberwinrdb"))
                File.Delete(".timberwinrdb");

            var mgr = new Manager();
            mgr.LogfileDir = ".";

            var tf = new TailFile();
            var cancelTokenSource = new CancellationTokenSource();
            tf.Location = "TestTailFile1.log";

   
            if (File.Exists(tf.Location))
                File.Delete(tf.Location);

            try
            {
                var listener = new TailFileListener(tf, cancelTokenSource.Token);

                listener.OnMessageRecieved += o =>
                {
                    events.Add(o);
                    if (events.Count >= 100)
                        cancelTokenSource.Cancel();
                };

                GenerateLogFile(tf.Location);

                bool createdFile = false;
                while (!listener.Stop && !cancelTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                    if (!createdFile)
                    {
                        GenerateLogFile(tf.Location);
                        createdFile = true;
                    }
                }
            }
            catch (OperationCanceledException oex)
            {
                Console.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

            }
            finally
            {
                Assert.AreEqual(100, events.Count);
            }
        }

        private static void GenerateLogFile(string fileName)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fileName))
            {
                for (int i = 0; i < 100; i++)
                {
                    file.WriteLine("Log Line Number {0}", i);
                }
            }
        }
    }
}
