using System;
using System.Reflection;
using Pfz.Extensions;

namespace Pfz.Remoting
{
	/// <summary>
	/// Attribute used in remote interfaces to tell that all their properties must be cached or not, or
	/// directly in their properties, to tell something different that the interface tells.
	/// By default, without this attribute, property values are never cached.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
	public sealed class CacheRemotePropertyValuesAttribute:
		Attribute
	{
		/// <summary>
		/// Initializes a new instance of thie attribute.
		/// </summary>
		public CacheRemotePropertyValuesAttribute(bool cacheRemotePropertyValues = true)
		{
			CacheRemotePropertyValues = cacheRemotePropertyValues;
		}

		/// <summary>
		/// Gets a value indicating if property values must be cached.
		/// </summary>
		public bool CacheRemotePropertyValues { get; private set; }


		/// <summary>
		/// Gets the value of CacheRemotePropertyValues for the given property.
		/// </summary>
		public static bool GetValueFor(PropertyInfo propertyInfo)
		{
			if (propertyInfo == null)
				throw new ArgumentNullException("propertyInfo");

			var declaringType = propertyInfo.DeclaringType;
			if (!declaringType.IsInterface)
				throw new ArgumentException("CacheRemotePropertyValuesAttribute.GetValueFor must only be used for interface properties.");

			var attribute = propertyInfo.GetCustomAttribute<CacheRemotePropertyValuesAttribute>();
			if (attribute != null)
				return attribute.CacheRemotePropertyValues;

			attribute = declaringType.GetCustomAttribute<CacheRemotePropertyValuesAttribute>();
			if (attribute != null)
				return attribute.CacheRemotePropertyValues;

			return false;
		}
	}
}
