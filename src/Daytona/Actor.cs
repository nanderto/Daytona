namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using NetMQ;

    using NProxy.Core;


    /// <summary>
    ///     The Actor is the coe object of the Actor framework, it is self configuring to listen for messages that come in and
    ///     execute what ever
    ///     workload that is configured for it.
    /// </summary>
    /// <typeparam name="T">
    ///     The object to compose with this actor
    /// </typeparam>
    [Serializable]
    public class Actor<T> : Actor
        where T : class
    {
        private T model;
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="Actor" /> class.
        ///     This is generally used when creating a actor to act as a Actor factory.
        /// </summary>
        /// <param name="context">The context.</param>
        public Actor(NetMQContext context)
            : base(context)
        {
        }

        public Actor(NetMQContext context, ISerializer serializer)
            : base(context, serializer)
        {
            var inRoute = typeof(T).FullName;

            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, ISerializer serializer, string inRoute)
            : base(context, serializer)
        {
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, T model)
            : base(context)
        {
            this.model = model;
            var inRoute = typeof(T).FullName;
            //Name.Replace("{", string.Empty)
            //        .Replace("}", string.Empty)
            //        .Replace("_", string.Empty)
            //        .Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(NetMQContext context, T model, ISerializer serializer)
            : this(context, model)
        {
            this.Serializer = serializer;
        }

        public Actor(ISerializer serializer)
            : base(serializer)
        {
        }
        
        public override event EventHandler<CallBackEventArgs> SaveCompletedEvent;
        
        public static NetMQMessage    PackZmqMessage(object[] parameters, MethodInfo methodInfo, ISerializer serializer, string addressToSendTo)
        {
            var zmqMessage = new NetMQMessage();
            zmqMessage.Append(new NetMQFrame(serializer.GetBuffer(addressToSendTo)));
            zmqMessage.Append(new NetMQFrame(serializer.GetBuffer("MethodInfo")));

            var serializedMethodInfo = serializer.GetBuffer(methodInfo);
            zmqMessage.Append(new NetMQFrame(serializedMethodInfo));
            //zmqMessage.Append(new NetMQFrame(serializer.GetBuffer(string.Format("ParameterCount:{0}", parameters.Length))));
            foreach (var parameter in parameters)
            {
                zmqMessage.Append(serializer.GetBuffer(parameter.GetType()));
                zmqMessage.Append(serializer.GetBuffer(parameter));
            }

            return zmqMessage;
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

        /// <summary>
        ///     Start is called on all actors to have them listen for messages, they will receive and process one message
        ///     at a time
        /// </summary>
        // public void Start()
        // {
        // while (true)
        // {
        // if (this.subscriberDisposed != true)
        // {
        // var zmqmessage = this.subscriber.ReceiveMessage();
        // var frameContents = zmqmessage.Select(f => this.Serializer.Encoding.GetString(f.Buffer)).ToList();

        // if (frameContents.Count > 1)
        // {
        // var message = frameContents[1];

        // if (message != null)
        // {
        // if (string.IsNullOrEmpty(this.OutRoute))
        // {
        // var inputParameters = new object[2];
        // inputParameters[0] = message;
        // inputParameters[1] = this.InRoute;
        // this.Workload.DynamicInvoke(inputParameters);
        // }
        // else
        // {
        // if (this.PropertyBag != null)
        // {
        // var inputParameters = new object[5];
        // inputParameters[0] = message;
        // inputParameters[1] = this.InRoute;
        // inputParameters[2] = this.OutRoute;
        // inputParameters[3] = this.OutputChannel;
        // inputParameters[4] = this;
        // this.Workload.DynamicInvoke(inputParameters);
        // }
        // else
        // {
        // var inputParameters = new object[4];
        // inputParameters[0] = message;
        // inputParameters[1] = this.InRoute;
        // inputParameters[2] = this.OutRoute;
        // inputParameters[3] = this.OutputChannel;
        // this.Workload.DynamicInvoke(inputParameters);
        // }
        // }
        // }
        // }
        // }
        // else
        // {
        // break;
        // }
        // }
        // }
        public TInterface CreateInstance<TInterface>(Type actoryType) where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy<T>(this, actoryType);
            var proxyFactory = new ProxyFactory();
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        }

        public TInterface CreateInstance<TInterface>(Type actoryType, long id) where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy<T>(this, actoryType, id);
            var proxyFactory = new ProxyFactory();
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        }

        public TInterface CreateInstance<TInterface>(Type actoryType, Guid uniqueGuid) where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy<T>(this, actoryType, uniqueGuid);
            var proxyFactory = new ProxyFactory();
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        }

        public virtual bool ReceiveMessage(NetMQSocket subscriber)
        {
            var stopSignal = false;
            var methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            MethodInfo returnedMethodInfo = null;
            var returnedMessageType = string.Empty;
            var returnedAddress = string.Empty;


            returnedAddress = GetString(subscriber, serializer);
            returnedMessageType = GetString(subscriber, serializer);

            if (returnedMessageType == "MethodInfo")
            {
                returnedMethodInfo = GetMethodInfo(subscriber, serializer);
                while (AddParameter(subscriber, serializer, methodParameters)) ;
                
                var target = (T)Activator.CreateInstance(typeof(T));
                var result = returnedMethodInfo.Invoke(target, methodParameters.ToArray());

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

            //zmqOut.Append(new NetMQFrame(buffer));
            if (returnedMessageType.ToLower() == "stop")
            {
                stopSignal = true;
            }

            return stopSignal;
        }

        //public override bool ReceiveMessage(NetMQSocket subscriber)
        //{
        //    var stopSignal = false;
        //    var zmqOut = new NetMQMessage();
        //    bool hasMore = true;

        //    // var address = string.Empty;
        //    // byte[] messageAsBytes = null;
        //    int frameCount = 0;
        //    MethodInfo methodinfo = null;
        //    var methodParameters = new List<object>();
        //    var serializer = new BinarySerializer();
        //    var typeParameter = true;
        //    Type type = null;
        //    MethodInfo returnedMethodInfo = null;
        //    string messageType, returnedMessageType = string.Empty;
        //    string address, returnedAddress = string.Empty;
            
        //    var buffer = subscriber.Receive(out hasMore);

        //    while (hasMore)
        //    {
        //        stopSignal = UnPackNetMQFrame(frameCount, serializer, buffer, out address, ref methodinfo, methodParameters, ref typeParameter, ref type, out messageType);
        //        if (frameCount == 0)
        //        {
        //            returnedAddress = address;
        //        } 
                
        //        if (frameCount == 1)
        //        {
        //            returnedMessageType = messageType;
        //        }

        //        if (frameCount == 2)
        //        {
        //            returnedMethodInfo = methodinfo;
        //        }

        //        frameCount++;
        //        zmqOut.Append(new NetMQFrame(buffer));
        //        buffer = subscriber.Receive(out hasMore);
        //    }

        //    //if (returnedMessageType.ToLower() == "raw")
        //    //{
        //    //    var inputParameters = new object[4];
        //    //    inputParameters[0] = returnedAddress;
        //    //    inputParameters[1] = methodParameters;
        //    //    inputParameters[3] = this;
        //    //    this.Workload.DynamicInvoke(inputParameters);
        //    //}
        //    //else
        //    //{
        //        var target = (T)Activator.CreateInstance(typeof(T));
        //        var result = returnedMethodInfo.Invoke(target, methodParameters.ToArray());
        //    //}
        //    return stopSignal;
        //}

        public void SendMessage(object[] parameters, MethodInfo methodInfo, string TypeFullName)
        {
            var zmqMessage = PackZmqMessage(parameters, methodInfo, this.Serializer, TypeFullName);

            this.OutputChannel.SendMessage(zmqMessage);
        }

        public override void Start()
        {
            bool stop = false;
            while (stop == false)
            {
                this.IsRunning = true;
                string address = string.Empty;
                NetMQMessage zmqmessage = null;

                this.WriteLineToMonitor("Waiting for message");

                byte[] messageAsBytes = null;
                stop = this.ReceiveMessage(this.Subscriber);
                if (stop)
                {
                    this.IsRunning = false;
                }

                this.WriteLineToMonitor("Received message");
            }

            this.WriteLineToMonitor("Exiting actor");
        }

         public void StartWithIdandMessage(string address, NetMQMessage zmqMessage)
        {
            throw new NotImplementedException();
        }

        public Actor RegisterActor(string name, string inRoute, ISerializer serializer, Action<string, List<object>, Actor> workload)
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


        public void StartWithIdAndMethod(string address, MethodInfo methodInfo, List<object> parameters)
        {
            var target = (T)Activator.CreateInstance(typeof(T));
            var result = methodInfo.Invoke(target, parameters.ToArray());
            this.Start();
        }
    }
}