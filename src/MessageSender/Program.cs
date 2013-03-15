using Daytona;
using Daytona.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestHelpers;
using ZeroMQ;

namespace MessageSender
{
    class Program
    {
        static bool interrupted = false;
        static uint nbSubscribersConnected = 0;
        static Options options;
        static long msgCptr = 0;
        static int msgIndex = 0;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }

        static void Main(string[] args)
        {
            var input = string.Empty;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);

            using (var ctx = ZmqContext.Create())
            {
                var pubSocket = ctx.CreateSocket(SocketType.PUB);
                pubSocket.Bind(Pipe.PublishAddressClient);
                pubSocket.SendReady += new EventHandler<SocketEventArgs>(pubSocket_SendReady);
                var repSocket = ctx.CreateSocket(SocketType.REP);
                repSocket.Bind(Pipe.PubSubControlFrontAddressClient);
                repSocket.SendReady += new EventHandler<SocketEventArgs>(repSocket_SendReady);
                repSocket.ReceiveReady += new EventHandler<SocketEventArgs>(repSocket_ReceiveReady);
                
                Poller poller = new Poller(new ZmqSocket[] { pubSocket, repSocket });
                while (true)
                {
                    poller.Poll();
                    if (options.maxMessage >= 0)
                        if (msgCptr > options.maxMessage)
                            Environment.Exit(0);
                }

            }
            ////var pipe = new Pipe();
            ////using (var pipeContext = ZmqContext.Create())
            ////{
            ////    // pipe = new Pipe();
            ////    pipe.Start(pipeContext);

            ////    //Task.Run(() =>
            ////    //{
            ////    //    return RunSubscriber();
            ////    //});

