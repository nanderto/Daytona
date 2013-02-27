using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;
using ZeroMQ;

namespace MessageSender
{
    class Program
    {
        static bool interrupted = false;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }

        static void Main(string[] args)
        {
            var input = string.Empty;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);

            using (var context = ZmqContext.Create())
            {
                //string Address = "11111 ";
                //string message = "Hi johnny was here";
                var customer = new Customer()
                {
                    Firstname = "Willie",
                    Lastname = "Loman"
                };

                ISerializer serializer = new Serializer(Encoding.Unicode);
                using (ZmqSocket publisher = context.CreateSocket(SocketType.PUB))
                {
                    publisher.Connect("tcp://localhost:5556");
                    Helper.SendOneMessageOfType<Customer>("XXXX", customer, serializer, publisher);
                }
                //RunWeatherWithFrames(context, Address, message);
                //RunWeatherDataPublisher(context);
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
