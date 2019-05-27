using Daytona.baseclasses;
namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    using NetMQ;
    using NetMQ.zmq;

    using NProxy.Core;

    [Serializable]
    public class Actor : IDisposable
    {
        #region Static Fields

        public static Func<NetMQSocket, BinarySerializer, List<object>, bool> AddParameter =
            (socket, serializer, parameters) =>
                {
                    Type returnedType = GetObjectType(socket, serializer);
                    object parameter = null;
                    var result = GetParameter(socket, serializer, returnedType);
                    parameters.Add(result.Item1);
                    return result.Item2;
                };

        public static Func<NetMQSocket, BinarySerializer, Tuple<MethodInfo, bool>> GetMethodInfo = (socket, serializer) =>
            {
                var hasMore = false;
                var buffer = socket.Receive(out hasMore);
                var methodinfo = (MethodInfo)serializer.Deserializer(buffer, typeof(MethodInfo));
                return new Tuple<MethodInfo, bool>(methodinfo, hasMore);
            };

        public static Func<NetMQSocket, BinarySerializer, Tuple<ExceptionDetails, bool>> GetExceptionDetails = (socket, serializer) =>
        {
            var hasMore = false;
            var buffer = socket.Receive(out hasMore);
            var methodinfo = (ExceptionDetails)serializer.Deserializer(buffer, typeof(ExceptionDetails));
            return new Tuple<ExceptionDetails, bool>(methodinfo, hasMore);
        };

        public static Func<NetMQSocket, BinarySerializer, Tuple<Exception, bool>> GetException = (socket, serializer) =>
        {
            var hasMore = false;
            var buffer = socket.Receive(out hasMore);
            var exception = (Exception)serializer.Deserializer(buffer, typeof(Exception));
            return new Tuple<Exception, bool>(exception, hasMore);
        };

        public static Func<NetMQSocket, BinarySerializer, Type> GetObjectType = (socket, serializer) =>
            {
                var hasMore = false;
                var buffer = socket.Receive(out hasMore);
                return (Type)serializer.Deserializer(buffer, typeof(Type));
            };

        public static Func<NetMQSocket, BinarySerializer, Type, Tuple<object, bool>> GetParameter =
            (socket, serializer, type) =>
                {
                    var hasMore = false;
                    var buffer = socket.Receive(out hasMore);
                    var parameter = serializer.Deserializer(buffer, type);
                    return new Tuple<object, bool>(parameter, hasMore);
                };

        public static Func<NetMQSocket, BinarySerializer, string> GetString = (socket, serializer) =>
            {
                var hasMore = false;
                var buffer = socket.Receive(out hasMore);
                return serializer.GetString(buffer);
            };

        private static readonly object SynchLock = new object();

        #endregion

        #region Fields

        public readonly Dictionary<string, Entity> Entities = new Dictionary<string, Entity>();

        public readonly Dictionary<string, Action> actorTypes = new Dictionary<string, Action>();

        [NonSerialized]
        public NetMQContext Context;

        [NonSerialized]
        public NetMQSocket Subscriber;

        [NonSerialized]
        private NetMQSocket monitorChannel;

        private bool monitorChannelDisposed;

        [NonSerialized]
        private NetMQSocket outputChannel;

        [NonSerialized]
        private ISerializer serializer;

        private bool subscriberDisposed;

        #endregion

        #region Constructors and Destructors

        public Actor()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Actor{T}" /> class.
        ///     This is generally used when creating a actor to act as a Actor factory.
        /// </summary>
        /// <param name="context">The context.</param>
        public Actor(NetMQContext context)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = new DefaultSerializer(Encoding.Unicode);
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.PropertyBag = new Dictionary<string, object>();
            if (this.Subscriber != null)
            {
                ////cant add event handler without the subscriber existing
                ////not sure this is the correct pay to fix it but should do least halm
                this.Subscriber.ReceiveReady += Subscriber_ReceiveReady;
            }
        }

        public void Subscriber_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            Console.WriteLine("hey I fired");
        }

        public Actor(ISerializer serializer)
        {
            this.IsRunning = false;
            this.Serializer = serializer;
        }

        public Actor(NetMQContext context, ISerializer serializer)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = serializer;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
        }

        /// <summary>
        /// Creates an actor that does not listen for messages, it is used to send messages only.
        /// this is usefull in the root when sending messages.
        /// this is the most recent version, where the Serializerfactory is being used to create new serializers all of the time.
        /// </summary>
        /// <param name="context">NetMQContext needed for all messaging</param>
        /// <param name="messageSerializerFactory">factory can be used to produce new sertializers to ensure they are not shared across threads
        /// Just call GetNewSerializer everytime you need a serializer</param>
        public Actor(NetMQContext context, MessageSerializerFactory messageSerializerFactory)
        {
            this.IsRunning = false;
            this.Context = context;
            this.MessageSerializerFactory = messageSerializerFactory;
            this.Serializer = messageSerializerFactory.GetNewSerializer();
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
        }

        public MessageSerializerFactory MessageSerializerFactory { get; set; }

        public Actor(NetMQContext context, ISerializer serializer, string inRoute)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = serializer;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, ISerializer serializer, string name, string inRoute, Action<Actor> workload)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = serializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, ISerializer serializer, string name, string inRoute, Action<object, Sender, Actor> workload)
        {
            this.IsRunning = false;
            this.Context = context;
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
            this.Context = context;
            this.Serializer = serializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, ISerializer messageSerializer, ISerializer persistenceSerializer)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = messageSerializer;
            this.PersistanceSerializer = persistenceSerializer;
           // this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
        }

        public Actor(
            NetMQContext context,
            ISerializer serializer,
            string name,
            string inRoute,
            Dictionary<string, Entity> entities,
            Action<string, string, MethodInfo, List<object>, Actor> workload)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = serializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.Entities = entities;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(
            NetMQContext context,
            ISerializer serializer,
            ISerializer persistenceSerializer,
            string name,
            string inRoute,
            Action<string, MethodInfo, List<object>, Actor> workload)
        {
            this.IsRunning = false;
            this.Context = context;
            this.Serializer = serializer;
            this.PersistanceSerializer = persistenceSerializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        /// <summary>
        /// This constructor is useful for seting up a Actor factory that does not listen for its own messages
        /// </summary>
        /// <param name="context"></param>
        /// <param name="binarySerializer"></param>
        /// <param name="dontAcceptMessages"></param>
        public Actor(NetMQContext context, BinarySerializer binarySerializer, bool dontAcceptMessages)
        {
            this.Context = context;
            this.binarySerializer = binarySerializer;
            this.DontAcceptMessages = dontAcceptMessages;
        }

        public Actor(
            NetMQContext context,
            ISerializer serializer,
            ISerializer persistenceSerializer,
            string name,
            string inRoute,
            Dictionary<string, Entity> entities,
            Action<string, string, MethodInfo, List<object>, Actor> workload)
        {
            this.Context = context;
            this.Serializer = serializer;
            this.PersistanceSerializer = persistenceSerializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Entities = entities;
            this.Workload = workload;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(
            NetMQContext context,
            ISerializer serializer,
            ISerializer persistenceSerializer,
            string name,
            string inRoute,
            Dictionary<string, Entity> entities,
            Dictionary<string, Delegate> actions)
        {
            this.Context = context;
            this.Serializer = serializer;
            this.PersistanceSerializer = persistenceSerializer;
            this.Name = name;
            this.InRoute = inRoute;
            this.Entities = entities;
            this.Actions = actions;
            this.PropertyBag = new Dictionary<string, object>();
            this.SetUpMonitorChannel(context);
            this.SetUpOutputChannel(context);
            this.SetUpReceivers(context, inRoute);
        }
        #endregion

        #region Public Events

        //private NetMQContext context;

        private BinarySerializer binarySerializer;

        private Dictionary<string, Entity> entities;

        private NetMQContext netMQContext;
        
        private ISerializer persistenceSerializer;

        private Action<string, string, MethodInfo, List<object>, Actor> workload;

        public virtual event EventHandler<CallBackEventArgs> SaveCompletedEvent;

        #endregion

        #region Public Properties

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

        #endregion


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

        /// <summary>
        /// Receives the message, and translates it into objects that are called to do the work of the method
        /// </summary>
        /// <param name="subscriber">The socket that the messages are being received on</param>
        /// <returns>Stop signal, if true it has received a message to stop processing and exit</returns>
        public virtual bool  ReceiveMessage(NetMQSocket subscriber)
        {
            var stopSignal = false;
            var methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            MethodInfo returnedMethodInfo = null;
            var returnedMessageType = String.Empty;
            var returnedAddress = String.Empty;
            var returnAddress = String.Empty; //need to send the return address in message package

            returnedAddress = GetString(subscriber, serializer);
            returnedMessageType = GetString(subscriber, serializer);

            if (returnedMessageType == "MethodInfo")
            {
                var returned = GetMethodInfo(subscriber, serializer);
                returnedMethodInfo = returned.Item1;
                var hasMore = returned.Item2;
                while (hasMore)
                {
                    hasMore = AddParameter(subscriber, serializer, methodParameters);
                }

                var inputParameters = new object[5];
                inputParameters[0] = returnedAddress;
                inputParameters[1] = returnAddress;
                inputParameters[2] = returnedMethodInfo;
                inputParameters[3] = methodParameters;
                inputParameters[4] = this;

                Delegate action = null;
                this.Actions.TryGetValue("MethodInfo", out action);

                try
                {
                    action.DynamicInvoke(inputParameters);
                }
                catch (Exception ex)
                {
                    this.SendException(ex, returnedAddress);
                }
                

            }

            if (returnedMessageType == "Spawned")
            {
                var hasMore = true;
                while (hasMore)
                {
                    hasMore = AddParameter(subscriber, serializer, methodParameters);
                }

                var inputParameters = new object[3];
                inputParameters[0] = methodParameters[0];
                inputParameters[1] = new Sender(returnedAddress);
                inputParameters[2] = this;

                this.Workload.DynamicInvoke(inputParameters);
            }

            if (returnedMessageType == "Workload")
            {
                var inputParameters = new object[4];
                inputParameters[0] = returnedAddress;
                inputParameters[1] = returnedMethodInfo;
                inputParameters[2] = methodParameters;
                inputParameters[3] = this;

                this.Workload.DynamicInvoke(inputParameters);
            }

            if (returnedMessageType.ToLower() == "stop")
            {
                stopSignal = true;
            }

            if (returnedMessageType.ToLower() == "shutdownallactors")
             {
                var inputParameters = new object[2];
                inputParameters[0] = "shutdownallactors";
                inputParameters[1] = this;
                Delegate action = null;
                this.Actions.TryGetValue("ShutDownAllActors", out action);
                action.DynamicInvoke(inputParameters);
                //this.Workload.DynamicInvoke(inputParameters);
            }

            if (returnedMessageType.ToLower() == "handleexceptions")
            {
                var returned = GetException(subscriber, serializer);
                var returnedException = returned.Item1;
                string AddressThatThrewException = String.Empty;
                if (returned.Item2)
                {
                    var result = GetExceptionDetails(subscriber, serializer);
                    AddressThatThrewException = result.Item1.AddressThatThrewException;
                }

                Delegate action = null;
                if (this.Actions.TryGetValue("HandleExceptions", out action))
                {

                    var inputParameters = new object[5];
                    inputParameters[0] = "HandleExceptions";
                    inputParameters[1] = this;
                    inputParameters[2] = returnedException;
                    inputParameters[3] = "this is a message";
                    inputParameters[4] = AddressThatThrewException;

                    action.DynamicInvoke(inputParameters);
                    ////I have to do something to handle exceptions like restart the actor.
                    System.Diagnostics.Debug.Assert(true);
                }
            }

            return stopSignal;
        }

        internal void SendException(Exception ex, string address)
        {
            ExceptionDetails exceptionDetails = new ExceptionDetails();
            exceptionDetails.AddressThatThrewException = address;
            var netMqMessage = new NetMQMessage();
            var exceptionSerializer = new BinarySerializer();
            netMqMessage.Append(new NetMQFrame(exceptionSerializer.GetBuffer("ExceptionHandler")));
            netMqMessage.Append(new NetMQFrame(exceptionSerializer.GetBuffer("handleexceptions")));
            netMqMessage.Append(new NetMQFrame(exceptionSerializer.GetBuffer(ex)));
            netMqMessage.Append(new NetMQFrame(exceptionSerializer.GetBuffer(exceptionDetails)));
            this.OutputChannel.SendMessage(netMqMessage);
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
                        using (var actor = new Actor(this.Context, serializer, name, inRoute, workload))
                        {
                            actor.Start<T>();
                        }
                    });
            return this;
        }

        public Actor RegisterActor(
            string name,
            string inRoute,
            string outRoute,
            ISerializer serializer,
            Action<string, List<object>, Actor> workload)
        {
            this.actorTypes.Add(
                name,
                () =>
                    {
                        using (var actor = new Actor(this.Context, serializer, name, inRoute, workload))
                        {
                            actor.Start();
                        }
                    });
            return this;
        }

        /// <summary>
        ///     This constructor is used to handle messages that have been packaged up with a methodinfo object and parameters
        ///     These messages are sent by the proxy of the custom objects.
        /// </summary>
        /// <param name="name">Name of actor</param>
        /// <param name="inRoute">
        ///     the address the actor listens to. this will generally be blank so that this methid can handle all me
        ///     messages that are sent
        /// </param>
        /// <param name="outRoute">Generally unused</param>
        /// <param name="serializer">Serializer</param>
        /// <param name="workload">
        ///     the function to execute, this function will typically execute the methodinfo that it is sent
        ///     with the parameter sent
        /// </param>
        /// <returns>the actor</returns>
        public Actor RegisterActor(
            string name,
            string inRoute,
            string outRoute,
            ISerializer serializer,
            ISerializer persistenceSerializer,
            Action<string, MethodInfo, List<object>, Actor> workload)
        {
            this.actorTypes.Add(
                name,
                () =>
                    {
                        using (var actor = new Actor(this.Context, serializer, persistenceSerializer, name, inRoute, workload))
                        {
                            actor.Start();
                        }
                    });
            return this;
        }

        public Actor RegisterActor(
            string name,
            string inRoute,
            string outRoute,
            Dictionary<string, Entity> entities,
            ISerializer serializer,
            Action<string, string, MethodInfo, List<object>, Actor> workload)
        {
            this.actorTypes.Add(
                name,
                () =>
                    {
                        using (var actor = new Actor(this.Context, serializer, name, inRoute, entities, workload))
                        {
                            actor.Start();
                        }
                    });
            return this;
        }

        public Actor RegisterActor(
            string name,
            string inRoute,
            string outRoute,
            Dictionary<string, Entity> entities,
            ISerializer serializer,
            ISerializer persistenceSerializer,
            Action<string, string, MethodInfo, List<object>, Actor> workload)
        {
            this.actorTypes.Add(
                name,
                () =>
                    {
                        using (
                            var actor = new Actor(
                                this.Context,
                                serializer,
                                persistenceSerializer,
                                name,
                                inRoute,
                                entities,
                                workload))
                        {
                            actor.Start();
                        }
                    });
            return this;
        }

        public Actor RegisterActor(
            string name,
            string inRoute,
            string outRoute,
            Dictionary<string, Entity> entities,
            ISerializer serializer,
            ISerializer persistenceSerializer,
            Dictionary<string, Delegate> actions)
        {
            this.actorTypes.Add(
                name,
                () =>
                {
                    using (var actor = new Actor(
                            this.Context,
                            serializer,
                            persistenceSerializer,
                            name,
                            inRoute,
                            entities,
                            actions))
                    {
                        actor.Start();
                    }
                });
            return this;
        }

        public void SendKillMe(ISerializer serializer, NetMQSocket socket)
        {
            this.SendKillSignal(serializer, socket, this.InRoute);
        }

        public void SendKillSignal(ISerializer serializer, NetMQSocket socket, string address)
        {
            var netMqMessage = new NetMQMessage();
            netMqMessage.Append(new NetMQFrame(serializer.GetBuffer(address)));
            netMqMessage.Append(new NetMQFrame(serializer.GetBuffer("stop")));
            socket.SendMessage(netMqMessage);
        }

        public void SendMessage(string address, byte[] message, ISerializer serializer, NetMQSocket socket)
        {
            this.SendMessage(serializer.Encoding.GetBytes(address), message, socket);
        }

        public void SendMessage(string address, object message, ISerializer serializer, NetMQSocket socket)
        {
            var netMQMessage = new NetMQMessage();
            netMQMessage.Append(new NetMQFrame(serializer.GetBuffer(address)));
            netMQMessage.Append(new NetMQFrame(serializer.GetBuffer("Spawned")));
            netMQMessage.Append(new NetMQFrame(serializer.GetBuffer(message.GetType())));
            netMQMessage.Append(new NetMQFrame(serializer.GetBuffer(message)));
            socket.SendMessage(netMQMessage);
        }

        public void SendMessage(byte[] address, byte[] message, NetMQSocket socket)
        {
            var netMQMessage = new NetMQMessage();
            netMQMessage.Append(new NetMQFrame(address));
            netMQMessage.Append(new NetMQFrame(message));
            socket.SendMessage(netMQMessage);
        }

        public void SendMessage(object[] parameters, MethodInfo methodInfo, string TypeFullName)
        {
            var zmqMessage = PackZmqMessage(parameters, methodInfo, this.Serializer, TypeFullName);

            this.OutputChannel.SendMessage(zmqMessage);
        }

        public static NetMQMessage PackZmqMessage(object[] parameters, MethodInfo methodInfo, ISerializer serializer, string addressToSendTo)
        {
            var zmqMessage = new NetMQMessage();
            zmqMessage.Append(new NetMQFrame(serializer.GetBuffer(addressToSendTo)));
            zmqMessage.Append(new NetMQFrame(serializer.GetBuffer("MethodInfo")));

            var serializedMethodInfo = serializer.GetBuffer(methodInfo);
            zmqMessage.Append(new NetMQFrame(serializedMethodInfo));

            // zmqMessage.Append(new NetMQFrame(serializer.GetBuffer(string.Format("ParameterCount:{0}", parameters.Length))));
            foreach (var parameter in parameters)
            {
                //todo cant handle null parameters
                zmqMessage.Append(serializer.GetBuffer(parameter.GetType()));
                zmqMessage.Append(serializer.GetBuffer(parameter));
            }

            return zmqMessage;
        }

        public TInterface CreateInstance<TInterface>(Type actoryType) where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy(this, actoryType);
            var proxyFactory = new ProxyFactory(new MethodsOnlyInterceptionFilter());
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        }

        //public TInterface CreateInstance<TInterface>() where TInterface : class
        //{
        //    var invocationHandler = new MessageSenderProxy(this, actoryType);
        //    var proxyFactory = new ProxyFactory(new MethodsOnlyInterceptionFilter());
        //    return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        //}

        public TInterface CreateInstance<TInterface>(Type actoryType, long id) where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy(this, actoryType, id);
            var proxyFactory = new ProxyFactory(new MethodsOnlyInterceptionFilter());
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        }

        public TInterface CreateInstance<TInterface>(Type actoryType, Guid uniqueGuid) where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy(this, actoryType, uniqueGuid);
            var proxyFactory = new ProxyFactory(new MethodsOnlyInterceptionFilter());
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
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
                string address = String.Empty;
                //NetMQMessage NetMQMessage = null;

                this.WriteLineToMonitor($"The Actor \"{this.Name}\" is waiting for message");
                   
                //byte[] messageAsBytes = null;
                try
                {
                    stop = this.ReceiveMessage(this.Subscriber);
                }
                catch (SerializationException se)
                {
                    this.WriteLineToMonitor(String.Format("Serialization Error: {0}", se));

                    ////skip to end of message
                    bool more;
                    do
                    {
                        var data = this.Subscriber.Receive(out more);
                    }
                    while (more);
                }
                catch (TerminatingException te)
                {
                    ////Swallow excptions caused by the socet closing.
                    //// dont yet have a way to terminate gracefully

                    break;
                }

                ////non generic Actors do (should) not have any data to persist.
                ////this.PersistSelf(this.TypeOfActor, this, this.PersistanceSerializer);

                if (stop)
                {
                    this.IsRunning = false;
                }
            }

            this.WriteLineToMonitor(string.Format( "Exiting actor {0}", this.Name));
        }

        public void Start<T>() where T : IPayload
        {
            bool stop = false;
            while (stop == false)
            {
                this.IsRunning = true;
                string address = String.Empty;
                NetMQMessage NetMQMessage = null;

                this.WriteLineToMonitor("Waiting for message");

                byte[] messageAsBytes = null;
                var message = this.ReceiveMessage<T>(
                    this.Subscriber,
                    out NetMQMessage,
                    out address,
                    out stop,
                    out messageAsBytes,
                    this.Serializer);
                if (stop)
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
#if DEBUG
            if (this.monitorChannelDisposed == false)
            {
                try
                {
                    this.MonitorChannel.Send(line, Exchange.ControlChannelEncoding);
                    var signal = this.MonitorChannel.Receive();
                    //var signal = this.MonitorChannel.Receive(SendReceiveOptions.DontWait);
                }
                catch (Exception ex)
                {
                    this.AddFault(ex);
                }
            }
#endif
        }

        private readonly List<Exception> faults = new List<Exception>();


        public void AddFault(Exception ex)
        {
            this.faults.Add(ex);
        }

        public void WriteLineToSelf(string line, string PathSegment)
        {
            var PathSegmentWithoutID = PathSegment;
            if (PathSegment.Contains("/"))
            {
                PathSegmentWithoutID = PathSegment.Split('/')[0];
            }

            var fi = new FileInfo(String.Format(@"c:\dev\persistence\{0}.log", PathSegmentWithoutID));

            var stream = fi.AppendText();
            stream.WriteLine("{0}~{1}", line, DateTime.Now.Ticks);
            stream.Flush();
            stream.Close();
        }
        
        public bool DontAcceptMessages { get; set; }

        public ISerializer PersistanceSerializer { get; set; }

        public ISerializerFactory PersistSerializerFactory { get; set; }


        public object ReadfromPersistence(string returnedAddress, Type type)
        {
            var fileName = @"c:\Dev\Persistence\" + returnedAddress + ".log";
            var directoryInfo = new DirectoryInfo(returnedAddress);
            var fileInfo = new FileInfo(returnedAddress);
            object target = null;

            if (File.Exists(fileName))
            {
                var line = File.ReadLines(fileName).LastOrDefault();

                if (line != null)
                {
                    var returnedRecord = line.Split('~');
                    target = this.PersistanceSerializer.Deserializer(
                        Exchange.ControlChannelEncoding.GetBytes(returnedRecord[0]), type); 
                }
                else
                {
                    target = Activator.CreateInstance(type);
                    var store = new Store(this.PersistanceSerializer);
                    store.Persist(type, target, returnedAddress);
                }
            }
            else
            {
                //File.Create(fileName);
                if (returnedAddress.Contains(@"/"))
                {
                    var addressAndNumber = returnedAddress.Split('/');
                    var id = addressAndNumber[1];
                    long longId = 0;
                    if (Int64.TryParse(id, out longId))
                    {
                        target = Activator.CreateInstance(type, longId);
                    }
                    else
                    {
                        var guidId = new Guid(id);
                        target = Activator.CreateInstance(type, guidId);
                    }

                    var store = new Store(this.PersistanceSerializer);
                    store.Persist(type, target, returnedAddress); 
                }

            }
    
            return target;
        }


        protected void SetUpMonitorChannel(NetMQContext context)
        {
#if DEBUG
            this.MonitorChannel = context.CreateRequestSocket();
            this.MonitorChannel.Connect(Exchange.MonitorAddressClient);
#endif
        }

        protected void SetUpOutputChannel(NetMQContext context)
        {
            this.OutputChannel = context.CreatePublisherSocket();
            this.OutputChannel.Connect(Exchange.PublishAddress);

            this.WriteLineToMonitor(
                "Set up output channel on " + Exchange.PublishAddress + " Default sending on: " + this.OutRoute);

            ////if(this.sendControlChannel == null)
            ////{
            ////    this.sendControlChannel = context.CreateSocket(SocketType.REQ);
            ////    this.sendControlChannel.Connect(Pipe.PubSubControlBackAddressClient);
            ////}
            ////this.sendControlChannel.Send("Actor OutputChannel connected, Sending on " + Exchange.PublishAddressClient, Pipe.ControlChannelEncoding);
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
            if (disposing)
            {
                if (this.OutputChannel != null)
                {
                    if (this.IsRunning)
                    {
                       // I dont think this works
                       // this.SendKillMe(this.Serializer, this.outputChannel);
                    }

                    this.OutputChannelDisposed = true;
                    this.OutputChannel.Dispose();
                }

                if (this.Subscriber != null)
                {
                    this.subscriberDisposed = true;
                    this.Subscriber.Dispose();
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
            address = String.Empty;
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
                            Exchange.ControlChannelEncoding.GetBytes(Exchange.SubscriberCountAddress),
                            Exchange.ControlChannelEncoding.GetBytes("SHUTTINGDOWN"),
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
            this.Subscriber = context.CreateSubscriberSocket();
            this.Subscriber.Connect(Exchange.SubscribeAddress);

            if (String.IsNullOrEmpty(this.InRoute))
            {
                this.Subscriber.Subscribe(String.Empty);
            }
            else
            {
                this.Subscriber.Subscribe(this.Serializer.GetBuffer(this.InRoute));
            }

            this.MonitorChannel.Send(
                "Set up Receive channel on " + Exchange.SubscribeAddress + " listening on: " + this.InRoute,
                Exchange.ControlChannelEncoding);
            bool more = false;
            var signal = this.MonitorChannel.Receive(); // Pipe.ControlChannelEncoding, out more);
            this.SendMessage(
                Exchange.ControlChannelEncoding.GetBytes(Exchange.SubscriberCountAddress),
                Exchange.ControlChannelEncoding.GetBytes("ADDSUBSCRIBER"),
                this.OutputChannel);
        }

        public Dictionary<string, Delegate> Actions { get; set; }
    }
}