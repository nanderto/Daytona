namespace Daytona
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NProxy.Core;

    public class MessageSenderProxy : IInvocationHandler
    {
        public object Invoke(object target, System.Reflection.MethodInfo methodInfo, object[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}
