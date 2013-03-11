using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroMQ;
using ZeroMQ.Devices;

namespace Daytona
{
    public class DebuggingForwarder : ForwarderDevice
    {
        public DebuggingForwarder(ZmqContext context, string publishAddressServer, string subscribeAddressServer, DeviceMode mode)
            : base(context, publishAddressServer, subscribeAddressServer, mode)
        {

        }
        protected override void FrontendHandler(SocketEventArgs args)
        {
            Console.WriteLine("In forwarder" + args.ToString());
            this.FrontendSocket.Forward(this.BackendSocket);
        }
    }
}
