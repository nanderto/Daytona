namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using NProxy.Core;

    using ZeroMQ;

    /// <summary>
    /// MessageSenderproxy used to intercept method calls and send them ass messages to the actors that need to respond 
    /// </summary>
    /// <typeparam name="T">Type that this is sent from, allows us to save a strongly typed reference to the actor that is sending the message
    /// </typeparam>
    [Serializable]
    public class MessageSenderProxy<T> : IInvocationHandler where T : class
    {
        private Actor<T> actor;

        public bool WasCalled = false;

        private Type actorType;
        private long Id = 0;
        private Guid? uniqueGuid;

        /// <summary>
        /// MessageSenderProxy constructor, you need to pass in enough that your method will work when called by the interception proxy
        /// In this case se need a reference to the parent object that will be sending the message, and a way to determine where the message is being sent.
        /// Currently using the type of the object that the method is called on 
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="actorType"></param>
        public MessageSenderProxy(Actor<T> actor, Type actorType)
        {
            this.actorType = actorType;
            this.actor = actor;
        }

        public MessageSenderProxy(Actor<T> actor, Type actorType, long Id)
        {
            this.actor = actor;
            this.actorType = actorType;
            this.Id = Id;
        }

        public MessageSenderProxy(Actor<T> actor, Type actorType, Guid uniqueGuid)
        {
            this.actor = actor;
            this.actorType = actorType;
            this.uniqueGuid = uniqueGuid;
        }

        public object Invoke(object target, System.Reflection.MethodInfo methodInfo, object[] parameters)
        {
            this.WasCalled = true;
            var id = this.Id == 0 ? this.uniqueGuid.ToString() : this.Id.ToString();
            var address = new StringBuilder(this.actorType.FullName).Append(id);


            //var zmqMessage = Actor<string>.PackZmqMessage(parameters, methodInfo, new BinarySerializer(), address.ToString());


            //var stopSignal = false;
            //var zmqOut = new ZmqMessage();
            //bool hasMore = true;

            //// var address = string.Empty;
            //// byte[] messageAsBytes = null;
            //int frameCount = 0;
            //MethodInfo methodinfo = null;
            //var methodParameters = new List<object>();
            //var serializer = new BinarySerializer();
            //var typeParameter = true;
            //Type type = null;
            //MethodInfo returnedMethodInfo = null;
            //string messageType, returnedMessageType = string.Empty;
            //string address2, returnedAddress = string.Empty;

            //foreach (var frame in zmqMessage)
            //{
            //    stopSignal = Actor.UnPackFrame(frameCount, serializer, frame, out address2, ref methodinfo, methodParameters, ref typeParameter, ref type, out messageType);
            //    if (frameCount == 0)
            //    {
            //        returnedAddress = address2;
            //    }

            //    if (frameCount == 1)
            //    {
            //        returnedMessageType = messageType;
            //    }

            //    if (frameCount == 2)
            //    {
            //        returnedMethodInfo = methodinfo;
            //    }

            //    frameCount++;
            //}






            this.actor.SendMessage(parameters, methodInfo, address.ToString());
            return null;
        }
    }
}
