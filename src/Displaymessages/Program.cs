using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;
using ZeroMQ;

namespace Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            using (var context = ZmqContext.Create())
            {
                using (var actorFactory = new Actor<Silo>(context, new BinarySerializer(), string.Empty))
                {
                    actorFactory.RegisterActor("Publisher", "", "outRoute", new BinarySerializer(), (address, parameters, actor) =>
                    {
                        Console.WriteLine(address);
                        //actor.
                    });
                    actorFactory.StartAllActors();
                    Console.WriteLine("yada yada");
                    Console.ReadLine();
                    while (!interrupted)
                    {
                       
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
