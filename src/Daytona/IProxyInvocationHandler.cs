namespace Daytona
{
    using System;
    using System.Reflection;

    public interface IProxyInvocationHandler
    {
        Object Invoke(Object proxy, MethodInfo method, Object[] parameters);
    }
}