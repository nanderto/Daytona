using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorNetMq
{

    using NetMQ;

    using TestHelpers;

    class Program
    {
        private static void Main(string[] args)
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(ConsoleCancelHandler);
            Console.WriteLine("::>Ready ");

            using (NetMQContext context = NetMQContext.Create())
            {
                using (var rep = context.CreateResponseSocket())
                {
                    rep.Bind(MonitorAddressServer);
                    while (!interrupted)
                    {
                        var signal = rep.ReceiveString(ControlChannelEncoding, TimeSpan.FromMilliseconds(30));
                        if (!string.IsNullOrEmpty(signal))
                        {
                            Console.WriteLine("::> " + signal);
                            rep.Send(string.Empty, Encoding.Unicode);
                                
                        }
                    }
                }
            }
        }

        public static string MonitorAddressClient = "tcp://localhost:5560";  ////"inproc://pubsubcontrol";//

        public static string MonitorAddressServer = "tcp://*:5560";


        public static Encoding ControlChannelEncoding = Encoding.Unicode;

        static bool interrupted = false;

        static void ConsoleCancelHandler(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            interrupted = true;
        }
    }


}
