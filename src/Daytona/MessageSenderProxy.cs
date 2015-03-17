// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageSenderProxy.cs" company="Brookfield Global Relocation Services">
// Copyright © 2014 All Rights Reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Daytona
{
    using System;
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
        public bool WasCalled = false;

        private readonly long Id;

        private readonly Actor actor;

        private readonly Type actorType;

        private readonly Guid? uniqueGuid;

        public readonly string UniqueId;

        /// <summary>
        /// MessageSenderProxy constructor, you need to pass in enough that your method will work when called by the interception proxy
        /// In this case you need a reference to the parent object that will be sending the message, and a way to determine where the message is being sent.
        /// Currently using the type of the object that the method is called on 
        /// </summary>
        /// <param name="actor">The actor has a channel set up to send messages</param>
        /// <param name="actorType">The type of object you are sending messages too.</param>
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

        public MessageSenderProxy(Actor actor, Type actorType, string uniqueId)
        {
            this.actor = actor;
            this.actorType = actorType;
            this.UniqueId = uniqueId;
        }

        public object Invoke(object target, System.Reflection.MethodInfo methodInfo, object[] parameters)
        {
            this.WasCalled = true;
            ////this does not work for string Id's yet ...not sure I really want to support them
            var id = this.Id == 0 ? this.uniqueGuid.ToString() : this.Id.ToString();
            var address = new StringBuilder(this.actorType.FullName).Append("/").Append(id);

            ////the methodinfo past in will not deserialize, its a proxy method anyway so getting the actual method makes sence
            var realmethodInfo = this.actorType.GetMethod(methodInfo.Name);

            this.actor.SendMessage(parameters, realmethodInfo, address.ToString());
            return null;
        }
    }
}