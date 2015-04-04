using Daytona;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelpers;

namespace Monitor
{
    using NetMQ;

    class Program
    {
        static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            using (var context = NetMQContext.Create())
            {
                using (var monitorService = context.CreateResponseSocket())
                {
                    monitorService.Bind(Pipe.MonitorAddressServer);

                    while (!interrupted)
                    {
                        var signal = monitorService.ReceiveString(Exchange.ControlChannelEncoding);
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
