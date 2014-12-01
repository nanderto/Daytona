using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;
using ZeroMQ;

namespace SiloConsole
{
    using System.IO;
    using System.Reflection;

    class Program
    {
        public static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            var binarySerializer = new BinarySerializer();
            var useActor = true;

            using (var context = ZmqContext.Create())
            {
                //using (var silo = new Silo(context, new BinarySerializer()))
                //{

                //}               
                 using (var pipe = new Pipe())
                {
                    pipe.Start(context);
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
                                            var customer = new Actor<Customer>(actor.context, new BinarySerializer());
                                            customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                            ////start actor
                                            /// 

                                            runningActors.Add(new RunningActors(address));
                                        }
                                    }
                                    else
                                    {
                                        var customer = new Actor<Customer>(actor.context, new BinarySerializer());
                                        customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                        ////start actor
                                        /// 

                                        runningActors.Add(new RunningActors(address));
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
                }
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
