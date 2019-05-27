using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Newtonsoft.Json;
using TestHelpers;
using System.Threading;

namespace DaytonaTests
{
    using NetMQ;
    using NetMQ.Devices;
    using NetMQ.zmq;
    using System.Collections.Generic;
    using System.Diagnostics;

    [TestClass]
    public class PipeTests
    {
        [TestMethod]
        public void SendOneMessage()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = "ZZZ";
            var count = 2;

            using (var context = NetMQContext.Create())
            {
                var xForwarder = new XForwarder(
                    context,
                    Exchange.PublishAddressServer,
                    Exchange.SubscribeAddressServer,
                    DeviceMode.Threaded);
                xForwarder.Start();
                using (var pub = Helper.GetConnectedPublishSocket(context))
                {
                    using (var sub = Helper.GetConnectedSubscribeSocket(context))
                    {
                        Thread.Sleep(50);
                        Helper.SendOneSimpleMessage(expectedAddress, message, pub);

                        var netMQMessage = Helper.ReceiveMessage(sub);

                        Assert.AreEqual(count, netMQMessage.FrameCount);
                        NetMQFrame frame = netMQMessage[0];
                        var address = Encoding.Unicode.GetString(frame.Buffer);
                        Assert.AreEqual(expectedAddress, address);
                        
                    }
                }
              
                xForwarder.Stop();
            }
        }

         [TestMethod]
        public void SendOneMessage_inProc()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = "ZZZ";
            var count = 2;

