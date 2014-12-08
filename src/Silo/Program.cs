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
                //using (var silo = new Silo(context, new BinarySerializer()))
                //{

                //}               
                //using (var pipe = new Pipe())
                //{
                //    pipe.Start(context);
                    using (var actorFactory = new Actor(context, new BinarySerializer(), string.Empty))
                    {
                        actorFactory.RegisterActor(
                            "Silo",
                            "",
                            "SilooutRoute",
                            new BinarySerializer(),
                            (address, methodInfo, parameters, actor) =>
                                {
                                    object returnedObject = null;
                                    List<RunningActors> runningActors = null;

                                    if (actor.PropertyBag.TryGetValue("RunningActors", out returnedObject))
                                    {
                                        runningActors = (List<RunningActors>)returnedObject;
                                        var returnedActor = runningActors.FirstOrDefault(ra => ra.Address == address);

                                        if (returnedActor == null)
                                        {
                                            Console.WriteLine("We dident find an actor");
                                            var addressAndNumber = address.Split('/');
                                            if (addressAndNumber[0] == "TestHelpers.Customer")
                                            {
                                                var customer = new Actor<Customer>(actor.Context, new BinarySerializer());
                                                customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                            }

                                            if (addressAndNumber[0] == "TestHelpers.Order")
                                            {
                                                var order = new Actor<Order>(actor.Context, new BinarySerializer());
                                                order.StartWithIdAndMethod(address, methodInfo, parameters);
                                            }
                                            
                                            Console.WriteLine("I wish I could start a method");
                                            ////start actor
                                            /// 

                                            runningActors.Add(new RunningActors(address));
                                        }

                                        Console.WriteLine("We found a running actor so er did nothing");
                                    }
                                    else
                                    {
                                        var customer = new Actor<Customer>(actor.Context, new BinarySerializer());
                                       // customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                        Console.WriteLine("no collection of running actors, So I am creating one and starting a new runner");
                                        ////start actor
                                        /// 
                                        runningActors = new List<RunningActors>();
                                        runningActors.Add(new RunningActors(address));
                                        actor.PropertyBag.Add("RunningActors", runningActors);
                                    }

                                    var firstParameter = string.Empty;
                                    try
                                    {
                                        firstParameter = parameters[0].ToString();
                                    }
                                    catch (Exception)
                                    {
                                    }

                                    Console.WriteLine("Address: {0}, {1}", address, firstParameter);
                                });
                        actorFactory.StartAllActors();

                        Console.WriteLine("Run tests");
                        Console.ReadLine();
                        using (var actor = new Actor<Customer>(context, new BinarySerializer()))
                        {
                            var customer = actor.CreateInstance<ICustomer>(typeof(Customer), 33);
                            // Assert.IsInstanceOfType(customer, typeof(ICustomer));
                            customer.UpdateName("XXX"); //called without exception

                            var order = actor.CreateInstance<IOrder>(typeof(Order));
                            // Assert.IsInstanceOfType(order, typeof(IOrder));
                            order.UpdateDescription("XXX"); //called without exception

                            var order2 = actor.CreateInstance<IOrder>(typeof(Order), Guid.NewGuid());
                            // Assert.IsInstanceOfType(order2, typeof(IOrder));
                            order2.UpdateDescription("ZZZ"); //called without exception
                        }

                         Console.ReadLine();
                    }
                //}
                exchange.Stop(true);
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
