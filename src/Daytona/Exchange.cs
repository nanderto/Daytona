﻿namespace Daytona
{
    using System;
    using NetMQ;
    using NetMQ.Devices;

    /// <summary>
    /// Collects messages from a set of publishers and forwards these to a set of subscribers.
    /// </summary>
    /// <remarks>
    /// Generally used to bridge networks. E.g. read on TCP unicast and forward on multicast.
    /// This device is part of the publish-subscribe pattern. The frontend speaks to publishers
    /// and the backend speaks to subscribers.
    /// In order to use the <see cref="Exchange"/> please make sure you subscribe the FrontendSocket 
    /// using the <see cref="Exchange.FrontendSetup"/>.
    /// </remarks>
    /// <example>
    /// var device = new ForwarderDevice(ctx, "inproc://frontend", "inproc://backend");
    /// device.FrontendSetup.Subscribe("topic");
    /// </example>
    public class Exchange : DeviceBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Exchange"/> class.
        /// </summary>
        /// <param name="context">The <see cref="NetMQContext"/> to use when creating the sockets.</param>
        /// <param name="frontendBindAddress">The endpoint used to bind the frontend socket.</param>
        /// <param name="backendBindAddress">The endpoint used to bind the backend socket.</param>
        /// <param name="mode">The <see cref="DeviceMode"/> for the device.</param>
        public Exchange(NetMQContext context, string frontendBindAddress, string backendBindAddress,
            DeviceMode mode = DeviceMode.Threaded)
            : base(context.CreateXSubscriberSocket(), context.CreateXPublisherSocket(), mode)
        {

            FrontendSetup.Bind(frontendBindAddress);
            BackendSetup.Bind(backendBindAddress);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Exchange"/> class.
        /// </summary>
        /// <param name="context">The <see cref="NetMQContext"/> to use when creating the sockets.</param>
        /// <param name="poller">The <see cref="Poller"/> to use.</param>
        /// <param name="frontendBindAddress">The endpoint used to bind the frontend socket.</param>
        /// <param name="backendBindAddress">The endpoint used to bind the backend socket.</param>
        /// <param name="mode">The <see cref="DeviceMode"/> for the device.</param>		
        public Exchange(NetMQContext context, Poller poller, string frontendBindAddress, string backendBindAddress,
            DeviceMode mode = DeviceMode.Threaded)
            : base(poller, context.CreateXSubscriberSocket(), context.CreateXPublisherSocket(), mode)
        {

            FrontendSetup.Bind(frontendBindAddress);
            BackendSetup.Bind(backendBindAddress);
        }

        protected override void FrontendHandler(object sender, NetMQSocketEventArgs args)
        {
            bool more;

            do
            {
                var data = args.Socket.Receive(out more);

                if (more)
                    BackendSocket.SendMore(data);
                else
                {
                    BackendSocket.Send(data);
                }

            } while (more);
        }

        protected override void BackendHandler(object sender, NetMQSocketEventArgs args)
        {
            bool more;

            do
            {
                var data = args.Socket.Receive(out more);

                if (more)
                    FrontendSocket.SendMore(data);
                else
                {
                    FrontendSocket.Send(data);
                }

            } while (more);
        }
    }
}