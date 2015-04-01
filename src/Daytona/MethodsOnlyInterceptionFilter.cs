namespace Daytona
{
    using System;
    using System.Reflection;

    using NProxy.Core;

    public class MethodsOnlyInterceptionFilter : IInterceptionFilter
    {
        /// <summary>
        /// The name of the destructor method.
        /// </summary>
        private const string DestructorMethodName = "Finalize";

        public bool AcceptEvent(System.Reflection.EventInfo eventInfo)
        {
            return false;
        }

        public bool AcceptMethod(MethodInfo methodInfo)
        {
            if (methodInfo.IsDefined(typeof(NonInterceptedAttribute), false))
            {
                return false;
            }

            // Don't intercept the destructor method.
            if (methodInfo.DeclaringType != typeof(object))
            {
                return true;
            }

            return !String.Equals(methodInfo.Name, DestructorMethodName);
        }

        public bool AcceptProperty(System.Reflection.PropertyInfo propertyInfo)
        {
            return false;
        }
    }
}