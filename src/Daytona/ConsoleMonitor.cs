using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    public class ConsoleMonitor
    {
        Task Monitor = null;

        public ConsoleMonitor()
        {
            
        }


        public void Start(NetMQContext context)
        {
            Console.WriteLine("::>Ready ");
            Monitor = Task.Run(() =>
            {
                using (var rep = context.CreateResponseSocket())
                {
                    rep.Bind(MonitorAddressServer);
                    while (!interrupted)
                    {
                        try
                        {
                            var signal = rep.ReceiveString(ControlChannelEncoding, TimeSpan.FromMilliseconds(30));
                            if (!string.IsNullOrEmpty(signal))
                            {
                                Console.WriteLine("::> " + signal);
                                rep.Send(string.Empty, Encoding.Unicode);
                            }
                        }
                        catch (NetMQ.TerminatingException)
                        { }
                        finally
                        { }
                    }
                } 
            });
        }

        public void Stop()
        {
            interrupted = true;
        }

        public static string MonitorAddressClient = "tcp://localhost:5560";

        public static string MonitorAddressServer = "tcp://*:5560";

        public static Encoding ControlChannelEncoding = Encoding.Unicode;

        static bool interrupted = false;
    }
}
