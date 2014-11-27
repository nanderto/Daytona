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
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    using NProxy.Core;

    using ZeroMQ;

    /// <summary>
    ///     The Actor is the coe object of the Actor framework, it is self configuring to listen for messages that come in and
    ///     execute what ever
    ///     workload that is configured for it.
    /// </summary>
    /// <typeparam name="T">
    ///     The object to compose with this actor
    /// </typeparam>
    [Serializable]
    public class Actor<T> : Actor where T : class
    {
        private T model;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Actor" /> class.
        ///     This is generally used when creating a actor to act as a Actor factory.
        /// </summary>
        /// <param name="context">The context.</param>
        public Actor(ZmqContext context) : base(context)
        {
        }

        public Actor(ZmqContext context, ISerializer serializer) : base (context, serializer)
        {
            var inRoute = typeof(T)
                .FullName.Replace("{", string.Empty)
                .Replace("}", string.Empty)
                .Replace("_", string.Empty)
                .Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(ZmqContext context, T model) : base (context)
        {
            this.model = model;
            var inRoute =
                typeof(T).Name.Replace("{", string.Empty)
                    .Replace("}", string.Empty)
                    .Replace("_", string.Empty)
                    .Replace(".", string.Empty);
            this.SetUpReceivers(context, inRoute);
        }

        public Actor(ZmqContext context, T model, ISerializer serializer)
            : this(context, model)
        {
            this.Serializer = serializer;
        }

        public Actor(ISerializer serializer) : base(serializer)
        {
        }
        
        public override event EventHandler<CallBackEventArgs> SaveCompletedEvent;
        private BinarySerializer binarySerializer;

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

        public TInterface CreateInstance<TInterface>() where TInterface : class
        {
            var invocationHandler = new MessageSenderProxy<T>(this);
            var proxyFactory = new ProxyFactory();
            return proxyFactory.CreateProxy<TInterface>(Type.EmptyTypes, invocationHandler);
        }

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

        public void SendMessage(object[] parameters, MethodInfo methodInfo)
        {
            var zmqMessage = PackZmqMessage(parameters, methodInfo, this.Serializer);

            this.OutputChannel.SendMessage(zmqMessage);
        }

        public static ZmqMessage PackZmqMessage(object[] parameters, MethodInfo methodInfo, ISerializer serializer)
        {
            var zmqMessage = new ZmqMessage();
            var address = typeof(T).FullName;
            zmqMessage.Append(new Frame(serializer.GetBuffer(address)));
            zmqMessage.Append(new Frame(serializer.GetBuffer("Process")));
            
            var serializedMethodInfo = serializer.GetBuffer(methodInfo);
            zmqMessage.Append(new Frame(serializedMethodInfo));
            zmqMessage.Append(new Frame(serializer.GetBuffer(string.Format("ParameterCount:{0}", parameters.Length))));
            foreach (var parameter in parameters)
            {
                zmqMessage.Append(serializer.GetBuffer(parameter.GetType()));
                zmqMessage.Append(serializer.GetBuffer(parameter));
            }
            return zmqMessage;
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
    }
}