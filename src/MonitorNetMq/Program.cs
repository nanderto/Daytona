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
            using (NetMQContext context = NetMQContext.Create())
            {
                
                    using (var rep = context.CreateResponseSocket())
                    {
                        rep.Bind(MonitorAddressServer);
                        while (true)
                        {
                            var signal = rep.ReceiveString(ControlChannelEncoding);
                            Console.WriteLine("::> " + signal);
                            rep.Send(string.Empty, Encoding.Unicode);
                        }
                    }
               

            }
        }

        public static string MonitorAddressClient = "tcp://localhost:5560";  ////"inproc://pubsubcontrol";//

        public static string MonitorAddressServer = "tcp://*:5560";


        public static Encoding ControlChannelEncoding = Encoding.Unicode;
    }
}
