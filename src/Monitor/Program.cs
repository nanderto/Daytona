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
                using (var monitorService = context.CreateSocket(SocketType.REP))
                {
                    monitorService.Bind(Pipe.MonitorAddressServer);

                    while (!interrupted)
                    {
                        var signal = monitorService.Receive(Pipe.ControlChannelEncoding);
                        Console.WriteLine("::> " + signal);
                        monitorService.Send("", Encoding.Unicode);
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
