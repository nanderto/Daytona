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
                    silo.RegisterEntity(typeof(Customer));
                    silo.RegisterEntity(typeof(Order));
                    silo.Start();
                               

                    Console.WriteLine("Run tests");
                    
                    Console.ReadLine();
                    //silo.ActorFactory.CreateInstance<>()
                    using (var actor = new Actor(context, new BinarySerializer()))
                    {
                        var customer = silo.ActorFactory.CreateInstance<ICustomer>(typeof(Customer), 33);
                        customer.CreateOrder();

                        var uniqueGuid = Guid.NewGuid();

                        var order = silo.ActorFactory.CreateInstance<IOrder>(typeof(Order), uniqueGuid);
                        Thread.Sleep(300);
                        var productId = Guid.NewGuid()
                            .ToString()
                            .Replace("-", "")
                            .Replace("{", "")
                            .Replace("}", "")
                            .Substring(0, 10);

                        order.CreateOrder(
                            "Another order",
                            23,
                            productId,
                            12);


                        //customer.UpdateName("XXX - AAA");
                       // customer.CreateOrder();

                        //var order = actor.CreateInstance<IOrder>(typeof(Order));
                        //order.UpdateDescription("XXX");

                        //var order2 = actor.CreateInstance<IOrder>(typeof(Order), Guid.NewGuid());
                        //order2.UpdateDescription("ZZZ");

                        var exit = string.Empty;
                       // customer.UpdateName("XXX");
                        for (int i = 0; i < 100; i++)
                        {
                            if (exit.ToLower() != "runtoend")
                            {
                                 Console.WriteLine("Press Enter to send another message, or type exit to stop");
                                exit = Console.ReadLine();
                                if (exit.ToLower() == "exit")
                                {
                                    break;
                                }
                            }

                            order.UpdateOrder(uniqueGuid, "Updated order",
                            23 + i * 2,
                            productId,
                            12 + i); 
                            //var customer2 = actor.CreateInstance<ICustomer>(typeof(Customer), i);
                            ////Thread.Sleep(500);
                            //customer2.UpdateName("XXX - AAA" + i);

                            //if (exit.ToLower() == "update100")
                            //{

                            //    for (int j = 0; j < 1000; j++)
                            //    {
                            //        customer2.UpdateName("XXX - " + j); 
                            //    }   
                            //}                           
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
