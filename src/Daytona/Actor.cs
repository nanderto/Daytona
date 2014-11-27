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
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using NProxy.Core;

    using ZeroMQ;

    //public class Actor : Actor<T>
    //{
    //    #region Fields

    //    private ZmqContext context;

    //    #endregion

    //    #region Constructors and Destructors

    //    public Actor(ZmqContext context)
    //    {
    //        // TODO: Complete member initialization
    //        this.context = context;
    //    }

    //    #endregion

    //    #region Public Methods and Operators

    //    public virtual Actor<TObject> RegisterActor<TObject>(TObject objectToRun) where TObject : class
    //    {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}

    /// <summary>
    ///     The Actor is the coe object of the Actor framework, it is self configuring to listen for messages that come in and
    ///     execute what ever
    ///     workload that is configured for it.
    /// </summary>
    /// <typeparam name="T">
    ///     The object to compose with this actor
    /// </typeparam>
    [Serializable]
    public class Actor<T> : IDisposable
        where T : class
    {
        private static readonly object SynchLock = new object();
        
        private readonly Dictionary<string, Action> actorTypes = new Dictionary<string, Action>();

        [NonSerialized]
        private readonly ZmqContext context;

        private bool disposed;

        private T model;

        [NonSerialized]
        private ZmqSocket monitorChannel;

        private bool monitorChannelDisposed = false;

        [NonSerialized]
        private ZmqSocket outputChannel;

        [NonSerialized]
        private ISerializer serializer;

        [NonSerialized]
        public ZmqSocket subscriber;

        private bool subscriberDisposed = false;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="Actor" /> class.
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
        }

        public Actor(ZmqContext context, ISerializer serializer)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            var inRoute = typeof(T)
                .FullName.Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace("_", string.Empty)
                .Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(ZmqContext context, T model)
        {
            this.IsRunning = false;
            this.context = context;
            this.model = model;
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            var inRoute =
                typeof(T).Name.Replace("{", string.Empty)
                    .Replace("}", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
            this.PropertyBag = new Dictionary<string, string>();
        }

        public Actor(ZmqContext context, T model, ISerializer serializer)
            : this(context, model)
        {
            this.Serializer = serializer;
        }

        public event EventHandler<CallBackEventArgs> SaveCompletedEvent;
        
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

        public TInterface CreateInstance<TInterface>() where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy<T>(this);
            var proxyFactory = new ProxyFactory();
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
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

        //public Actor<T> RegisterActor<TObject>(TObject objectToRun) where TObject : class
        //{
        //    var nameIndex =
        //        typeof(TObject).ToString()
        //            .Replace("{", string.Empty)
        //            .Replace("}", string.Empty)
        //            .Replace("_", string.Empty)
        //            .Replace(".", string.Empty);
        //    this.actorTypes.Add(
        //        nameIndex, 
        //        () =>
        //            {
        //                using (var actorToRun = new Actor<TObject>(this.context, objectToRun))
        //                {
        //                    actorToRun.Start();
        //                }
        //            });

        //    return this;
        //}

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

        /// <summary>
        ///     Start is called on all actors to have them listen for messages, they will receive and process one message
        ///     at a time
        /// </summary>
        //public void Start()
        //{
        //    while (true)
        //    {
        //        if (this.subscriberDisposed != true)
        //        {
        //            var zmqmessage = this.subscriber.ReceiveMessage();
        //            var frameContents = zmqmessage.Select(f => this.Serializer.Encoding.GetString(f.Buffer)).ToList();

        //            if (frameContents.Count > 1)
        //            {
        //                var message = frameContents[1];

        //                if (message != null)
        //                {
        //                    if (string.IsNullOrEmpty(this.OutRoute))
        //                    {
        //                        var inputParameters = new object[2];
        //                        inputParameters[0] = message;
        //                        inputParameters[1] = this.InRoute;
        //                        this.Workload.DynamicInvoke(inputParameters);
        //                    }
        //                    else
        //                    {
        //                        if (this.PropertyBag != null)
        //                        {
        //                            var inputParameters = new object[5];
        //                            inputParameters[0] = message;
        //                            inputParameters[1] = this.InRoute;
        //                            inputParameters[2] = this.OutRoute;
        //                            inputParameters[3] = this.OutputChannel;
        //                            inputParameters[4] = this;
        //                            this.Workload.DynamicInvoke(inputParameters);
        //                        }
        //                        else
        //                        {
        //                            var inputParameters = new object[4];
        //                            inputParameters[0] = message;
        //                            inputParameters[1] = this.InRoute;
        //                            inputParameters[2] = this.OutRoute;
        //                            inputParameters[3] = this.OutputChannel;
        //                            this.Workload.DynamicInvoke(inputParameters);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            break;
        //        }
        //    }
        //}


        public void Start()
        {
            bool stop = false;
            while (stop == false)
            {
                this.IsRunning = true;
                string address = string.Empty;
                ZmqMessage zmqmessage = null;

                this.WriteLineToMonitor("Waiting for message");

                byte[] messageAsBytes = null;
                stop = this.ReceiveMessage(this.subscriber);
                if (stop)
                {
                    this.IsRunning = false;
                }

                this.WriteLineToMonitor("Received message");
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
        
        public void SendMessage(object[] parameters, MethodInfo methodInfo)
        {
            var zmqMessage = new ZmqMessage();
            var address = typeof(T).FullName;
            zmqMessage.Append(new Frame(this.Serializer.GetBuffer(address)));
            zmqMessage.Append(new Frame(this.Serializer.GetBuffer("Process")));

            // var binarySerializer = new BinarySerializer();
            // var buffer = binarySerializer.GetBuffer(methodInfo);
            var serializedMethodInfo = this.Serializer.GetBuffer(methodInfo);
            zmqMessage.Append(new Frame(serializedMethodInfo));
            zmqMessage.Append(
                new Frame(this.Serializer.GetBuffer(string.Format("ParameterCount:{0}", parameters.Length))));
            foreach (var parameter in parameters)
            {
                zmqMessage.Append(this.Serializer.GetBuffer(parameter.GetType()));
                zmqMessage.Append(this.Serializer.GetBuffer(parameter));
            }

            this.OutputChannel.SendMessage(zmqMessage);
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


        public bool ReceiveMessage(ZmqSocket subscriber)
        {
            var stopSignal = false;
            var zmqOut = new ZmqMessage();
            bool hasMore = true;
            //var address = string.Empty;
           // byte[] messageAsBytes = null;
            int frameCount = 0;
            MethodInfo methodinfo = null;
            var methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            var typeParameter = true;
            Type type = null;
            MethodInfo returnedMethodInfo = null;

            while (hasMore)
            {
                Frame frame = subscriber.ReceiveFrame();

                stopSignal = UnPackFrame(frameCount, serializer, frame, ref methodinfo, methodParameters, ref typeParameter, ref type);
                if (frameCount == 2)
                {
                    returnedMethodInfo = methodinfo;
                }
                frameCount++;
                zmqOut.Append(new Frame(frame.Buffer));
                hasMore = subscriber.ReceiveMore;
            }

            var target = (T)Activator.CreateInstance(typeof(T));
            var result = returnedMethodInfo.Invoke(target, methodParameters.ToArray());
            return stopSignal;
        }

        public static bool UnPackFrame(
            int frameCount,
            BinarySerializer serializer,
            Frame frame,
            ref MethodInfo methodinfo,
            List<object> methodParameters,
            ref bool typeParameter,
            ref Type type)
        {
            bool stopSignal = false;
            string address;
            byte[] messageAsBytes;
            int numberOfParameters;
            
            if (frameCount == 0)
            {
                address = serializer.GetString(frame.Buffer);
            }

            if (frameCount == 1)
            {
                messageAsBytes = frame.Buffer;
                string stopMessage = serializer.GetString(messageAsBytes);
                if (stopMessage.ToLower() == "stop")
                {
                    stopSignal = true;
                }
            }

            if (frameCount == 2)
            {
                methodinfo = (MethodInfo)serializer.Deserializer(frame.Buffer, typeof(MethodInfo));
            }

            if (frameCount == 3)
            {
                numberOfParameters = int.Parse(serializer.GetString(frame.Buffer).Replace("ParameterCount:", string.Empty));
            }

            if (frameCount > 3)
            {
                if (typeParameter)
                {
                    type = (Type)serializer.Deserializer(frame.Buffer, typeof(Type));
                    typeParameter = false;
                }
                else
                {
                    var parameter = serializer.Deserializer(frame.Buffer, type);
                    methodParameters.Add(parameter);
                    typeParameter = true;
                }
            }
            return stopSignal;
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

        private void SetUpReceivers(ZmqContext context, string route)
        {
            this.InRoute = route;
            this.SetUpReceivers(context);
        }
    }
}