            ////    Console.WriteLine("=>");
            ////    input = Console.ReadLine();
            ////    pipe.Exit();
            ////}
            //input = SynchronizedSender(input);
        }

        static void repSocket_ReceiveReady(object sender, SocketEventArgs e)
        {
            var reqMsg = e.Socket.Receive(Encoding.UTF8);
            Console.WriteLine("REP, received: " + reqMsg);
        }

        static void repSocket_SendReady(object sender, SocketEventArgs e)
        {
            Console.WriteLine("REP, sending: Sync OK");
            e.Socket.Send(Encoding.UTF8.GetBytes("Sync OK"));
            nbSubscribersConnected++;
        }

        static void pubSocket_SendReady(object sender, SocketEventArgs e)
        {
            var zmqMessage = new ZmqMessage();
            if (nbSubscribersConnected < options.nbExpectedSubscribers)
            {
                zmqMessage.Append(Encoding.UTF8.GetBytes("Sync"));
                zmqMessage.Append(Encoding.UTF8
                                          .GetBytes(options.repEndpoint));
                Thread.Sleep(options.delay);
                Console.WriteLine("Publishing: Sync");
            }
            else
            {
                zmqMessage.Append(Encoding.UTF8.GetBytes("Data"));
                var data = "MYDATA"; //BuildDataToPublish();
                if (!string.IsNullOrEmpty(data))
                {
                    zmqMessage.Append(Encoding.UTF8.GetBytes(data));
                    Thread.Sleep(options.delay);
                    Console.WriteLine("Publishing (Data): " + data);
                }
            }
            e.Socket.SendMessage(zmqMessage);
        }

        private static string SynchronizedSender(string input)
        {

            using (var context = ZmqContext.Create())
            {
                using (ZmqSocket pub = Helper.GetConnectedPublishSocket(context, Pipe.PublishAddressClient),
                      syncService = context.CreateSocket(SocketType.REP))
                {
                    syncService.Connect(Pipe.PubSubControlFrontAddressClient);
                    for (int i = 0; i < 1; i++)
                    {
                        syncService.Receive(Encoding.Unicode);
                        syncService.Send("", Encoding.Unicode);
                    }

                    for (int i = 0; i < 100000; i++)
                    {
                        Console.WriteLine("Enter to send message=>");
                        input = Console.ReadLine();
                        if (input == "Exit") break;

                        var cust = new Customer
                        {
                            Firstname = "John",
                            Lastname = "Lemon"
                        };
                        var pl = new Daytona.Store.DBPayload<Customer>();
                        pl.Payload = cust;
                        ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
                        Helper.SendOneMessageOfType<DBPayload<Customer>>("Writer", pl, serializer, pub);
                        // Helper.SendOneSimpleMessage("Writer", "Hello its me", pub);
                        Console.WriteLine("message sent");
                    }

                    Console.WriteLine("message sent enter to exit=>");
                    input = Console.ReadLine();
                }
            }
            return input;
        }
            


            
        

        private static void SendCustomers(ZmqContext context)
        {
            ISerializer serializer = new Daytona.Store.Serializer(Encoding.Unicode);
            using (ZmqSocket publisher = context.CreateSocket(SocketType.PUB))
            {
                publisher.Connect("tcp://localhost:5556");
                for (int i = 0; i < 10000; i++)
                {
                    var customer = new Customer()
                    {
                        Firstname = "Willie",
                        Lastname = "Loman" + i.ToString()
                    };
                    Helper.SendOneMessageOfType<Customer>("XXXX", customer, serializer, publisher);
                    Helper.Writeline("sent:" + i.ToString(), @"c:\dev\sender.log");
                }
                Helper.SendOneSimpleMessage("XXXX", "stop", publisher);
            }
        }

        private static void RunWeatherWithFrames(ZmqContext context, string Address, string message)
        {

            string input = string.Empty;
            using (ZmqSocket publisher = context.CreateSocket(SocketType.PUB))
            {
                publisher.Connect("tcp://localhost:5556");
                while (input != "exit")
                {
                    interrupted = false;
                    Address = "11111 ";
                    message = "Hi johnny was here";

                    var randomizer = new Random(DateTime.Now.Millisecond);
                    Console.Write("sending");
                    int i = 0;

                    while (!interrupted)
                    {
                        ++i;
                        //  Get values that will fool the boss
                        int zipcode = randomizer.Next(0, 100000);
                        int temperature = randomizer.Next(-80, 135);
                        int relativeHumidity = randomizer.Next(10, 60);

                        string update = "xx " + temperature.ToString() + " " + relativeHumidity.ToString();

                        if (i > 1000000)
                        {
                            Console.Write(".");
                            i = 0;
                        }

                        ZmqMessage zmqMessage = new ZmqMessage();
                        zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(zipcode.ToString())));
                        zmqMessage.Append(new Frame(Encoding.Unicode.GetBytes(update)));

                        publisher.SendMessage(zmqMessage);
                    }
                    Console.WriteLine("=>");
                    input = Console.ReadLine();
                }
            }
        }

        private static void RunWeatherDataPublisher(ZmqContext context)
        {
            string input = string.Empty;
            using (ZmqSocket publisher = context.CreateSocket(SocketType.PUB))
            {
                publisher.Connect("tcp://localhost:5556");
                while (input != "exit")
                {
                    interrupted = false;
                    var randomizer = new Random(DateTime.Now.Millisecond);
                    Console.Write("sending");
                    int i = 0;

                    while (!interrupted)
                    {
                        ++i;
                        //  Get values that will fool the boss
                        int zipcode = randomizer.Next(0, 100000);
                        int temperature = randomizer.Next(-80, 135);
                        int relativeHumidity = randomizer.Next(10, 60);

                        string update = zipcode.ToString() + " " + temperature.ToString() + " " + relativeHumidity.ToString();
                        //zipcode = 10001;
                        // Send message to 0..N subscribers via a pub socket
                        if (i > 1000000)
                        {
                            Console.Write(".");
                            i = 0;
                        }
                        publisher.Send(update, Encoding.Unicode);
                    }
                    Console.WriteLine("=>");
                    input = Console.ReadLine();
                }
            }
        }
    }
}
