namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using NProxy.Core;
    
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

            var realmethodInfo = this.actorType.GetMethod(methodInfo.Name);

            this.actor.SendMessage(parameters, realmethodInfo, address.ToString());
            return null;
        }
    }
}
