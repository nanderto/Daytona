// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Actor.cs" company="Brookfield Global Relocation Services">
// Copyright © 2014 All Rights Reserved
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
    public class Actor<T> : Actor where T : class
    {
        public T Model;

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
            this.Model = model;
            var inRoute = typeof(T).FullName;
            this.SetUpReceivers(context, inRoute);
        }

        /// <summary>
        /// this Constructor is specifically used by the silo to create new instances of user defined objects
        /// </summary>
        /// <param name="context"></param>
        /// <param name="model"></param>
        /// <param name="id"></param>
        /// <param name="messageSerializer"></param>
        /// <param name="persistenceSerializer"></param>
        public Actor(NetMQContext context, T model, string address, ISerializer messageSerializer, ISerializer persistenceSerializer)
            : base(context, messageSerializer, persistenceSerializer)
        {
            this.Model = model;
            //var inRoute = typeof(T).FullName + id;
            this.SetUpReceivers(context, address);
        }

        public Actor(ISerializer serializer)
            : base(serializer)
        {
        }

        public Actor()
        {
            // TODO: Complete member initialization
        }

        public override event EventHandler<CallBackEventArgs> SaveCompletedEvent;



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
                numberOfParameters = int.Parse(serializer.GetString(buffer).Replace("ParameterCount:", string.Empty));
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
       
        public virtual void PersistSelf(Type typeToBePersisted, object toBePersisted, ISerializer serializer)
        {
            
            if (serializer == null)
            {
                serializer = new DefaultSerializer(Pipe.ControlChannelEncoding);
            }

            //var store = new Store(serializer);
            //store.Persist(typeToBePersisted, toBePersisted);
            var pathSegment = this.InRoute;

            this.WriteLineToSelf(serializer.GetString(serializer.GetBuffer(toBePersisted)), pathSegment);
        }

        // public Action<Type, ISerializer> PersistSelf = (toBePersisted, serializer) =>
        // {
        // var pathSegment = typeof(T).FullName;

        // WriteLineToSelf(serializer.GetString(serializer.GetBuffer(toBePersisted)), pathSegment);
        // };
        public virtual bool ReceiveMessage(NetMQSocket subscriber)
        {
            var stopSignal = false;
            var methodParameters = new List<object>();
            var serializer = new BinarySerializer();
            MethodInfo returnedMethodInfo = null;
            var returnedMessageType = string.Empty;
            var returnedAddress = string.Empty;
            var returnAddress = string.Empty;

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

                if (this.Model == null)
                {
                    this.Model = this.ReadfromPersistence(returnedAddress);
                }
                //var target = (T)Activator.CreateInstance(typeof(T));
                var result = returnedMethodInfo.Invoke(this.Model, methodParameters.ToArray());
                
                this.PersistSelf(this.Model.GetType(), this.Model, this.PersistanceSerializer);
            }

            ////Should not get to here, this should get called only by Actors<genericobjects>
            //// they should respond only to messages directed at there address eg generic.object/23
            //// and they will be sending a method info to invoke. These types of actors do not have workloads to invoke 
            if (returnedMessageType == "Workload")
            {
                var inputParameters = new object[4];
                inputParameters[0] = returnedAddress;
                inputParameters[1] = returnedMethodInfo;
                inputParameters[2] = methodParameters;
                inputParameters[3] = this;

                this.Workload.DynamicInvoke(inputParameters);
            }

            // zmqOut.Append(new NetMQFrame(buffer));
            if (returnedMessageType.ToLower() == "stop")
            {
                stopSignal = true;
            }

            return stopSignal;
        }

       public T ReadfromPersistence(string returnedAddress)
        {
            //string line = string.Empty;
            var line = File.ReadLines(string.Format(@"c:\Dev\Persistence\{0}.log", returnedAddress)).LastOrDefault();
            //using (var sr = new StreamReader(string.Format(@"c:\Dev\Persistence\{0}.log", returnedAddress)))
            //{
            //    line = sr.ReadLine();
            //}
            if (line != null)
            {
                var returnedRecord = line.Split('~');

                var target = this.PersistanceSerializer.Deserializer<T>(returnedRecord[0]);
                return target; 
            }

           return null;
        }

        public Actor RegisterActor(string name, string inRoute, ISerializer serializer, Action<string, List<object>, Actor> workload)
        {
            this.actorTypes.Add(name, () =>
                {
                    using (var actor = new Actor(this.Context, serializer, name, inRoute, workload))
                    {
                        actor.Start();
                    }
                });
            return this;
        }

        // public override bool ReceiveMessage(NetMQSocket subscriber)
        // {
        // var stopSignal = false;
        // var zmqOut = new NetMQMessage();
        // bool hasMore = true;

        // // var address = string.Empty;
        // // byte[] messageAsBytes = null;
        // int frameCount = 0;
        // MethodInfo methodinfo = null;
        // var methodParameters = new List<object>();
        // var serializer = new BinarySerializer();
        // var typeParameter = true;
        // Type type = null;
        // MethodInfo returnedMethodInfo = null;
        // string messageType, returnedMessageType = string.Empty;
        // string address, returnedAddress = string.Empty;

        // var buffer = subscriber.Receive(out hasMore);

        // while (hasMore)
        // {
        // stopSignal = UnPackNetMQFrame(frameCount, serializer, buffer, out address, ref methodinfo, methodParameters, ref typeParameter, ref type, out messageType);
        // if (frameCount == 0)
        // {
        // returnedAddress = address;
        // } 

        // if (frameCount == 1)
        // {
        // returnedMessageType = messageType;
        // }

        // if (frameCount == 2)
        // {
        // returnedMethodInfo = methodinfo;
        // }

        // frameCount++;
        // zmqOut.Append(new NetMQFrame(buffer));
        // buffer = subscriber.Receive(out hasMore);
        // }

        // //if (returnedMessageType.ToLower() == "raw")
        // //{
        // //    var inputParameters = new object[4];
        // //    inputParameters[0] = returnedAddress;
        // //    inputParameters[1] = methodParameters;
        // //    inputParameters[3] = this;
        // //    this.Workload.DynamicInvoke(inputParameters);
        // //}
        // //else
        // //{
        // var target = (T)Activator.CreateInstance(typeof(T));
        // var result = returnedMethodInfo.Invoke(target, methodParameters.ToArray());
        // //}
        // return stopSignal;
        // }


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
                try
                {
                    stop = this.ReceiveMessage(this.Subscriber);
                }
                catch (TerminatingException te)
                {
                    ////Swallow excptions caused by the socet closing.
                    //// dont yet have a way to terminate gracefully
                    this.AddFault(te);
                    break;
                }
                
                if (stop)
                {
                    this.IsRunning = false;
                }

                //this.WriteLineToMonitor("Received message");              
            }

            //this.WriteLineToMonitor("Exiting actor");
        }

        public void StartWithIdAndMethod(string address, MethodInfo methodInfo, List<object> parameters)
        {
            var target = (T)Activator.CreateInstance(typeof(T));
            var result = methodInfo.Invoke(target, parameters.ToArray());
            this.Start();
        }

        public void StartWithIdandMessage(string address, NetMQMessage zmqMessage)
        {
            throw new NotImplementedException();
        }
    }
}