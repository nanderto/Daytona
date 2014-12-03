namespace Daytona.DynamicProxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DynamicProxyImplementation;

    public class MessageSenderDynamicProxy<T> : DynamicProxy
    {
        private Actor<T> actor;

        private string address;

        public MessageSenderDynamicProxy(Actor<T> actor, Type classToSendTo)
        {
            this.address = classToSendTo.Name; 
            this.actor = actor;
        }

        protected override bool TryInvokeMember(Type interfaceType, string name, object[] args, out object result)
        {
            
            this.actor.SendOneMessageOfType<MessagePayload<object[]>>(this.address, new MessagePayload<object[]>(args), this.actor);
            result = new object();
            return true;
        }

        protected override bool TrySetMember(Type interfaceType, string name, object value)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetMember(Type interfaceType, string name, out object result)
        {
            throw new NotImplementedException();
        }

        protected override bool TrySetEvent(Type interfaceType, string name, object value)
        {
            throw new NotImplementedException();
        }

        
    }
}
