using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Counter
{
    using System.Threading;

    using Daytona;

    using NetMQ;

    using TestHelpers;

    class Program
    {
        private static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            using (var context = NetMQContext.Create())
            {
                var exchange = new Exchange(context);
                exchange.Start();
                using (var silo = new Silo(context, new BinarySerializer()))
                {
                    silo.RegisterEntity(typeof(TestHelpers.Counter));
                    silo.RegisterEntity(typeof(DiagnosticMessage));
                    silo.Start();

                    var diagnosticMessage = silo.ActorFactory.CreateInstance<IDiagnosticMessage>(typeof(DiagnosticMessage));

                    diagnosticMessage.WriteToConsole("Yeah this works");

                    Console.WriteLine("Press enter to start");
                    Console.ReadLine();

                    var counter = silo.ActorFactory.CreateInstance<ICounter>(typeof(Counter), Guid.NewGuid());

                    var exit = string.Empty;

                    for (int i = 0; i < 100; i++)
                    {
                        diagnosticMessage.WriteToConsole("Yeah this works");
                        Console.WriteLine(i);
                        counter.Add();
                        Thread.Sleep(100);
                    }

                    Console.WriteLine("Done: press enter to exit");
                    Console.ReadLine();
                    silo.Stop();
                }

                exchange.Stop(true);

                Console.WriteLine("Press Enter to Exit....again");
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
