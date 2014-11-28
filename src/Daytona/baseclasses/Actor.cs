// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Actor.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    using ZeroMQ;

    [Serializable]
    public class Actor : IDisposable
    {
        [NonSerialized]
        public ZmqSocket subscriber;

        [NonSerialized]
        protected ZmqContext context;

        private readonly Dictionary<string, Action> actorTypes = new Dictionary<string, Action>();

        private static readonly object SynchLock = new object();

        private bool disposed;

        [NonSerialized]
        private ZmqSocket monitorChannel;

        private bool monitorChannelDisposed = false;

        [NonSerialized]
        private ZmqSocket outputChannel;

        [NonSerialized]
        private ISerializer serializer;

        private bool subscriberDisposed = false;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="Actor{T}" /> class.
        ///     This is generally used when creating a actor to act as a Actor factory.
        /// </summary>
        /// <param name="context">The context.</param>
        public Actor(ZmqContext context)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.PropertyBag = new Dictionary<string, string>();
        }

        public Actor(ISerializer serializer)
        {
            this.IsRunning = false;
            this.Serializer = serializer;
        }

        public Actor(ZmqContext context, ISerializer serializer)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
        }

        public Actor(ZmqContext context, ISerializer serializer, string inRoute, Action<Actor> workload)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, string>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public virtual event EventHandler<CallBackEventArgs> SaveCompletedEvent;
        
        public Delegate Callback { get; set; }

        public int Id { get; set; }

        public string InRoute { get; set; }

        public bool IsRunning { get; set; }

        public ZmqSocket MonitorChannel
        {
            get
            {
                return this.monitorChannel;
            }

            set
            {
                this.monitorChannel = value;
            }
        }

        public string OutRoute { get; set; }

        public ZmqSocket OutputChannel
        {
            get
            {
                return this.outputChannel;
            }

            set
            {
                this.outputChannel = value;
            }
        }

        public bool OutputChannelDisposed
        {
            get
            {
                return this.monitorChannelDisposed;
            }

            set
            {
                this.monitorChannelDisposed = value;
            }
        }

        public Dictionary<string, string> PropertyBag { get; set; }

        public ISerializer Serializer
        {
            get
            {
                return this.serializer;
            }

            set
            {
                this.serializer = value;
            }
        }

        public Delegate Workload { get; set; }
        
        public static void Writeline(string line)
        {
            lock (SynchLock)
            {
                var fi = new FileInfo(@"c:\dev\Actor.log");
                var stream = fi.AppendText();
                stream.WriteLine(line);
                stream.Flush();
                stream.Close();
            }
        }

        public void CallBack(int result, List<IPayload> payload, Exception exception)
        {
            var eventArgs = new CallBackEventArgs { Result = result, Error = exception, Payload = payload };

            this.SaveCompletedEvent(this, eventArgs);
        }

        /// <summary>
        ///     Create and start a new actor by invoking the Lambda registered with the name provided. This new actor is
        ///     created on its own thread
        /// </summary>
        /// <param name="name">name of the actor</param>
        public void CreateNewActor(string name)
        {
            Action del;
            if (this.actorTypes.TryGetValue(name, out del))
            {
                Task.Run(() => { del.DynamicInvoke(); });
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Actor RegisterActor<T>(
            string name, 
            string inRoute, 
            string outRoute, 
            ISerializer serializer, 
            Action<Actor> workload) where T : IPayload
        {
            this.actorTypes.Add(
                name, 
                () =>
                    {
                        using (var actor = new Actor(this.context, serializer, inRoute, workload))
                        {
                            actor.Start<T>();
                        }
                    });
            return this;
        }

        public void SendMessage(string address, byte[] message, ISerializer serializer, ZmqSocket socket)
        {
            this.SendMessage(serializer.Encoding.GetBytes(address), message, socket);
        }

        public void SendMessage(byte[] address, byte[] message, ZmqSocket socket)
        {
            var zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(address));
            zmqMessage.Append(new Frame(message));
            socket.SendMessage(zmqMessage);
        }

        public void SendOneMessageOfType<T>(string address, T message, ISerializer serializer, ZmqSocket socket)
            where T : IPayload
        {
            var zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(serializer.GetBuffer(address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer(message)));

            ////var replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            socket.SendMessage(zmqMessage);

            ////this.sendControlChannel.Send("Just sent message to " + address + " Message is: " + message, Pipe.ControlChannelEncoding);
            ////replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            ////Actor.Writeline(replySignal);
        }

        public void Start<T>() where T : IPayload
        {
            bool stop = false;
            while (stop == false)
            {
                this.IsRunning = true;
                string address = string.Empty;
                ZmqMessage zmqmessage = null;

                this.WriteLineToMonitor("Waiting for message");

                byte[] messageAsBytes = null;
                var message = this.ReceiveMessage<T>(
                    this.subscriber, 
                    out zmqmessage, 
                    out address, 
                    out stop, 
                    out messageAsBytes, 
                    this.Serializer);
                if (stop == true)
                {
                    this.IsRunning = false;
                }

                this.WriteLineToMonitor("Received message");

                if (message != null)
                {
                    var parameters = new object[6];
                    parameters[0] = message;
                    parameters[1] = messageAsBytes;
                    parameters[2] = address;
                    parameters[3] = this.OutRoute;
                    parameters[4] = this.OutputChannel;
                    parameters[5] = this;
                    this.Workload.DynamicInvoke(parameters);
                }
            }

            this.WriteLineToMonitor("Exiting actor");
        }

        /// <summary>
        ///     Create and start all actors that are registered in the collection of subActors. this
        ///     is done by invoking the Lambda registered in the collection.
        ///     Each actor is started on its own thread
        /// </summary>
        public void StartAllActors()
        {
            foreach (var item in this.actorTypes)
            {
                Task.Run(() => { item.Value.DynamicInvoke(); });
            }
        }

        public void WriteLineToMonitor(string line)
        {
            if (this.monitorChannelDisposed == false)
            {
                this.MonitorChannel.Send(line, Pipe.ControlChannelEncoding);
                var signal = this.MonitorChannel.Receive(Pipe.ControlChannelEncoding);
            }
        }
        
        protected void SetUpMonitorChannel(ZmqContext context)
        {
            this.MonitorChannel = context.CreateSocket(SocketType.REQ);
            this.MonitorChannel.Connect(Pipe.MonitorAddressClient);
        }

        protected void SetUpOutputChannel(ZmqContext context)
        {
            this.OutputChannel = context.CreateSocket(SocketType.PUB);
            this.OutputChannel.Connect(Pipe.PublishAddressClient);

            this.WriteLineToMonitor(
                "Set up output channel on " + Pipe.PublishAddressClient + " Default sending on: " + this.OutRoute);

            ////if(this.sendControlChannel == null)
            ////{
            ////    this.sendControlChannel = context.CreateSocket(SocketType.REQ);
            ////    this.sendControlChannel.Connect(Pipe.PubSubControlBackAddressClient);
            ////}
            ////this.sendControlChannel.Send("Actor OutputChannel connected, Sending on " + Pipe.PublishAddressClient, Pipe.ControlChannelEncoding);
            ////var replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            ////Actor.Writeline(replySignal);
        }

        protected void SetUpReceivers(ZmqContext context, string route)
        {
            this.InRoute = route;
            this.SetUpReceivers(context);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.subscriber != null)
                    {
                        this.subscriberDisposed = true;
                        this.subscriber.Dispose();
                    }

                    if (this.OutputChannel != null)
                    {
                        this.OutputChannelDisposed = true;
                        this.OutputChannel.Dispose();
                    }

                    if (this.MonitorChannel != null)
                    {
                        this.monitorChannelDisposed = true;
                        this.MonitorChannel.Dispose();
                    }
                }

                //// There are no unmanaged resources to release, but
                //// if we add them, they need to be released here.
            }

            this.disposed = true;
        }

        private T ReceiveMessage<T>(
            ZmqSocket subscriber, 
            out ZmqMessage zmqMessage, 
            out string address, 
            out bool stopSignal, 
            out byte[] messageAsBytes, 
            ISerializer serializer)
        {
            stopSignal = false;
            T result = default(T);
            var zmqOut = new ZmqMessage();
            bool hasMore = true;
            address = string.Empty;
            messageAsBytes = null;
            int i = 0;
            while (hasMore)
            {
                Frame frame = subscriber.ReceiveFrame();
                if (i == 0)
                {
                    address = serializer.GetString(frame.Buffer);
                }

                if (i == 1)
                {
                    messageAsBytes = frame.Buffer;
                    string stopMessage = serializer.GetString(messageAsBytes);
                    this.WriteLineToMonitor("Message: " + stopMessage);
                    if (stopMessage.ToLower() == "stop")
                    {
                        Writeline("received stop");
                        this.SendMessage(
                            Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress), 
                            Pipe.ControlChannelEncoding.GetBytes("SHUTTINGDOWN"), 
                            this.OutputChannel);
                        stopSignal = true;
                    }
                    else
                    {
                        result = serializer.Deserializer<T>(stopMessage);
                    }
                }

                i++;
                zmqOut.Append(new Frame(frame.Buffer));
                hasMore = subscriber.ReceiveMore;
            }

            zmqMessage = zmqOut;
            return result;
        }

        /// <summary>
        ///     Creates a Socket and connects it to a endpoint that is bound to a Pipe
        /// </summary>
        /// <param name="context">The ZeroMQ context required to create the receivers</param>
        private void SetUpReceivers(ZmqContext context)
        {
            this.subscriber = context.CreateSocket(SocketType.SUB);
            this.subscriber.Connect(Pipe.SubscribeAddressClient);
            this.subscriber.Subscribe(this.Serializer.GetBuffer(this.InRoute));
            this.MonitorChannel.Send(
                "Set up Receive channel on " + Pipe.SubscribeAddressClient + " listening on: " + this.InRoute, 
                Pipe.ControlChannelEncoding);
            var signal = this.MonitorChannel.Receive(Pipe.ControlChannelEncoding);
            this.SendMessage(
                Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress), 
                Pipe.ControlChannelEncoding.GetBytes("ADDSUBSCRIBER"), 
                this.OutputChannel);
        }
    }
}