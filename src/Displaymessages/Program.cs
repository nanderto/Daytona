using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;

namespace Monitor
{
    using System.IO;
    using System.Reflection;

    using NetMQ;

    class Program
    {
        static void Main(string[] args)
        {
        //    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        //    var directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
        //    var Files = directoryInfo.GetFiles();
        //    var path = Environment.CurrentDirectory + @"\NProxy.Core.dll";
        //    var assembly = Assembly.LoadFrom(path);

            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            var binarySerializer = new BinarySerializer();
            var useActor = true;

            using (var context = NetMQContext.Create())
            {
                //if (!useActor)
                //{
                //    var subscriber = context.CreateSubscriberSocket();
                //    subscriber.Connect(Pipe.SubscribeAddressClient);
                //    subscriber.Subscribe(string.Empty);
                //    while (!interrupted)
                //    {
                //        var frame = subscriber.ReceiveFrame();
                //        try
                //        {
                //            Console.WriteLine(binarySerializer.Deserializer(frame, typeof(string)));
                //        }
                //        catch (Exception)
                //        {
                //        }

                //        while (subscriber.ReceiveMore)
                //        {
                //            var frame1 = subscriber.ReceiveFrame();
                //        }
                //    }
                //}
                //else
                //{
                using (var actorFactory = new Actor<Silo>(context, new BinarySerializer(), string.Empty))
                {
                    actorFactory.RegisterActor(
                        "Display",
                        "",
                        "outRoute",
                        new BinarySerializer(),
                        (address, methodinfo, parameters, actor) =>
                            {
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
                    Console.WriteLine("yada ");
                       
                    while (!interrupted)
                    {
                        Console.ReadLine();
                    }
                }
                //}
            }
        }

        static bool interrupted = false;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }
        

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) 
        {
            Assembly ayResult = null;
            string sShortAssemblyName = args.Name.Split(',')[0];
            Assembly[] ayAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly ayAssembly in ayAssemblies) 
            {
                if (sShortAssemblyName == ayAssembly.FullName.Split(',')[0]) 
                {
                 ayResult = ayAssembly;
                 break;
                }
            }
            return ayResult;
        }
    }
}