            using (var context = NetMQContext.Create())
            {
                var exchange = new XForwarder(
                    context,
                    "inproc://frontend",
                    "inproc://SubscribeAddress",
                    DeviceMode.Threaded);
                exchange.Start();
                using (var pub = Helper.GetConnectedPublishSocket(context, "inproc://frontend"))
                {
                    using (var sub = Helper.GetConnectedSubscribeSocket(context, "inproc://SubscribeAddress"))
                    {
                        Thread.Sleep(50);
                        Helper.SendOneSimpleMessage(expectedAddress, message, pub);

                        var netMQMessage = Helper.ReceiveMessage(sub);

                        Assert.AreEqual(count, netMQMessage.FrameCount);
                        NetMQFrame frame = netMQMessage[0];
                        var address = Encoding.Unicode.GetString(frame.Buffer);
                        Assert.AreEqual(expectedAddress, address);

                    }
                }

                exchange.Stop();
            }
        }

        [TestMethod]
        public void InProcOnlyWithXForwarder()
        {
            string expectedAddress = "XXXX";
            string message = "hello its me";
            int count = 0;
            using (var context = NetMQContext.Create())
            {
                var xForwarder = new XForwarder(context, "inproc://frontend", "inproc://SubscribeAddress", DeviceMode.Threaded);
                xForwarder.Start();
                var queueDevice = new QueueDevice(
                    context,
                    Exchange.PubSubControlBackAddressServer,
                    Exchange.PubSubControlFrontAddressServer,
                    DeviceMode.Threaded);
                queueDevice.Start();

                Task.Run(() =>
                {
                    return RunSubscriber(context);
                });

                using (NetMQSocket pub = Helper.GetConnectedPublishSocket(context, "inproc://frontend"), 
                   syncService = context.CreateResponseSocket())
                {
                    syncService.Connect(Exchange.PubSubControlFrontAddressClient);
                    for (int i = 0; i < 1; i++)
                    {
                        var received = syncService.Receive();
                        syncService.Send("");
                    }
    
                    Helper.SendOneSimpleMessage(expectedAddress, message, pub);
                           

                }

                xForwarder.Stop(true);
                queueDevice.Stop(true);
            }          
        }

        private Task RunSubscriber(NetMQContext context)
        {
            using (NetMQSocket sub = Helper.GetConnectedSubscribeSocket(context, "inproc://SubscribeAddress"),
                syncClient = context.CreateRequestSocket())
            {
                syncClient.Connect(Exchange.PubSubControlBackAddressClient);
                syncClient.Send("");
                syncClient.Receive();
                NetMQMessage NetMQMessage = null;
                while (NetMQMessage == null)
                {
                     NetMQMessage = Helper.ReceiveMessage(sub);
                }

                Assert.AreEqual(2, NetMQMessage.FrameCount);
                NetMQFrame frame = NetMQMessage[0];
                var address = Encoding.Unicode.GetString(frame.Buffer);
                Assert.AreEqual("XXXX", address);
            }
            return null;
        }
    


        private static async Task<NetMQMessage> LoopReceiver(NetMQSocket sub)
        {
            NetMQMessage NetMQMessage = null;
            while (NetMQMessage == null)
            {
                NetMQMessage = Helper.ReceiveMessage(sub);
                //isReady = true;
            }
            return NetMQMessage;
        }

        static bool interupt = false;
        
        //[TestMethod]
        //[Ignore]
        //public void SendOneMessageUsingSpawn()
        //{
        //    Thread.Sleep(5000);
        //    string passedMessage = string.Empty;

        //    using (var DaytonaContext = Context.Create(new ConsoleMonitor()))
        //    {
        //        var actorReference = DaytonaContext.Spawn("Johnny", (message, sender, actor) =>
        //        {
        //            passedMessage = (string)message;
        //            Console.WriteLine($"here this is the message:{message}");
        //            Console.WriteLine("here this is the Sender:{0}##", sender.ReturnedAddress);
        //            Console.WriteLine("hey is there enything there##{0}##", actor.Name);


        //            //No point asserting here it will get swallowed
        //            //Assert.AreEqual("hello", message);
        //        });
               
        //        Thread.Sleep(20);
        //        actorReference.Tell("hello");
        //        Thread.Sleep(20);
        //        actorReference.Kill();
        //    }

        //    Console.WriteLine($"hello = {passedMessage}");
        //    Assert.AreEqual("hello", passedMessage);
        //}

        [TestMethod]
        public void SendFiveMessagesUsingSpawn()
        {
            Thread.Sleep(5000);
            string passedMessage = string.Empty;
            List<string> receivedMessages = new List<string>();

            using (var context = Context.Create(new ConsoleMonitor()))
            {
                var actorReference = context.Spawn("Johnny", (message, sender, actor) =>
                {
                    passedMessage = (string)message;
                    receivedMessages.Add(passedMessage);
                    Console.WriteLine($"here this is the message:{message}");
                    Console.WriteLine("here this is the Sender:{0}##", sender.ReturnedAddress);
                    Console.WriteLine("hey is there enything there##{0}##", actor.Name);

                    int c = 0;
                    object count = 0;
                    if (actor.PropertyBag.TryGetValue("Counter", out count))
                    {
                        c = (int)count;
                        count = ++c;
                        actor.PropertyBag["Counter"] = count;
                    }
                    else
                    {
                        c = 0;
                        actor.PropertyBag.Add("Counter", c);
                    }


                });

                Thread.Sleep(20);
                actorReference.Tell(1);
                actorReference.Tell(2);
                actorReference.Tell(3);
                actorReference.Tell(4);
                Thread.Sleep(20);
                actorReference.Kill();
            }

            Console.WriteLine($"message = {passedMessage}");

            var i = 0;
            foreach (var item in receivedMessages)
            {
                i++;
                Assert.AreEqual($"{i}", item);
            }
        }


        [TestMethod]
        [TestCategory("DoNotRunOnServer")]
        public void SendAlotOfMessagesUsingSpawn()
        {
            string passedMessage = string.Empty;
            List<string> receivedMessages = new List<string>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var context = Context.Create(new ConsoleMonitor()))
            {
                var actorReference = context.Spawn("Johnny", (message, sender, actor) =>
                {
                    passedMessage = (string)message.ToString();
                    receivedMessages.Add(passedMessage);
                    Console.WriteLine($"This is the message:{message}");
                    Console.WriteLine($"Sender:{sender.ReturnedAddress}##");
                    Console.WriteLine($"Actor Name:{actor.Name}##");

                    int c = 0;
                    object count = 0;
                    if (actor.PropertyBag.TryGetValue("Counter", out count))
                    {
                        c = (int)count;
                        count = ++c;
                        actor.PropertyBag["Counter"] = count;
                    }
                    else
                    {
                        c = 0;
                        actor.PropertyBag.Add("Counter", c);
                    }

                    if (c == 1000)
                    {
                        stopWatch.Stop();
                    }

                });

                Thread.Sleep(20);
                for (int j = 0; j < 1000; j++)
                {
                    actorReference.Tell(j);
                }

                Thread.Sleep(20);
                actorReference.Kill();
            }

            Console.WriteLine($"message = {passedMessage}");
            Console.WriteLine($"StopWatch time (milliseconds): {stopWatch.ElapsedMilliseconds}");
            Console.WriteLine($"thats {stopWatch.ElapsedMilliseconds / 1000} per message");
            var i = 0;
            foreach (var item in receivedMessages)
            {
                Assert.AreEqual($"{i}", item);
                i++;
            }
        }

        [TestMethod]
        public void SendOneMessageInProc()
        {
            string expectedAddress = "XXXX";
            string message = "hello its me";

            using (var context = NetMQContext.Create())
            using (var exchange = new Exchange(context))
            {
                exchange.Start();
                {
                    using (var sub = Helper.GetConnectedSubscribeSocket(context, Exchange.SubscribeAddress))
                    {
                        using (var pub = Helper.GetConnectedPublishSocket(context, Exchange.PublishAddress)) // "tcp://localhost:5555"
                        {

                            NetMQMessage NetMQMessage = null;
                            var task = Task.Run(() =>
                                {
                                    if (sub != null)
                                    {
                                    NetMQMessage = Helper.ReceiveMessage(sub);
                                    }
                                    return NetMQMessage;
                                });

                            if (pub != null)
                            {
                                Thread.Sleep(30); // need to make sure that the other thread is ready to receive 
                                // before this thread sends the message
                                Helper.SendOneSimpleMessage(expectedAddress, message, pub);
                            }

                            task.Wait();
                            Assert.AreEqual(2, NetMQMessage.FrameCount);
                            NetMQFrame frame = NetMQMessage[0];
                            var address = Encoding.Unicode.GetString(frame.Buffer);
                            Assert.AreEqual(expectedAddress, address);

                            frame = NetMQMessage[1];
                            var returnedMessage = Encoding.Unicode.GetString(frame.Buffer);
                            Assert.AreEqual(message, returnedMessage);
                        }
                    }
                }

                exchange.Stop(true);
            }
        }

        ///the helper class is obsolee now these tests are not working and pointless as it is no longer done the way the 
        ///helper is set up
        //[TestMethod, TestCategory("IntegrationZMQ")]
        //[TestCategory("DoNotRunOnServer")]
        //public void SendOneMessageOfType()
        //{
        //    string input = string.Empty;
        //    string expectedAddress = "XXXXxxxx";
        //    string message = string.Empty;

        //    using (var context = NetMQContext.Create())
        //    {
        //        var pipe = new Exchange(context);
        //        pipe.Start();
        //        using (var pub = Helper.GetConnectedPublishSocket(context))
        //        {
        //            using (var sub = Helper.GetConnectedSubscribeSocket(context))
        //            {
        //                ISerializer serializer = new Serializer(Encoding.Unicode);
        //                Customer cust = new Customer(1);
        //                cust.Firstname = "John";
        //                cust.Lastname = "Wilson";

        //                Helper.SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);

        //                Customer customer = Helper.ReceiveMessageofType<Customer>(sub);

        //                Assert.AreEqual(cust.Firstname, customer.Firstname);
        //                Assert.AreEqual(cust.Lastname, customer.Lastname);
        //            }
        //        }

        //        pipe.Dispose();
        //    }
        //}

        // not reall clear what these test were testing or attempting to test
        //TO DO create new tests
        //[TestMethod, TestCategory("IntegrationZMQ")]
        //[TestCategory("DoNotRunOnServer")]
        //public void SendOneMessageOfTypeConfigureActorToProcess()
        //{
        //    string input = string.Empty;
        //    string expectedAddress = "XXXXxxxx";
        //    string message = string.Empty;

        //    using (var context = NetMQContext.Create())
        //    {

        //        using (var pub = Helper.GetConnectedPublishSocket(context))
        //        {
        //            //using (var sub = GetConnectedSubscribeSocket(context))
        //            //{
        //            ISerializer serializer = new Serializer(Encoding.Unicode);
        //            Customer cust = new Customer(1);
        //            cust.Firstname = "Johnx";
        //            cust.Lastname = "Wilson";

        //            //using (var actor = new Actor(context))
        //            //{
        //            //    actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
        //            //        {
        //            //            var customer = (Customer)Message;
        //            //            Assert.AreEqual(cust.Firstname, customer.Firstname);
        //            //            Helper.Writeline(customer.Firstname, @"c:\dev\xx.log");
        //            //        });
        //            //    actor.StartAllActors();

        //            //    Thread.Sleep(0);
        //            //}

        //            for (int i = 0; i < 10; i++)
        //            {
        //                cust.Firstname = i.ToString();
        //                Helper.SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);
        //                Thread.Sleep(0);
        //            }
        //            Helper.SendOneSimpleMessage(expectedAddress, "stop", pub);
        //            Thread.Sleep(0);
        //        }

        //    }
        //}

        ////[TestMethod, TestCategory("IntegrationZMQ")]
        //////obviously broken test
        //public void SendFiveMessageOfTypeConfigureActorToProcess()
        //{
        //     string input = string.Empty;
        //    string expectedAddress = "XXXXxxxx";
        //    string message = string.Empty;

        //    using (var context = NetMQContext.Create())
        //    {
        //        var pipe = new Exchange(context);
        //        pipe.Start();
        //        using (var pub = Helper.GetConnectedPublishSocket(context))
        //        {
        //            using (var sub = Helper.GetConnectedSubscribeSocket(context))
        //            {
        //                //using (var actor = new Actor(context))
        //                //{
        //                //    ISerializer serializer = new Serializer(Encoding.Unicode);
        //                //    actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
        //                //    {
        //                //        var customer = (Customer)Message;
        //                //        if (!Actor.PropertyBag.ContainsKey("Count"))
        //                //        {
        //                //            Actor.PropertyBag.Add("Count", "0");
        //                //        }
        //                //        var count = int.Parse(Actor.PropertyBag["Count"]);
        //                //        count++;
        //                //        Actor.PropertyBag["Count"] = count.ToString();

        //                //        //Assert.AreEqual(cust.Firstname, customer.Firstname);
        //                //        Helper.SendOneSimpleMessage("log", customer.Firstname + " " + customer.Lastname + " " + " Count " + Actor.PropertyBag["Count"], Socket);
        //                //    }).RegisterActor("Logger", "log", (Message, InRoute) =>
        //                //        {
        //                //            Helper.Writeline(Message);
        //                //        });
        //                //    actor.StartAllActors();

        //                //    Task.Delay(5000);

        //                //    for (int i = 0; i < 5; i++)
        //                //    {
        //                //        ISerializer serializer2 = new Serializer(Encoding.Unicode);
        //                //        Customer cust = new Customer();
        //                //        cust.Firstname = "John" + i.ToString();
        //                //        cust.Lastname = "Wilson" + i.ToString();

        //                //        Helper.SendOneMessageOfType<Customer>(expectedAddress, cust, serializer2, pub);
        //                //    }

        //                //    Helper.SendOneSimpleMessage(expectedAddress, "Stop", pub);
        //                //    Task.Delay(5000);
        //                //}
        //            }
        //        }
        //        pipe.Dispose();
        //    }
        //}



    }
}
