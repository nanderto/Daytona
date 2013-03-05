using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroMQ;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Newtonsoft.Json;
using TestHelpers;
using System.Threading;

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
                //var pipe = new Pipe();
                //pipe.Start(context);
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

                //pipe.Exit();
            }
        }

        [TestMethod]
        public void SendOneMessageInProc()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;
            var count = 2;

            using (var context = ZmqContext.Create())
            {
                //var pipe = new Pipe();
                //pipe.Start(context);
                using (var pub = Helper.GetBoundSubscribeSocket(context, "inproc://somename"))
                {
                    using (var sub = Helper.GetConnectedPublishSocket(context, "inproc://somename"))
                    {

                        Helper.SendOneSimpleMessage(expectedAddress, message, pub);

                        var zmqMessage = Helper.ReceiveMessage(sub);

                        Assert.AreEqual(count, zmqMessage.FrameCount);
                        Frame frame = zmqMessage[0];
                        var address = Encoding.Unicode.GetString(frame.Buffer);
                        Assert.AreEqual(expectedAddress, address);
                    }
                }

                //pipe.Exit();
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
