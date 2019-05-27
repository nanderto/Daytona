// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Context.cs" company="N.K.Anderton">
// Copyright © 2015 All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Daytona
{
    using NetMQ;
    using System;

    public class Context : IDisposable
    {
        private bool disposed;

        private ConsoleMonitor consoleMonitor = null;

        public Context(NetMQContext netMqContext)
        {
            this.NetMqContext = netMqContext;
            this.Exchange = new Exchange(this.NetMqContext);
            this.Exchange.Start();
        }

        public Context(NetMQContext netMqContext, MessageSerializerFactory messageSerializerFactory)
            : this(netMqContext)
        {
            this.MessageSerializerFactory = messageSerializerFactory;
            this.ActorFactory = new Actor(netMqContext, this.MessageSerializerFactory.GetNewSerializer());
        }

        /// <summary>
        /// Creates a Context with a ConsoleMonitor to send output to stdout. it can be seen in the test results 
        /// or a console if you are using one.
        /// </summary>
        /// <param name="netMqContext"></param>
        /// <param name="messageSerializerFactory"></param>
        /// <param name="consoleMonitor"></param>
        private Context(NetMQContext netMqContext, MessageSerializerFactory messageSerializerFactory, ConsoleMonitor consoleMonitor)
        {
            this.consoleMonitor = consoleMonitor;
            this.consoleMonitor.Start(netMqContext); // this needs to be started frst other wise the attempts to send messages 
            // to it will cause a thread to hang - and likely everything else with it.
            // underlying is a request response type of connection. (waiting to receive will hang, sending message before it is 
            // ready will also cause it to hang as ther is no message to wait for.
            this.Exchange = new Exchange(netMqContext);
            this.Exchange.Start(); //the exchange has t be started befor an acto can be cerated below because the underlying forwwarder
            // needs to be created. (the underlying forwarder device binds to the end points which must occur before the actors can attempt 
            // to connect
            this.NetMqContext = netMqContext;
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
        /// Static constructor to create a context with a ConsoleMonitor to write to stdout
        /// Could probably swap this for an INterface so other places could be written to.
        /// </summary>
        /// <param name="consoleMonitor">new up a Console Monitor and pass it in</param>
        /// <returns></returns>
        public static Context Create(ConsoleMonitor consoleMonitor)
        {
            var messageSerializerFactory = new MessageSerializerFactory(() => new BinarySerializer());

            return new Context(NetMQContext.Create(), messageSerializerFactory, consoleMonitor);
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
                    if (this.consoleMonitor != null)
                    {
                        this.consoleMonitor.Stop();
                    }

                    this.NetMqContext.Dispose();
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }
    }
}