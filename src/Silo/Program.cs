using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;

namespace SiloConsole
{
    using System.Globalization;
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
                   
            using (var context = NetMQContext.Create())
            {
                var exchange = new Exchange(context);
                exchange.Start();
                using (var silo = new Silo(context, new BinarySerializer()))
                {
                    silo.RegisterEntity(typeof(Customer));
                    silo.RegisterEntity(typeof(Order));
                    silo.RegisterEntity(typeof(DiagnosticMessage));
                    silo.Start();

                    var diagnosticMessage = silo.ActorFactory.CreateInstance<IDiagnosticMessage>(typeof(DiagnosticMessage));
                          
                    diagnosticMessage.WriteToConsole("Yeah this works");
                    //Console.WriteLine("Run tests");
                    //Console.ReadLine();

                    var customer = silo.ActorFactory.CreateInstance<ICustomer>(typeof(Customer),99);
                        
                    //customer.CreateOrder();

                    var uniqueGuid = Guid.NewGuid();
                    var order = silo.ActorFactory.CreateInstance<IOrder>(typeof(Order), uniqueGuid);

                    var productId = Guid.NewGuid()
                        .ToString()
                        .Replace("-", "")
                        .Replace("{", "")
                        .Replace("}", "")
                        .Substring(0, 10);

                    order.CreateOrder("Another order", 23, productId, 12);

                    var exit = string.Empty;
                    var description = "XXXX";

                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine(
                               "({0}) Last order created was {1}, its description was {2}", i,
                               uniqueGuid,
                               description);

                       // uniqueGuid = Guid.NewGuid();
                        customer.CreateOrder();

                        if (exit != null && exit.ToLower() != "runtoend")
                        {     
                            Console.WriteLine("Press Enter to send another message, or type exit to stop");
                            exit = Console.ReadLine();
                            if (exit != null && exit.ToLower() == "exit")
                            {
                                break;
                            }
                        }

                        description = "New Description " + 23 + (i * 2);
                        order.UpdateDescription(description);

                    //    customer.UpdateName(string.Format("new name, {0}", i.ToString(CultureInfo.InvariantCulture)));
                    //    var customer2 = silo.ActorFactory.CreateInstance<ICustomer>(typeof(Customer), i);
                    //    ////Thread.Sleep(500);
                    //    customer2.UpdateName("XXX - AAA" + i);
                    //    Console.WriteLine("Customer {0} was updated", i);
                    }

                    ////var netMqMessage = new NetMQMessage();
                    ////netMqMessage.Append(new NetMQFrame(actor.Serializer.GetBuffer("Aslongasitissomething")));
                    ////netMqMessage.Append(new NetMQFrame(actor.Serializer.GetBuffer("shutdownallactors")));
                    ////actor.OutputChannel.SendMessage(netMqMessage);

                    Console.WriteLine("Out of loop; Press Enter to stop silo");
                    Console.ReadLine();
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
