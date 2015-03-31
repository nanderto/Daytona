namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;

    using NProxy.Core;
    
    /// <summary>
    /// MessageSenderproxy used to intercept method calls and send them ass messages to the actors that need to respond 
    /// </summary>
    /// <typeparam name="T">Type that this is sent from, allows us to save a strongly typed reference to the actor that is sending the message
    /// </typeparam>
    [Serializable]
    public class MessageSenderProxy : IInvocationHandler 
    {
        private Actor actor;

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
        public MessageSenderProxy(Actor actor, Type actorType)
        {
            this.actorType = actorType;
            this.actor = actor;
        }

        public MessageSenderProxy(Actor actor, Type actorType, long Id)
        {
            this.actor = actor;
            this.actorType = actorType;
            this.Id = Id;
        }

        public MessageSenderProxy(Actor actor, Type actorType, Guid uniqueGuid)
        {
            this.actor = actor;
            this.actorType = actorType;
            this.uniqueGuid = uniqueGuid;
        }

        public object Invoke(object target, MethodInfo methodInfo, object[] parameters)
        {
            this.WasCalled = true;
            var id = this.Id == 0 ? this.uniqueGuid.ToString() : this.Id.ToString();
            string address;

            if (!string.IsNullOrEmpty(id))
            {
                address = new StringBuilder(this.actorType.FullName).Append("/").Append(id).ToString();
            }
            else
            {
                address = this.actorType.FullName;
            }

            MethodInfo realmethodInfo = null;

            if (parameters.Length > 0)
            {
                var parametrs = new Type[parameters.Length];

                for (var i = 0; i < parameters.Length; i++)
                {
                    parametrs[i] = parameters[i].GetType();
                }

                realmethodInfo = this.actorType.GetMethod(methodInfo.Name, parametrs);
            }
            else
            {
                realmethodInfo = this.actorType.GetMethod(methodInfo.Name);
            }

            this.actor.SendMessage(parameters, realmethodInfo, address);
            return null;
        }
    }
}
