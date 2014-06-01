namespace Daytona
{
    using System;
    using System.Collections;
    using System.Reflection;

    /// <summary>
    /// Factory class used to cache Types instances
    /// </summary>
    public class MetaDataFactory
    {
        private readonly Hashtable typeMap = new Hashtable();

        ///<summary>
        /// Method to add a new Type to the cache, using the type's fully qualified
        /// name as the key
        ///</summary>
        ///<param name="interfaceType">Type to cache</param>
        public void Add(Type interfaceType)
        {
            if (interfaceType != null)
            {
                if (!typeMap.ContainsKey(interfaceType.FullName))
                {
                    typeMap.Add(interfaceType.FullName, interfaceType);
                }
            }
        }

        ///<summary>
        /// Method to return the method of a given type at a specified index.
        ///</summary>
        ///<param name="name">Fully qualified name of the method to return</param>
        ///<param name="i">Index to use to return MethodInfo</param>
        ///<returns>MethodInfo</returns>
        public MethodInfo GetMethod(string name, int i)
        {
            Type type = null;
            type = (Type)typeMap[name];
            MethodInfo[] methods = type.GetMethods();
            if (i < methods.Length)
            {
                return methods[i];
            }

            return null;
        }
    }
}
