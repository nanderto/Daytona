using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroMQ;
using System.Text;
using System.Threading.Tasks;
using Daytona;
using Newtonsoft.Json;
using TestHelpers;

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
                using (var pub = GetConnectedPublishSocket(context))
                {
                    using (var sub = GetConnectedSubscribeSocket(context))
                    {

                        SendOneSimpleMessage(expectedAddress, message, pub);

                        var zmqMessage = ReceiveMessage(sub);

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
        public void SendOneMessageOfType()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);
                using (var pub = GetConnectedPublishSocket(context))
                {
                    using (var sub = GetConnectedSubscribeSocket(context))
                    {
                        ISerializer serializer = new Serializer(Encoding.Unicode);
                        Customer cust = new Customer();
                        cust.Firstname = "John";
                        cust.Lastname = "Wilson";

                        SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);

                        Customer customer = ReceiveMessageofType<Customer>(sub);

                        Assert.AreEqual(cust.Firstname, customer.Firstname);
                        Assert.AreEqual(cust.Lastname, customer.Lastname);
                    }
                }

                pipe.Exit();
            }
        }

        [TestMethod]
        public void SendOneMessageOfTypeConfigureActorToProcess()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);
                using (var pub = GetConnectedPublishSocket(context))
                {
                    using (var sub = GetConnectedSubscribeSocket(context))
                    {
                        ISerializer serializer = new Serializer(Encoding.Unicode);
                        Customer cust = new Customer();
                        cust.Firstname = "John";
                        cust.Lastname = "Wilson";

                        SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);

                        using (var actor = new Actor(context))
                        {
                            actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
                                {
                                    var customer = (Customer)Message;
                                    Assert.AreEqual(cust.Firstname, customer.Firstname);
                                    Console.WriteLine(Message);
                                });
                            actor.StartAllActors();
                        }

                    }
                }
                pipe.Exit();
            }
        }

        [TestMethod]
        public void SendFiveMessageOfTypeConfigureActorToProcess()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;

            using (var context = ZmqContext.Create())
            {
                var pipe = new Pipe();
                pipe.Start(context);
                using (var pub = GetConnectedPublishSocket(context))
                {
                    using (var sub = GetConnectedSubscribeSocket(context))
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

                            SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);
                        }

                        SendOneSimpleMessage(expectedAddress, "Stop", pub);
                        Task.Delay(5000);
                    }
                }
                pipe.Exit();
            }
        }
        private T ReceiveMessageofType<T>(ZmqSocket sub)
        {
            string address = string.Empty;
            ZmqMessage message = null;
            return ReceiveMessage<T>(sub, out message, out address);
        }

        private void SendOneMessageOfType<T>(string Address, T message, ISerializer serializer, ZmqSocket publisher)
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(Address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer(message)));
            publisher.SendMessage(zmqMessage);
        }


        private static T ReceiveMessage<T>(ZmqSocket Subscriber, out ZmqMessage zmqMessage, out string address)
        {
            T result = default(T);
            ZmqMessage zmqOut = new ZmqMessage();
            bool hasMore = true;
            string message = "";
            address = string.Empty;
            int i = 0;
            while (hasMore)
            {
                Frame frame = Subscriber.ReceiveFrame();
                if (i == 0)
                {
                    address = Encoding.Unicode.GetString(frame.Buffer);
                }
                if (i == 1)
                {
                    result = (T)JsonConvert.DeserializeObject<T>(Encoding.Unicode.GetString(frame.Buffer));
                }

                i++;
                zmqOut.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                hasMore = Subscriber.ReceiveMore;
            }

            zmqMessage = zmqOut;
            return result;
        }

        private static ZmqMessage ReceiveMessage(ZmqSocket Subscriber)
        {
            var zmqMessage = new ZmqMessage();
            bool hasMore = true;
            string message = "";
             
            while (hasMore)
            {
                message = Subscriber.Receive(Encoding.Unicode);

                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                hasMore = Subscriber.ReceiveMore;
            }

            return zmqMessage;
        }

        private static ZmqSocket GetConnectedPublishSocket(ZmqContext context)
        {
            ZmqSocket publisher = context.CreateSocket(SocketType.PUB);
            publisher.Connect("tcp://localhost:5556");
            return publisher;
        }

        private static ZmqSocket GetConnectedSubscribeSocket(ZmqContext context)
        {
            ZmqSocket Subscriber = context.CreateSocket(SocketType.SUB);
            Subscriber.Connect("tcp://localhost:5555");
            Subscriber.SubscribeAll();
            return Subscriber;
        }

        private static void SendOneSimpleMessage(string address, string message, ZmqSocket publisher)
        {
            {
                ZmqMessage zmqMessage = new ZmqMessage();
                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(address)));
                zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(message)));
                publisher.SendMessage(zmqMessage);
            }
        }


    }
}
