using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;

namespace Samples
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
                RunPipe(context);
                using (var actor = new Actor(context))
                {
                    actor.RegisterActor("Basic", "85308", (Message, InRoute) =>
                        {
                            Console.WriteLine(Message);
                        });
                    actor.StartAllActors();
                

                    while (input != "exit")
                    {
                         input = Console.ReadLine();
                    }
                }
            }
        }

        private static void RunPipe(ZmqContext context)
        {
            Task.Run(() =>
            {
                var pipe = new Pipe(context);
            });
        }
    }
}
