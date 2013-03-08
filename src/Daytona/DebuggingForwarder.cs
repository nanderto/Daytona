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
        public DebuggingForwarder(ZmqContext context, string PublishAddressServer, string SubscribeAddressServer, DeviceMode mode)
            : base(context, PublishAddressServer, SubscribeAddressServer, mode)
        {

        }
        protected override void FrontendHandler(SocketEventArgs args)
        {
            this.FrontendSocket.Forward(this.BackendSocket);
        }
    }
}
