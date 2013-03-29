using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestHelpers;
using ZeroMQ;
using ZeroMQ.Devices;
using Daytona.Store;

namespace Samples
{
    class Program
    {
        static bool interrupted = false;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
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

        private static void SendOneMessageOfType<T>(string Address, T message, ISerializer serializer, ZmqSocket publisher)
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(Address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer(message)));
            publisher.SendMessage(zmqMessage);
        }

        static void Main(string[] args)
        {
            var input = string.Empty;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);

            //input = RunSenderWriterTest(input);

            RunStoreTest();
            ////RunSubscriber();
            //test1();
            //input = RunPropertyBagTest(input);
        }

        private static string RunSenderWriterTest(string input)
        {
            using (var context = ZmqContext.Create())
            {
                ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
                var actorFactory = new Actor(context);

                actorFactory.RegisterActor<DBPayload<Customer>>("Writer", "Writer", "Sender", serializer, (IPayload message, byte[] messageAsBytes, string inRoute, string outRoute, ZmqSocket socket, Actor actor) =>
                {
                    Actor.Writeline("Got here in the writer");
                    var writer = new Writer();
                    int Id = writer.Save(messageAsBytes);
                    var dBPayload = new DBPayload<Customer>();
                    dBPayload.Id = Id;
                    actor.SendOneMessageOfType<DBPayload<Customer>>(outRoute, dBPayload, serializer, socket);
                });

                actorFactory.RegisterActor<DBPayload<Customer>>("Sender", "Sender", "NO OUT ROUTE", serializer, (IPayload message, byte[] messageAsBytes, string inRoute, string outRoute, ZmqSocket socket, Actor actor) =>
                {
                    try
                    {
                        Actor.Writeline("Got here in the Sender");
                        var dBPayload = new DBPayload<Customer>();
                        actor.CallBack(1, null, null);
                    }
                    catch (Exception ex)
                    {
                        actor.CallBack(1, null, ex);
                    }
                    
                });

                actorFactory.StartAllActors();

                Console.WriteLine("enter to exit=>");
                input = Console.ReadLine();
            }
            return input;
        }

        private static string RunPropertyBagTest(string input)
        {
            using (var context = ZmqContext.Create())
            {
                Pipe pipe = new Pipe();
                pipe.Start(context);
                using (var actor = new Actor(context))
                {
                    //actor.RegisterActor("Basic", "85308", (Message, InRoute) =>
                    //    {
                    //        Console.WriteLine(Message);
                    //    });
                    //actor.StartAllActors();
                    var serializer = new TestHelpers.Serializer(Encoding.Unicode);
                    string expectedAddress = "XXXX";
                    actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
                    {
                        var customer = (Customer)Message;
                        //Console.WriteLine(customer.Firstname + " " + customer.Lastname);

                        if (!Actor.PropertyBag.ContainsKey("Count"))
                        {
                            Actor.PropertyBag.Add("Count", "0");
                        }
                        var count = int.Parse(Actor.PropertyBag["Count"]);
                        count++;
                        Actor.PropertyBag["Count"] = count.ToString();

                        Helper.Writeline(customer.Firstname + " " + customer.Lastname + " Count:" + count.ToString());
                    });
                    actor.StartAllActors();

                    while (input != "exit")
                    {
                        input = Console.ReadLine();
                    }
                }

                pipe.Exit();
            }
            return input;
        }

        private static void RunStoreTest()
        {
            var task = Task.Run(async () =>
            {
                using (Daytona.Store.Context context = new Daytona.Store.Context())
                {
                    using (var connection = context.GetConnection<Customer>())
                    {
                        var customer = new Customer
                        {
                            Firstname = "John",
                            Lastname = "Lemon"
                        };
                        var task1 = connection.Save(customer);
                        int id = task1.Result;
                        Console.WriteLine("the id returned is: " + id.ToString());
                        Console.WriteLine("Pausing=>");
                        Console.ReadLine();

                        for (int i = 0; i < 100; i++)
                        {
                            var customer2 = new Customer
                            {
                                Firstname = "John" + i.ToString(),
                                Lastname = "Lemon"
                            };
                            var task2 = connection.Save(customer2);
                            id = -1;
                            id = await task2;
                            Console.WriteLine("the id returned is: " + id.ToString()); 
                        }

                        Console.WriteLine("Pausing=>");
                        Console.ReadLine();
                    }
                }
            });
            task.Wait();
        }

        private static Task RunSubscriber()
        {
            using (var context = ZmqContext.Create())
            {
                using (ZmqSocket sub = Helper.GetConnectedSubscribeSocket(context, Pipe.SubscribeAddressClient),
                    syncClient = context.CreateSocket(SocketType.REQ))
                {
                    syncClient.Connect(Pipe.PubSubControlBackAddressClient);

                    Console.WriteLine("Send message that you are connected=>");
                    Console.ReadLine();

                    syncClient.Send("", Encoding.Unicode);
                    syncClient.Receive(Encoding.Unicode);


                    Console.WriteLine("Received acknowledgement=>");
                    // Console.ReadLine();
                    bool run = true;
                    string input;
                    while (run)
                    {
                        ZmqMessage zmqMessage = null;
                        while (zmqMessage == null)
                        {
                            zmqMessage = Helper.ReceiveMessage(sub);
                        }

                        //Assert.AreEqual(2, zmqMessage.FrameCount);
                        Frame frame = zmqMessage[0];
                        var address = Encoding.Unicode.GetString(frame.Buffer);
                        Console.WriteLine(address);
                        frame = zmqMessage[1];
                        var message = Encoding.Unicode.GetString(frame.Buffer);
                        Console.Write(" " + message);
                        Console.WriteLine();
                        Console.WriteLine("Received message Exit to Exit=>");
                        input = Console.ReadLine();
                        if (input == "exit") break;
                    }
                }
                return null;
            }



        }

        public static void test1()
        {
            string input = string.Empty;
            string expectedAddress = "XXXXxxxx";
            string message = string.Empty;

            using (var context = ZmqContext.Create())
            {

                using (var pub = GetConnectedPublishSocket(context))
                {
                    //using (var sub = GetConnectedSubscribeSocket(context))
                    //{
                    ISerializer serializer = new TestHelpers.Serializer(Encoding.Unicode);
                    Customer cust = new Customer();
                    cust.Firstname = "Johnx";
                    cust.Lastname = "Wilson";

                    using (var actor = new Actor(context))
                    {
                        actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
                        {
                            var customer = (Customer)Message;
                            //Assert.AreEqual(cust.Firstname, customer.Firstname);
                            Helper.Writeline(customer.Firstname, @"c:\dev\xx.log");
                        });
                        actor.StartAllActors();

                        Thread.Sleep(0);
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        cust.Firstname = i.ToString();
                        SendOneMessageOfType<Customer>(expectedAddress, cust, serializer, pub);
                        Thread.Sleep(0);
                    }
                    SendOneSimpleMessage(expectedAddress, "stop", pub);
                    Thread.Sleep(0);
                }
                //pipe.Exit();
                //Thread.Sleep(0);
            }

            //pipe.Exit();
            //}
        }

        private static ZmqSocket GetConnectedPublishSocket(ZmqContext context)
        {
            ZmqSocket publisher = context.CreateSocket(SocketType.PUB);
            publisher.Connect("tcp://localhost:5556");
            return publisher;
        }

    }
}

