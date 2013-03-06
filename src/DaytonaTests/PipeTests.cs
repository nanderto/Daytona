using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroMQ;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Newtonsoft.Json;
using TestHelpers;
using System.Threading;
using ZeroMQ.Devices;

namespace DaytonaTests
{
    [TestClass]
    public class PipeTests
    {
        [TestMethod]
        public void SendOneMessage()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;
            var count = 2;

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);
                using (var pub = Helper.GetConnectedPublishSocket(context))
                {
                    using (var sub = Helper.GetConnectedSubscribeSocket(context))
                    {

                        Helper.SendOneSimpleMessage(expectedAddress, message, pub);

                        var zmqMessage = Helper.ReceiveMessage(sub);

                        Assert.AreEqual(count, zmqMessage.FrameCount);
                        Frame frame = zmqMessage[0];
                        var address = Encoding.Unicode.GetString(frame.Buffer);
                        Assert.AreEqual(expectedAddress, address);
                    }
                }

                pipe.Exit();
            }
        }

        [TestMethod]
        public void InProcOnlyWithForwarder ()
        {
            string expectedAddress = "XXXX";
            string message = "hello its me";
            int count = 0;
            using (var context = ZmqContext.Create())
            {
                using (var forwarderDevice = new ForwarderDevice(context, "inproc://front", "inproc://back", DeviceMode.Threaded))
                {
                    forwarderDevice.Start();

                    using (var pub = Helper.GetConnectedPublishSocket(context, "inproc://front"))
                    {
                        using (var sub = Helper.GetConnectedSubscribeSocket(context, "inproc://back")) 
                        {
                            Task<ZmqMessage> task = Task.Run(() =>
                                {
                                    var zmqMessage = Helper.ReceiveMessage(sub);
                                    return zmqMessage;
                                });

                            Helper.SendOneSimpleMessage(expectedAddress, message, pub);
                            task.Wait();

                            var result = task.Result;

                            Assert.AreEqual(count, result.FrameCount);
                            Frame frame = result[0];
                            var address = Encoding.Unicode.GetString(frame.Buffer);
                            Assert.AreEqual(expectedAddress, address);
                        }
                    }
                    forwarderDevice.Stop();
                }
            }
        }

        static bool interupt = false;
        [TestMethod]
        public void SendOneMessageInProc()
        {
            string expectedAddress = "XXXX";
            string message = "hello its me";
            int count = 0;

            using (var context = ZmqContext.Create())
            {
                using (var forwarderDevice = new ForwarderDevice(context, "tcp://*:5555", "inproc://back", DeviceMode.Threaded))
                {
                    forwarderDevice.Start();

                    using (var pub = Helper.GetConnectedPublishSocket(context, "tcp://localhost:5555"))
                    {
                        using (var sub = Helper.GetConnectedSubscribeSocket(context, "inproc://back")) 
                        {

                            ZmqMessage zmqMessage = null;
                            var task = Task.Run(() =>
                                {
                                    if (sub != null)
                                    {
                                        //while (interupt != true)
                                        //{
                                            zmqMessage = Helper.ReceiveMessage(sub);
                                            //if (zmqMessage.FrameCount > 0)
                                            //{
                                            //    interupt = true;
                                            //}
                                        //}
                                    }
                                    return zmqMessage;
                                });

                            if (pub != null)
                            {
                                Helper.SendOneSimpleMessage(expectedAddress, message, pub);
                            }


                            Assert.AreEqual(count, zmqMessage.FrameCount);
                            Frame frame = zmqMessage[0];
                            var address = Encoding.Unicode.GetString(frame.Buffer);
                            Assert.AreEqual(expectedAddress, address);
                        }
                    }
                    forwarderDevice.Stop();
                }
            }
        }

        [TestMethod, TestCategory("IntegrationZMQ")]
        public void SendOneMessageOfType()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);
                using (var pub = Helper.GetConnectedPublishSocket(context))
                {
                    using (var sub = Helper.GetConnectedSubscribeSocket(context))
                    {
                        ISerializer serializer = new Serializer(Encoding.Unicode);
                        Customer cust = new Customer();
                        cust.Firstname = "John";
                        cust.Lastname = "Wilson";

                        Helper.SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);

                        Customer customer = Helper.ReceiveMessageofType<Customer>(sub);

                        Assert.AreEqual(cust.Firstname, customer.Firstname);
                        Assert.AreEqual(cust.Lastname, customer.Lastname);
                    }
                }

                pipe.Exit();
            }
        }

        [TestMethod, TestCategory("IntegrationZMQ")]
        public void SendOneMessageOfTypeConfigureActorToProcess()
        {
            using (var pipeContext = ZmqContext.Create())
            {
                var pipe = new Pipe();
                var task2 = Task.Run(() =>
                    {
                        pipe.Start(pipeContext);
                    });

                var task = Task.Run(() =>
                    {
                        string input = string.Empty;
                        string expectedAddress = "XXXXxxxx";
                        string message = string.Empty;

                        using (var context = ZmqContext.Create())
                        {

                            using (var pub = Helper.GetConnectedPublishSocket(context))
                            {
                                //using (var sub = GetConnectedSubscribeSocket(context))
                                //{
                                ISerializer serializer = new Serializer(Encoding.Unicode);
                                Customer cust = new Customer();
                                cust.Firstname = "Johnx";
                                cust.Lastname = "Wilson";

                                using (var actor = new Actor(context))
                                {
                                    actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
                                        {
                                            var customer = (Customer)Message;
                                            Assert.AreEqual(cust.Firstname, customer.Firstname);
                                            Helper.Writeline(customer.Firstname, @"c:\dev\xx.log");
                                        });
                                    actor.StartAllActors();

                                    Thread.Sleep(0);
                                }

                                for (int i = 0; i < 10; i++)
                                {
                                    cust.Firstname = i.ToString();
                                    Helper.SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);
                                    Thread.Sleep(0);
                                }
                                Helper.SendOneSimpleMessage(expectedAddress, "stop", pub);
                                Thread.Sleep(0);
                            }
                            //pipe.Exit();
                            //Thread.Sleep(0);
                        }
                    });
                Task.WaitAll(task, task2);
                pipe.Exit();
            }
        }

        [TestMethod, TestCategory("IntegrationZMQ")]
        public void SendFiveMessageOfTypeConfigureActorToProcess()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);
                using (var pub = Helper.GetConnectedPublishSocket(context))
                {
                    using (var sub = Helper.GetConnectedSubscribeSocket(context))
                    {
                        using (var actor = new Actor(context))
                        {
                            ISerializer serializer = new Serializer(Encoding.Unicode);
                            actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
                            {
                                var customer = (Customer)Message;
                                if (!Actor.PropertyBag.ContainsKey("Count"))
                                {
                                    Actor.PropertyBag.Add("Count", "0");
                                }
                                var count = int.Parse(Actor.PropertyBag["Count"]);
                                count++;
                                Actor.PropertyBag["Count"] = count.ToString();
                                    
                                //Assert.AreEqual(cust.Firstname, customer.Firstname);
                                Helper.SendOneSimpleMessage("log", customer.Firstname + " " + customer.Lastname + " " + " Count " + Actor.PropertyBag["Count"], Socket);
                            }).RegisterActor("Logger", "log", (Message, InRoute) =>
                                {
                                    Helper.Writeline(Message);
                                });
                            actor.StartAllActors();
                        }
                        Task.Delay(5000);

                        for (int i = 0; i < 5; i++)
                        {
                            ISerializer serializer = new Serializer(Encoding.Unicode);
                            Customer cust = new Customer();
                            cust.Firstname = "John" + i.ToString();
                            cust.Lastname = "Wilson" + i.ToString();

                            Helper.SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);
                        }

                        Helper.SendOneSimpleMessage(expectedAddress, "Stop", pub);
                        Task.Delay(5000);
                    }
                }
                pipe.Exit();
            }
        }



    }
}
