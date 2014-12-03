//-----------------------------------------------------------------------
// <copyright file="DebuggingForwarder.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona
{
    using System;

    using NetMQ;
    using NetMQ.Devices;
    
    public class DebuggingForwarder : ForwarderDevice
    {
        public DebuggingForwarder(NetMQContext context, string publishAddressServer, string subscribeAddressServer, DeviceMode mode)
            : base(context, publishAddressServer, subscribeAddressServer, mode)
        {
        }

        protected override void FrontendHandler(NetMQSocketEventArgs args)
        {
            Console.WriteLine("In forwarder" + args.ToString());
            this.FrontendSocket.Forward(this.BackendSocket);
        }
    }
}