// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Context.cs" company="N.K.Anderton">
// Copyright © 2015 All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Daytona
{
    using System;

    using NetMQ;

    public class Context : IDisposable
    {
        private bool disposed;

        public Context(NetMQContext netMqContext)
        {
            this.NetMqContext = netMqContext;
            this.Exchange = new Exchange(this.NetMqContext);
            this.Exchange.Start();
            this.Poller = new Poller();
            //this.NetMQScheduler = new NetMQScheduler(netMqContext);
        }

        public Context(NetMQContext netMqContext, MessageSerializerFactory messageSerializerFactory)
            : this(netMqContext)
        {
            this.MessageSerializerFactory = messageSerializerFactory;
            this.ActorFactory = new Actor(netMqContext, this.MessageSerializerFactory.GetNewSerializer());
        }

        public Actor ActorFactory { get; set; }

        public Exchange Exchange { get; set; }

        public MessageSerializerFactory MessageSerializerFactory { get; private set; }

        public NetMQContext NetMqContext { get; set; }

        /// <summary>
        /// Create a new context should be called only once in an application, the context is passed around and provides access to the 
        /// infrastructure.
        /// </summary>
        /// <returns>Daytona Context</returns>
        public static Context Create()
        {
            var messageSerializerFactory = new MessageSerializerFactory(() => new BinarySerializer());

            return new Context(NetMQContext.Create(), messageSerializerFactory);
        }

        /// <summary>
        /// Create a new conext should be called only once in  and application, the context is passed around and provides access to the 
        /// infrastructure.
        /// pass in a Lambda to use your own serialization e.g. () => new BinarySerializer()
        /// </summary>
        /// <param name="func">Lambda to return your Serialization implementation</param>
        /// <returns>Daytona Context</returns>
        public static Context Create(Func<ISerializer> func)
        {
            var messageSerializerFactory = new MessageSerializerFactory(func);

            return new Context(NetMQContext.Create(), messageSerializerFactory);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.ActorFactory.Dispose();
                    this.Exchange.Stop(true);
                    this.Exchange.Dispose();
                    //this.NetMqContext.Dispose();

                    if (this.NetMqContext != null)
                    {
                        this.NetMqContext.Dispose();
                    }

                    if (this.Poller != null)
                    {
                        this.Poller.Stop(true);
                        this.Poller.Dispose();
                    }

                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }

        public Poller Poller { get; set; }

        public NetMQScheduler NetMQScheduler { get; set; }
    }
}