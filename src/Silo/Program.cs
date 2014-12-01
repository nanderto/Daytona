using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;
using ZeroMQ;

namespace Silo
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
                using (var actorFactory = new Actor(context, new BinarySerializer(), string.Empty))
                {
                    actorFactory.RegisterActor(
                        "Silo",
                        "",
                        "outRoute",
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
                                       var customer = new Actor<Customer>(actor.Context, new BinarySerializer());
                                       customer.StartWithIdAndMethod(address, methodInfo, parameters);
                                       ////start actor
                                       /// 
                                       
                                       runningActors.Add(new RunningActors(address));
                                   }
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
                    Console.WriteLine("yada yada");

                    while (!interrupted)
                    {
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
