namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NProxy.Core;

    [Serializable]
    public class MessageSenderProxy<T> : IInvocationHandler where T : class
    {
        private Actor<T> actor;

        public bool WasCalled = false;

        public MessageSenderProxy(Actor<T> actor)
        {
            this.actor = actor;
        }

        public object Invoke(object target, System.Reflection.MethodInfo methodInfo, object[] parameters)
        {
            this.WasCalled = true;
            this.actor.SendMessage(parameters, methodInfo);
            return null;
        }
    }
}
