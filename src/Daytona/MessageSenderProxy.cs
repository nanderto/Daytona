using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daytona
{
    public class MessageSenderProxy : IProxyInvocationHandler
    {
        public object Invoke(object proxy, System.Reflection.MethodInfo method, object[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}
