using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorNetMq
{
    using Daytona;

    using NetMQ;

    using TestHelpers;

    class Program
    {
        private static void Main(string[] args)
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                using (var rep = contex.CreateSubscriberSocket())
                {
                    rep.Bind(Pipe.PublishAddressServer);
                    while (true)
                    {
                        var str = rep.ReceiveString();
                        Console.Out.WriteLine(str);

                    }
                }

            }
        }
    }
}
