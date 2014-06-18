//-----------------------------------------------------------------------
// <copyright file="Actor.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using NProxy.Core;

    using ZeroMQ;

    /// <summary>
    /// The Actor is the coe object of the Actor framework, it is self configuring to listen for messages that come in and execute what ever 
    /// workload that is configured for it.
    /// </summary>
    /// <typeparam name="T">
    /// The object to compose with this actor
    /// </typeparam>
    public class Actor<T> : IDisposable
    {
        private static readonly object SynchLock = new object();

        private readonly Dictionary<string, Action> actorTypes = new Dictionary<string, Action>();

        private readonly ZmqContext context;

        private bool disposed;

        private ZmqSocket subscriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Actor"/> class.
        /// This is generally used when creating a actor to act as a Actor factory.
        /// </summary>
        /// <param name="context">The context.</param>
        public Actor(ZmqContext context)
        {
            this.IsRunning = false;
            this.context = context;
        }
        
        private T actor;

        public Actor(ZmqContext context, T actor)
        {
            this.IsRunning = false;
            this.context = context;
            this.actor = actor;
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            var inRoute = typeof(T).Name.Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
            this.PropertyBag = new Dictionary<string, string>();
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
        }

        public Actor(ZmqContext context, T actor, ISerializer serializer)
        {
            this.IsRunning = false;
            this.context = context;
            this.actor = actor;
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            var inRoute = typeof(T).Name.Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
            this.PropertyBag = new Dictionary<string, string>();
            this.Serializer = serializer;
        }

        public TInstance CreateInstance<TInstance>() where TInstance : class
        {
            var invocationHandler = new MessageSenderProxy<T>(this);
            var proxyFactory = new ProxyFactory();
            return proxyFactory.CreateProxy<TInstance>(Type.EmptyTypes, invocationHandler);
        }

        public event EventHandler<CallBackEventArgs> SaveCompletedEvent;
        
        private bool monitorChannelDisposed = false;
        
        private bool subscriberDisposed = false;
       
        public bool OutputChannelDisposed
        {
            get { return monitorChannelDisposed; }
            set { monitorChannelDisposed = value; }
        }

        public Delegate Callback { get; set; }

        public int Id { get; set; }

        public string InRoute { get; set; }

        public bool IsRunning { get; set; }

        public ZmqSocket MonitorChannel { get; set; }
        
        public ZmqSocket OutputChannel { get; set; }

        public string OutRoute { get; set; }

        public Dictionary<string, string> PropertyBag { get; set; }

        public ISerializer Serializer { get; set; }
        
        public Delegate Workload { get; set; }
        
        public static void Writeline(string line)
        {
            lock (SynchLock)
            {
                FileInfo fi = new FileInfo(@"c:\dev\Actor.log");
                var stream = fi.AppendText();
                stream.WriteLine(line);
                stream.Flush();
                stream.Close();
            }
        }

        public void CallBack(int result, List<IPayload> payload, Exception exception)
        {
            var eventArgs = new CallBackEventArgs
            {
                Result = result,
                Error = exception,
                Payload = payload
            };

            this.SaveCompletedEvent(this, eventArgs);
        }

        /// <summary>
        /// Create and start a new actor by invoking the Lambda registered with the name provided. This new actor is 
        /// created on its own thread
        /// </summary>
        /// <param name="name">name of the actor</param>
        public void CreateNewActor(string name)
        {
            Action del;
            if (this.actorTypes.TryGetValue(name, out del))
            {
                Task.Run(() =>
                {
                    del.DynamicInvoke();
                });
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        public Actor<T> RegisterActor<TObject>(TObject objectToRun) where TObject : class
        {
            var nameIndex = typeof(TObject).ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
            this.actorTypes.Add(
                nameIndex,
                () =>
                {
                    using (var actorToRun = new Actor<TObject>(this.context, objectToRun))
                    {
                        actorToRun.Start();
                    }
                });

            return this;
        }
 

        public void SendMessage(string address, byte[] message, ISerializer serializer, ZmqSocket socket)
        {
            SendMessage(serializer.Encoding.GetBytes(address), message, socket);
        }

        public void SendMessage(byte[] address, byte[] message, ZmqSocket socket)
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(address));
            zmqMessage.Append(new Frame(message));
            socket.SendMessage(zmqMessage);
        }

        public void SendOneMessageOfType<T>(string address, T message, ISerializer serializer, ZmqSocket socket) where T : IPayload
        {
            ZmqMessage zmqMessage = new ZmqMessage();
            zmqMessage.Append(new Frame(serializer.GetBuffer(address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer(message)));
            ////var replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            socket.SendMessage(zmqMessage);
            ////this.sendControlChannel.Send("Just sent message to " + address + " Message is: " + message, Pipe.ControlChannelEncoding);
            ////replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            ////Actor.Writeline(replySignal);
        }

        /// <summary>
        /// Start is called on all actors to have them listen for messages, they will receive and process one message 
        /// at a time
        /// </summary>
        public void Start()
        {
            while (true)
            {
                if (this.subscriberDisposed != true)
                {
                    var zmqmessage = this.subscriber.ReceiveMessage();
                    var frameContents = zmqmessage.Select(f => this.Serializer.Encoding.GetString(f.Buffer)).ToList();

                    if (frameContents.Count > 1)
                    {
                        var message = frameContents[1];

                        if (message != null)
                        {
                            if (string.IsNullOrEmpty(this.OutRoute))
                            {
                                object[] inputParameters = new object[2];
                                inputParameters[0] = message;
                                inputParameters[1] = this.InRoute;
                                this.Workload.DynamicInvoke(inputParameters);
                            }
                            else
                            {
                                if (this.PropertyBag != null)
                                {
                                    object[] inputParameters = new object[5];
                                    inputParameters[0] = message;
                                    inputParameters[1] = this.InRoute;
                                    inputParameters[2] = this.OutRoute;
                                    inputParameters[3] = this.OutputChannel;
                                    inputParameters[4] = this;
                                    this.Workload.DynamicInvoke(inputParameters);
                                }
                                else
                                {
                                    object[] inputParameters = new object[4];
                                    inputParameters[0] = message;
                                    inputParameters[1] = this.InRoute;
                                    inputParameters[2] = this.OutRoute;
                                    inputParameters[3] = this.OutputChannel;
                                    this.Workload.DynamicInvoke(inputParameters);
                                }
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
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
                T message = this.ReceiveMessage<T>(this.subscriber, out zmqmessage, out address, out stop, out messageAsBytes, this.Serializer);
                if (stop == true)
                {
                    this.IsRunning = false;
                }

                this.WriteLineToMonitor("Received message");
                
                if (message != null)
                {
                    object[] parameters = new object[6];
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
        /// Create and start all actors that are registered in the collection of subActors. this 
        /// is done by invoking the Lambda registered in the collection.
        /// Each actor is started on its own thread
        /// </summary>
        public void StartAllActors()
        {
            foreach (var item in this.actorTypes)
            {
                Task.Run(() =>
                {
                    item.Value.DynamicInvoke();
                });
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

        private T ReceiveMessage<T>(ZmqSocket subscriber, out ZmqMessage zmqMessage, out string address, out bool stopSignal, out byte[] messageAsBytes, ISerializer serializer)
        {
            stopSignal = false;
            T result = default(T);
            ZmqMessage zmqOut = new ZmqMessage();
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
                        this.SendMessage(Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress),Pipe.ControlChannelEncoding.GetBytes("SHUTTINGDOWN"), this.OutputChannel);
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

        private void SetUpMonitorChannel(ZmqContext context)
        {
            this.MonitorChannel = context.CreateSocket(SocketType.REQ);
            this.MonitorChannel.Connect(Pipe.MonitorAddressClient);
        }

        private void SetUpOutputChannel(ZmqContext context)
        {
            this.OutputChannel = context.CreateSocket(SocketType.PUB);
            this.OutputChannel.Connect(Pipe.PublishAddressClient);

            this.WriteLineToMonitor("Set up output channel on " + Pipe.PublishAddressClient + " Default sending on: " + this.OutRoute);
            ////if(this.sendControlChannel == null)
            ////{
            ////    this.sendControlChannel = context.CreateSocket(SocketType.REQ);
            ////    this.sendControlChannel.Connect(Pipe.PubSubControlBackAddressClient);
            ////}
            ////this.sendControlChannel.Send("Actor OutputChannel connected, Sending on " + Pipe.PublishAddressClient, Pipe.ControlChannelEncoding);
            ////var replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            ////Actor.Writeline(replySignal);
        }

        /// <summary>
        /// Creates a Socket and connects it to a endpoint that is bound to a Pipe
        /// </summary>
        /// <param name="context">The ZeroMQ context required to create the receivers</param>
        private void SetUpReceivers(ZmqContext context)
        {
            this.subscriber = context.CreateSocket(SocketType.SUB);
            this.subscriber.Connect(Pipe.SubscribeAddressClient);
            this.subscriber.Subscribe(this.Serializer.GetBuffer(this.InRoute));
            this.MonitorChannel.Send("Set up Receive channel on " + Pipe.SubscribeAddressClient + " listening on: " + this.InRoute, Pipe.ControlChannelEncoding);
            var signal = this.MonitorChannel.Receive(Pipe.ControlChannelEncoding);
            this.SendMessage(Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress), Pipe.ControlChannelEncoding.GetBytes("ADDSUBSCRIBER"), this.OutputChannel);
        }

        private void SetUpReceivers(ZmqContext context, string route)
        {
            this.InRoute = route;
            this.SetUpReceivers(context);
        }       
    }
}