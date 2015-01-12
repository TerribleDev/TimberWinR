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
    public class MultilineTests
    {
     //   [Test(Description = "Test using next")]
        public void TestMultiline1()
        {
            using (StreamReader sr = new StreamReader("Multiline1.txt"))
            {
                List<JObject> events = new List<JObject>();

                Console.SetIn(sr);

                Stdin sin = new Stdin();

                sin.Codec = new Codec();
                sin.Codec.Pattern = "\\\\$";
                sin.Codec.What = Codec.WhatType.next;
                sin.Codec.Type = Codec.CodecType.multiline;

                var cancelTokenSource = new CancellationTokenSource();

                using (var syncHandle = new ManualResetEventSlim())
                {
                    try
                    {
                        StdinListener sl = new StdinListener(sin, cancelTokenSource.Token);

                        sl.OnMessageRecieved += o =>
                        {
                            events.Add(o);
                            if (events.Count >= 6)
                                cancelTokenSource.Cancel();
                        };

                        if (!cancelTokenSource.Token.IsCancellationRequested)
                            syncHandle.Wait(TimeSpan.FromSeconds(10000), cancelTokenSource.Token);
                    }
                    catch (OperationCanceledException oex)
                    {
                    }
                }

                Assert.AreEqual(events.Count, 6);
                Assert.AreEqual(events[0]["message"].ToString(), "multiline1 \\\nml1_1 \\\nml1_2 \\\nml1_2 ");
                Assert.AreEqual(events[1]["message"].ToString(), "singleline1");
                Assert.AreEqual(events[2]["message"].ToString(), "singleline2");
                Assert.AreEqual(events[3]["message"].ToString(), "multiline2 \\\nml2_1 \\\nml2_2");
                Assert.AreEqual(events[4]["message"].ToString(), "multiline3 \\\nml3_1 \\\nml3_2");
                Assert.AreEqual(events[5]["message"].ToString(), "singleline3");
            }
        }

    //    [Test(Description = "Test using previous")]
        public void TestMultiline2()
        {
            using (StreamReader sr = new StreamReader("Multiline2.txt"))
            {
                List<JObject> events = new List<JObject>();

                Console.SetIn(sr);

                Stdin sin = new Stdin();

                sin.Codec = new Codec();
                sin.Codec.Pattern = "^(\\d{4}-\\d{2}-\\d{2}\\s\\d{2}:\\d{2}:\\d{2},\\d{3})(.*)$";              
                sin.Codec.What = Codec.WhatType.previous;
                sin.Codec.Type = Codec.CodecType.multiline;
                sin.Codec.Negate = true;

                var cancelTokenSource = new CancellationTokenSource();

                using (var syncHandle = new ManualResetEventSlim())
                {
                    try
                    {
                        StdinListener sl = new StdinListener(sin, cancelTokenSource.Token);

                        sl.OnMessageRecieved += o =>
                        {
                            events.Add(o);
                            if (events.Count >= 4)
                                cancelTokenSource.Cancel();
                        };

                        if (!cancelTokenSource.Token.IsCancellationRequested)
                            syncHandle.Wait(TimeSpan.FromSeconds(10000), cancelTokenSource.Token);
                    }
                    catch (OperationCanceledException oex)
                    {
                    }
                }

                Assert.AreEqual(events.Count, 4);
                Assert.AreEqual(events[0]["message"].ToString(), "2015-01-07 13:14:26,572 TEST DEBUG [THREAD : 25] - Sending message to TServer - tcp://10.1111.11.111:1111\n'RequestAttachUserData' ('30')\nmessage attributes:\nAttributeConnID [long] = 00890\nAttributeReferenceID [int] = 88\nAttributeThisDN [str] = \"2214\"\nAttributeUserData [bstr] = KVList: \n\t\t'ActivityID' [str] = \"1-XXXXXX\"");
                Assert.AreEqual(events[1]["message"].ToString(), "2015-01-07 13:14:26,574 TEST DEBUG [THREAD : 25] - Writing message RequestAttachUserData in 'proxy1' via '.StatePrimary proxy: proxy1'");
                Assert.AreEqual(events[2]["message"].ToString(), "2015-01-07 13:14:26,575 TEST DEBUG [THREAD : 25] - sending RequestAttachUserData to Test.Platform.Commons.Connection.CommonConnection");
                Assert.AreEqual(events[3]["message"].ToString(), "2015-01-07 13:20:31,665 TEST DEBUG [THREAD : SelectorThread] - Proxy got message 'EventOnHook' ('87')\nmessage attributes:\nAttributeEventSequenceNumber [long] = 4899493\nTime            = ComplexClass(TimeStamp):\n\tAttributeTimeinuSecs [int] = 573000\n\tAttributeTimeinSecs [int] = 1420644031\nAttributeThisDN [str] = \"2214\"\n. Processing with  state .StatePrimary proxy: proxy1");
            }
        }

    }
}
