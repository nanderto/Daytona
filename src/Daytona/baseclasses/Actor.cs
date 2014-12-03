﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Actor.cs" company="">
//   
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    using NetMQ;

    [Serializable]
    public class Actor : IDisposable
    {
        [NonSerialized]
        public NetMQSocket subscriber;

        [NonSerialized]
        public NetMQContext context;

        public readonly Dictionary<string, Action> actorTypes = new Dictionary<string, Action>();

        private static readonly object SynchLock = new object();

        private bool disposed;

        [NonSerialized]
        private NetMQSocket monitorChannel;

        private bool monitorChannelDisposed = false;

        [NonSerialized]
        private NetMQSocket outputChannel;

        [NonSerialized]
        private ISerializer serializer;

        private bool subscriberDisposed = false;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="Actor{T}" /> class.
        ///     This is generally used when creating a actor to act as a Actor factory.
        /// </summary>
        /// <param name="context">The context.</param>
        public Actor(NetMQContext context)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.PropertyBag = new Dictionary<string, object>();
        }

        public Actor(ISerializer serializer)
        {
            this.IsRunning = false;
            this.Serializer = serializer;
        }

        public Actor(NetMQContext context, ISerializer serializer)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
        }

        public Actor(NetMQContext context, ISerializer serializer, string inRoute)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, ISerializer serializer, string name, string inRoute, Action<Actor> workload)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }


        public Actor(NetMQContext context, ISerializer serializer, string name, string inRoute, Action<string, List<object>, Actor> workload)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, ISerializer serializer, string name, string inRoute, Action<string, MethodInfo, List<object>, Actor> workload)
        {
            this.IsRunning = false;
            this.context = context;
            this.Serializer = serializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public virtual event EventHandler<CallBackEventArgs> SaveCompletedEvent;
        
        public Delegate Callback { get; set; }

        public int Id { get; set; }

        public string InRoute { get; set; }

        public bool IsRunning { get; set; }

        public NetMQSocket MonitorChannel
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

        public string Name { get; set; }

        public string OutRoute { get; set; }

        public NetMQSocket OutputChannel
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

        public Dictionary<string, object> PropertyBag { get; set; }

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

        public Actor RegisterActor<T>(string name, string inRoute, string outRoute, ISerializer serializer, Action<Actor> workload) where T : IPayload
        {
            this.actorTypes.Add(
                name, 
                () =>
                    {
                        using (var actor = new Actor(this.context, serializer, name, inRoute, workload))
                        {
                            actor.Start<T>();
                        }
                    });
            return this;
        }


        public Actor RegisterActor(string name, string inRoute, string outRoute, ISerializer serializer, Action<string, List<object>, Actor> workload) 
        {
            this.actorTypes.Add(
                name,
                () =>
                {
                    using (var actor = new Actor(this.context, serializer, name, inRoute, workload))
                    {
                        actor.Start();
                    }
                });
            return this;
        }


        public Actor RegisterActor(string name, string inRoute, string outRoute, ISerializer serializer, Action<string, MethodInfo, List<object>, Actor> workload)
        {
            this.actorTypes.Add(
                name,
                () =>
                {
                    using (var actor = new Actor(this.context, serializer, name, inRoute, workload))
                    {
                        actor.Start();
                    }
                });
            return this;
        }

        public void SendMessage(string address, byte[] message, ISerializer serializer, NetMQSocket socket)
        {
            this.SendMessage(serializer.Encoding.GetBytes(address), message, socket);
        }

        public void SendMessage(byte[] address, byte[] message, NetMQSocket socket)
        {
            var netMQMessage = new NetMQMessage();
            netMQMessage.Append(new NetMQFrame(address));
            netMQMessage.Append(new NetMQFrame(message));
            socket.SendMessage(netMQMessage);
        }

        public void SendOneMessageOfType<T>(string address, T message, ISerializer serializer, NetMQSocket socket)
            where T : IPayload
        {
            var netMQMessage = new NetMQMessage();
            netMQMessage.Append(new NetMQFrame(serializer.GetBuffer(address)));
            netMQMessage.Append(new NetMQFrame(serializer.GetBuffer(message)));

            ////var replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            socket.SendMessage(netMQMessage);

            ////this.sendControlChannel.Send("Just sent message to " + address + " Message is: " + message, Pipe.ControlChannelEncoding);
            ////replySignal = this.sendControlChannel.Receive(Pipe.ControlChannelEncoding);
            ////Actor.Writeline(replySignal);
        }

        public virtual void Start()
        {
            bool stop = false;
            while (stop == false)
            {
                this.IsRunning = true;
                string address = string.Empty;
                NetMQMessage NetMQMessage = null;

                this.WriteLineToMonitor("Waiting for message");

                byte[] messageAsBytes = null;
                try
                {
                    stop = this.ReceiveMessage(this.subscriber);
                    this.WriteLineToMonitor("Received message");
                }
                catch (SerializationException se)
                {
                    this.WriteLineToMonitor(string.Format("Serialization Error: {0}", se.ToString()));
                    ////skip to end of message
                    bool more;
                    do
                    {
                        var data = this.subscriber.Receive(out more);
                    
                    } while (more);
                }
                
                if (stop)
                {
                    this.IsRunning = false;
                }
            }

            this.WriteLineToMonitor("Exiting actor");
        }

        public virtual bool ReceiveMessage(NetMQSocket subscriber)
        {
            var stopSignal = false;
            var zmqOut = new NetMQMessage();
            bool hasMore = true;

            // var address = string.Empty;
            // byte[] messageAsBytes = null;
            int frameCount = 0;
            MethodInfo methodinfo = null;
            var methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            var typeParameter = true;
            Type type = null;
            MethodInfo returnedMethodInfo = null;
            string messageType = string.Empty, returnedMessageType = string.Empty;
            string address = string.Empty;
            var returnedAddress = string.Empty;
            var exceptionThrown = false;
            var buffer = subscriber.Receive(out hasMore);

            while (hasMore)
            {
                
                try
                {
                    stopSignal = UnPackNetMQFrame(frameCount, serializer, buffer, out address, ref methodinfo, methodParameters, ref typeParameter, ref type, out messageType);
                }
                catch (SerializationException se)
                {
                    this.WriteLineToMonitor(string.Format("Serialization Error: {0}", se.Message));
                    exceptionThrown = true;
                }

                if (frameCount == 0)
                {
                    returnedAddress = address;
                }

                if (frameCount == 1)
                {
                    returnedMessageType = messageType;
                }

                if (frameCount == 2)
                {
                    returnedMethodInfo = methodinfo;
                }

                frameCount++;
                zmqOut.Append(new NetMQFrame(buffer));
                buffer = subscriber.Receive(out hasMore);
            }

            //if (!exceptionThrown)
            //{
                var inputParameters = new object[4];
                inputParameters[0] = returnedAddress;
                inputParameters[1] = returnedMethodInfo;
                inputParameters[2] = methodParameters;
                inputParameters[3] = this;

                this.Workload.DynamicInvoke(inputParameters);
            //}
            //else
            //{
            //    var target = (T)Activator.CreateInstance(typeof(T));
            //    var result = returnedMethodInfo.Invoke(target, methodParameters.ToArray());
            //}
            return stopSignal;
        }

        public static bool UnPackNetMQFrame(int frameCount, BinarySerializer serializer, byte[] buffer, out string address, ref MethodInfo methodinfo, List<object> methodParameters, ref bool typeParameter, ref Type type, out string messageType)
        {
            messageType = string.Empty;
            bool stopSignal = false;
            address = string.Empty;
            byte[] messageAsBytes;
            int numberOfParameters;

            if (frameCount == 0)
            {
                address = serializer.GetString(buffer);
            }

            if (frameCount == 1)
            {
                messageAsBytes = buffer;
                messageType = serializer.GetString(messageAsBytes);
                if (messageType.ToLower() == "stop")
                {
                    stopSignal = true;
                }
            }

            if (frameCount == 2)
            {
                methodinfo = (MethodInfo)serializer.Deserializer(buffer, typeof(MethodInfo));
            }

            if (frameCount == 3)
            {
                numberOfParameters =
                    int.Parse(serializer.GetString(buffer).Replace("ParameterCount:", string.Empty));
            }

            if (frameCount > 3)
            {
                if (typeParameter)
                {
                    type = (Type)serializer.Deserializer(buffer, typeof(Type));
                    typeParameter = false;
                }
                else
                {
                    var parameter = serializer.Deserializer(buffer, type);
                    methodParameters.Add(parameter);
                    typeParameter = true;
                }
            }

            return stopSignal;
        }

        public void Start<T>() where T : IPayload
        {
            bool stop = false;
            while (stop == false)
            {
                this.IsRunning = true;
                string address = string.Empty;
                NetMQMessage NetMQMessage = null;

                this.WriteLineToMonitor("Waiting for message");

                byte[] messageAsBytes = null;
                var message = this.ReceiveMessage<T>(
                    this.subscriber, 
                    out NetMQMessage, 
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
                this.MonitorChannel.Send(line);
                var signal = this.MonitorChannel.Receive();
            }
        }
        
        protected void SetUpMonitorChannel(NetMQContext context)
        {
            this.MonitorChannel = context.CreateRequestSocket();
            this.MonitorChannel.Connect(Pipe.MonitorAddressClient);
        }

        protected void SetUpOutputChannel(NetMQContext context)
        {
            this.OutputChannel = context.CreatePublisherSocket();
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

        protected void SetUpReceivers(NetMQContext context, string route)
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
            NetMQSocket subscriber, 
            out NetMQMessage netMqMessage, 
            out string address, 
            out bool stopSignal, 
            out byte[] messageAsBytes, 
            ISerializer serializer)
        {
            stopSignal = false;
            T result = default(T);
            var zmqOut = new NetMQMessage();
            bool hasMore = true;
            address = string.Empty;
            messageAsBytes = null;
            int i = 0;

            var buffer = subscriber.Receive(out hasMore);

            while (hasMore)
            {
                
                if (i == 0)
                {
                    address = serializer.GetString(buffer);
                }

                if (i == 1)
                {
                    messageAsBytes = buffer;
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
                zmqOut.Append(new NetMQFrame(buffer));
                buffer = subscriber.Receive(out hasMore);
            }

            netMqMessage = zmqOut;
            return result;
        }

        /// <summary>
        ///     Creates a Socket and connects it to a endpoint that is bound to a Pipe
        /// </summary>
        /// <param name="context">The ZeroMQ context required to create the receivers</param>
        private void SetUpReceivers(NetMQContext context)
        {
            this.subscriber = context.CreateSubscriberSocket();
            this.subscriber.Connect(Pipe.SubscribeAddressClient);
            
            if (string.IsNullOrEmpty(this.InRoute))
            {
                this.subscriber.Subscribe(string.Empty);
            }
            else
            {
                this.subscriber.Subscribe(this.Serializer.GetBuffer(this.InRoute));
            }

            this.MonitorChannel.Send(
                "Set up Receive channel on " + Pipe.SubscribeAddressClient + " listening on: " + this.InRoute, 
                Pipe.ControlChannelEncoding);
            bool more = false;
            var signal = this.MonitorChannel.Receive(); // Pipe.ControlChannelEncoding, out more);
            this.SendMessage(
                Pipe.ControlChannelEncoding.GetBytes(Pipe.SubscriberCountAddress), 
                Pipe.ControlChannelEncoding.GetBytes("ADDSUBSCRIBER"), 
                this.OutputChannel);
        }
    }
}