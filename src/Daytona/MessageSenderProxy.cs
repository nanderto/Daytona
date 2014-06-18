namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NProxy.Core;

    public class MessageSenderProxy<T> : IInvocationHandler
    {
        private Actor<T> actor;

        public MessageSenderProxy(Actor<T> actor)
        {
            this.actor = actor;
        }

        public object Invoke(object target, System.Reflection.MethodInfo methodInfo, object[] parameters)
        {
            //this.actor.SendOneMessageOfType<MessagePayload<object[]>>(this.address, new MessagePayload<object[]>(args), this.actor);
            throw new NotImplementedException();
        }
    }
}
