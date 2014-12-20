using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;

namespace SiloConsole
{
    using System.IO;
    using System.Reflection;
    using System.Threading;

    using NetMQ;
    using NetMQ.Devices;

    class Program
    {
        public static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            var binarySerializer = new BinarySerializer();
            var useActor = true;

            using (var context = NetMQContext.Create())
            {
                var exchange = new Exchange(context);
                exchange.Start();
                using (var silo = new Silo(context, new BinarySerializer()))
                {
                    silo.RegisterClown(typeof(Customer));
                    silo.RegisterClown(typeof(Order));
                    silo.Start();
                               

                    Console.WriteLine("Run tests");
                    Console.ReadLine();
                    using (var actor = new Actor(context, new BinarySerializer()))
                    {
                        var customer = actor.CreateInstance<ICustomer>(typeof(Customer), 33);
                        Thread.Sleep(300);
                        customer.UpdateName("XXX - AAA");

                        //var order = actor.CreateInstance<IOrder>(typeof(Order));
                        //order.UpdateDescription("XXX");

                        //var order2 = actor.CreateInstance<IOrder>(typeof(Order), Guid.NewGuid());
                        //order2.UpdateDescription("ZZZ");

                        customer.UpdateName("XXX");
                        for (int i = 0; i < 100; i++)
                        {
                            Console.WriteLine("Press Enter to send another message");
                            var exit = Console.ReadLine();
                            if (exit.ToLower() == "exit")
                            {
                                break;
                            }

                            customer.UpdateName("XXX - " + i);
                            //order2.UpdateDescription("ZZZ" + i);


                            var customer2 = actor.CreateInstance<ICustomer>(typeof(Customer), i);
                            Thread.Sleep(300);
                            customer2.UpdateName("XXX - AAA" + i);
                        }
                 
                        //var netMqMessage = new NetMQMessage();
                        //netMqMessage.Append(new NetMQFrame(actor.Serializer.GetBuffer("Aslongasitissomething")));
                        //netMqMessage.Append(new NetMQFrame(actor.Serializer.GetBuffer("shutdownallactors")));
                        //actor.OutputChannel.SendMessage(netMqMessage);
                        //Console.WriteLine("Press Enter to Exit");
                        //Console.ReadLine();
                    }
                    
                    silo.Stop();
                }
                
                exchange.Stop(true);

                Console.WriteLine("Press Enter to Exit");
                Console.ReadLine();
             }
        }

        static bool interrupted = false;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }
    }
}
