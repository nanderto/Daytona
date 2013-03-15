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

        private static ZmqSocket GetConnectedPublishSocket(ZmqContext context)
        {
            ZmqSocket publisher = context.CreateSocket(SocketType.PUB);
            publisher.Connect("tcp://localhost:5556");
            return publisher;
        }

        public static void test1()
        {
            //using (var pipeContext = ZmqContext.Create())
            //{
            //    var pipe = new Pipe();
            //    pipe.Start(pipeContext);
                


                    string input = string.Empty;
                    string expectedAddress = "XXXXxxxx";
                    string message = string.Empty;

                    using (var context = ZmqContext.Create())
                    {

                        using (var pub = GetConnectedPublishSocket(context))
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


            //using (var context = ZmqContext.Create())
            //{
            //    //var ForwarderDevice = new ForwarderDevice(context, "tcp://127.0.0.1:5550", "tcp://127.0.0.1:5553", DeviceMode.Threaded);
            //    //ForwarderDevice.Start();
            //    //while (!ForwarderDevice.IsRunning)
            //    //{ }

            //    using (ZmqSocket sub = context.CreateSocket(SocketType.SUB), pub = context.CreateSocket(SocketType.PUB))
            //    {
            //        sub.Connect("tcp://127.0.0.1:5550");
            //        sub.SubscribeAll();

            //        pub.Connect("tcp://127.0.0.1:5550");

            //        Task.Run(() =>
            //        {
            //            while (true)
            //            {
            //                var message = sub.ReceiveMessage();
            //                Console.WriteLine("here" + message);
            //            }
            //        });

            //        Task.Run(() =>
            //        {
            //            while (true)
            //            {
            //                pub.Send("XXX HELO", Encoding.Unicode);
            //                var message = sub.ReceiveMessage();
            //                Console.WriteLine("just sent");
            //            }
            //        });

            //         Console.ReadLine();
            //    }
            //}
            //RunStoreTest();
            RunSubscriber();

            Console.WriteLine("enter to exit=>");
            input = Console.ReadLine();


            //var pipe = new Pipe();
            //using (var pipeContext = ZmqContext.Create())
            //{
            //    // pipe = new Pipe();
            //    //pipe.Start(pipeContext);

            //    //var t = Task.Run(() =>
            //    //{
            //         RunSubscriber();
            //    //});

            //    //t.Wait();
            //    Console.WriteLine("enter to exit=>");
            //    input = Console.ReadLine();
            //    pipe.Exit();
            //}

           
            //test1();
            //using (var context = ZmqContext.Create())
            //{
            //    Pipe pipe = new Pipe();
            //    pipe.Start(context);
            //    using (var actor = new Actor(context))
            //    {
            //        //actor.RegisterActor("Basic", "85308", (Message, InRoute) =>
            //        //    {
            //        //        Console.WriteLine(Message);
            //        //    });
            //        //actor.StartAllActors();
            //        var Serializer = new Serializer(Encoding.Unicode);
            //        string expectedAddress = "XXXX";
            //        actor.RegisterActor<Customer>("Basic", expectedAddress, "OutRoute", serializer, (Message, InRoute, OutRoute, Socket, Actor) =>
            //        {
            //            var customer = (Customer)Message;
            //            //Console.WriteLine(customer.Firstname + " " + customer.Lastname);

            //            if (!Actor.PropertyBag.ContainsKey("Count"))
            //            {
            //                Actor.PropertyBag.Add("Count", "0");
            //            }
            //            var count = int.Parse(Actor.PropertyBag["Count"]);
            //            count++;
            //            Actor.PropertyBag["Count"] = count.ToString();

            //            Helper.Writeline(customer.Firstname + " " + customer.Lastname + " Count:" + count.ToString());
            //        });
            //        actor.StartAllActors();

            //        while (input != "exit")
            //        {
            //             input = Console.ReadLine();
            //        }
            //    }

            //    pipe.Exit();
            //}
        }

        private static void RunStoreTest()
        {
            Daytona.Store.Context context = new Daytona.Store.Context();
            using (var connection = context.GetConnection<Customer>())
            {
                var customer = new Customer
                {
                    Firstname = "John",
                    Lastname = "Lemon"
                };
                //var task = connection.Save(customer);
                //int id = task.Result;
                Console.WriteLine("Pausing=>");
                Console.ReadLine();
            }
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
    }
}
