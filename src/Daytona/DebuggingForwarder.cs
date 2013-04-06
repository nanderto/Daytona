//-----------------------------------------------------------------------
// <copyright file="DebuggingForwarder.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;
    using ZeroMQ;
    using ZeroMQ.Devices;

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