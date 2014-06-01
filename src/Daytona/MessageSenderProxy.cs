using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    public class MessageSenderProxy : IProxyInvocationHandler
    {
        private   actor;

        public MessageSenderProxy(Actor actor)
        {
            this.actor = actor;
        }

        public object Invoke(object proxy, System.Reflection.MethodInfo method, object[] parameters)
        {
            return new object();
           // throw new NotImplementedException();
        }
    }
}
