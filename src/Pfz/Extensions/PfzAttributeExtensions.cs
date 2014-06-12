using System;
using System.Reflection;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds some useful methods to the MemberInfo and Type types to
	/// work easily with attributes (Generics).
	/// </summary>
	public static class PfzAttributeExtensions
	{
		#region GetCustomAttributes
			/// <summary>
			/// Gets the attributes of the specified type, and return them typed.
			/// </summary>
			/// <typeparam name="T">The type of the attribute to find and to return.</typeparam>
			/// <param name="memberInfo">The memberInfo where the attributes will be searched.</param>
			/// <returns>A typed array with the attributes found.</returns>
			public static T[] GetCustomAttributes<T>(this MemberInfo memberInfo)
			where
				T: Attribute
			{
				if (memberInfo == null)
					throw new ArgumentNullException("memberInfo");

				return (T[])memberInfo.GetCustomAttributes(typeof(T), false);
			}

			/// <summary>
			/// Gets the attributes of the specified type, and return them typed.
			/// </summary>
			/// <typeparam name="T">The type of the attribute to find and to return.</typeparam>
			/// <param name="type">The type that can contains the attributes that will be searched.</param>
			/// <param name="inherit">If true search the attribute in base classes, but only if the attribute supports inheritance.</param>
			/// <returns>A typed array with the attributes found.</returns>
			public static T[] GetCustomAttributes<T>(this Type type, bool inherit)
			where
				T: Attribute
			{
				if (type == null)
					throw new ArgumentNullException("type");

				return (T[])type.GetCustomAttributes(typeof(T), inherit);
			}

			/// <summary>
			/// Gets the custom attributes of a parameterInfo, already typed.
			/// </summary>
			public static T[] GetCustomAttributes<T>(this ParameterInfo parameterInfo)
			where
				T: Attribute
			{
				if (parameterInfo == null)
					throw new ArgumentNullException("parameterInfo");

				return (T[])parameterInfo.GetCustomAttributes(typeof(T), false);
			}
		#endregion
		#region GetCustomAttribute
			/// <summary>
			/// Gets an attribute of the specified type, or null.
			/// This is useful when the attribute has AllowMultiple=false, but
			/// don't use it if the class can have more than one attribute of such
			/// type, as this method throws an exception when this happens.
			/// </summary>
			/// <typeparam name="T">The type of the parameter to find.</typeparam>
			/// <param name="memberInfo">The member info to search the attribute.</param>
			/// <returns>The found attribute or null.</returns>
			public static T GetCustomAttribute<T>(this MemberInfo memberInfo)
			where
				T: Attribute
			{
				T[] attributes = memberInfo.GetCustomAttributes<T>();
				
				switch(attributes.Length)
				{
					case 0:
						return null;
					
					case 1:
						return attributes[0];
				}

				throw new InvalidOperationException("There is more than one attribute of type " + typeof(T).FullName + ".");
			}

			/// <summary>
			/// Gets an attribute of the specified type, or null.
			/// This is useful when the attribute has AllowMultiple=false, but
			/// don't use it if the class can have more than one attribute of such
			/// type, as this method throws an exception when this happens.
			/// </summary>
			/// <typeparam name="T">The type of the parameter to find.</typeparam>
			/// <param name="type">The type to search the attribute.</param>
			/// <param name="inherit">true to search in base classes for attributes that support inheritance.</param>
			/// <returns>The found attribute or null.</returns>
			public static T GetCustomAttribute<T>(this Type type, bool inherit)
			where
				T: Attribute
			{
				T[] attributes = type.GetCustomAttributes<T>(inherit);
				
				switch(attributes.Length)
				{
					case 0:
						return null;
					
					case 1:
						return attributes[0];
				}

				throw new InvalidOperationException("There is more than one attribute of type " + typeof(T).FullName + ".");
			}

			/// <summary>
			/// Gets a CustomAttribute set to this parameterInfo.
			/// </summary>
			public static T GetCustomAttribute<T>(this ParameterInfo parameterInfo)
			where
				T: Attribute
			{
				T[] result = parameterInfo.GetCustomAttributes<T>();

				switch(result.Length)
				{
					case 0:
						return null;

					case 1:
						return result[0];
				}

				throw new InvalidOperationException("There is more than one attribute of type " + typeof(T).FullName + ".");
			}
		#endregion
		#region ContainsCustomAttribute
			/// <summary>
			/// Verifies if a member contains an specific custom attribute.
			/// </summary>
			/// <typeparam name="T">The type of the attribute to check for existance.</typeparam>
			/// <param name="member">The member in which to find search for attribute.</param>
			/// <returns>true if the member constains the attribute, false otherwise.</returns>
			public static bool ContainsCustomAttribute<T>(this MemberInfo member)
			where
				T: Attribute
			{
				if (member == null)
					throw new ArgumentNullException("member");

				return member.IsDefined(typeof(T), false);
			}

			/// <summary>
			/// Verifies if a type contains an specific custom attribute.
			/// </summary>
			/// <typeparam name="T">The type of the attribute to check for existance.</typeparam>
			/// <param name="type">The member in which to find search for attribute.</param>
			/// <param name="inherit">true to search in base classes for attributes that support inheritance.</param>
			/// <returns>true if the member constains the attribute, false otherwise.</returns>
			public static bool ContainsCustomAttribute<T>(this Type type, bool inherit)
			where
				T: Attribute
			{
				if (type == null)
					throw new ArgumentNullException("type");

				return type.IsDefined(typeof(T), inherit);
			}
		#endregion
	}
}
