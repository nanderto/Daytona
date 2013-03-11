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
                using (var syncService = context.CreateSocket(SocketType.REP))
                {
                    syncService.Connect(Pipe.PubSubControlFrontAddressClient);

                    while (!interrupted)
                    {
                        var signal = syncService.Receive(Encoding.Unicode);
                        Console.WriteLine("Signal Recieved: " + signal);
                        syncService.Send("", Encoding.Unicode);

                       
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
