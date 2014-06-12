using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds some methods to the Type class so you can discover the
	/// sub-types easily.
	/// </summary>
	public static class PfzTypeExtensions
	{
		#region GetDirectSubClasses
			private static Dictionary<KeyValuePair<Type, Assembly>, ReadOnlyCollection<Type>> _subClasses = new Dictionary<KeyValuePair<Type, Assembly>, ReadOnlyCollection<Type>>();
			private static ReaderWriterLockSlim _subClassesLock = new ReaderWriterLockSlim();
			/// <summary>
			/// Gets the sub-classes of the specific type, in the specific assembly.
			/// </summary>
			public static ReadOnlyCollection<Type> GetDirectSubClasses(this Type type, Assembly inAssembly)
			{
				ReadOnlyCollection<Type> result = null;
				KeyValuePair<Type, Assembly> pair = new KeyValuePair<Type,Assembly>(type, inAssembly);
				
				_subClassesLock.ReadLock
				(
					() => _subClasses.TryGetValue(pair, out result)
				);

				if (result == null)
				{
					_subClassesLock.UpgradeableLock
					(
						() =>
						{
							if (!_subClasses.TryGetValue(pair, out result))
							{
								List<Type> list = new List<Type>();
								foreach(Type possibleSubType in inAssembly.GetTypes())
									if (possibleSubType.BaseType == type)
										list.Add(possibleSubType);
							
								result = new ReadOnlyCollection<Type>(list.ToArray());

								_subClassesLock.WriteLock
								(
									() => _subClasses.Add(pair, result)
								);
							}
						}
					);
				}
				
				return result;
			}
		#endregion
		#region GetSubClassesRecursive
			private static Dictionary<KeyValuePair<Type, Assembly>, ReadOnlyCollection<Type>> _subClassesRecursive = new Dictionary<KeyValuePair<Type, Assembly>, ReadOnlyCollection<Type>>();
			private static ReaderWriterLockSlim _subClassesRecursiveLock = new ReaderWriterLockSlim();
			/// <summary>
			/// Gets the sub-classes of the specific type, in the specific assembly.
			/// </summary>
			public static ReadOnlyCollection<Type> GetSubClassesRecursive(this Type type, Assembly inAssembly)
			{
				ReadOnlyCollection<Type> result = null;
				KeyValuePair<Type, Assembly> pair = new KeyValuePair<Type,Assembly>(type, inAssembly);

				_subClassesRecursiveLock.ReadLock
				(
					() => _subClassesRecursive.TryGetValue(pair, out result)
				);

				if (result == null)
				{
					_subClassesRecursiveLock.UpgradeableLock
					(
						delegate
						{
							if (!_subClassesRecursive.TryGetValue(pair, out result))
							{
								List<Type> list = new List<Type>();
								foreach(Type possibleSubType in inAssembly.GetTypes())
									if (possibleSubType != type && type.IsAssignableFrom(possibleSubType))
										list.Add(possibleSubType);
							
								result = new ReadOnlyCollection<Type>(list.ToArray());

								_subClassesRecursiveLock.WriteLock
								(
									() => _subClassesRecursive.Add(pair, result)
								);
							}
						}
					);
				}
				
				return result;
			}
		#endregion
		
		#region GetOrderedInterfaces
			private static Dictionary<Type, ReadOnlyCollection<Type>> _orderedInterfaces = new Dictionary<Type, ReadOnlyCollection<Type>>();
			
			/// <summary>
			/// Gets the interfaces from this type ordered from the most "new" to the most
			/// "old" in the base types. Note that 2 or more interfaces added at the same
			/// "level" will not have an specific order.
			/// </summary>
			public static ReadOnlyCollection<Type> GetOrderedInterfaces(this Type type)
			{
				if (type == null)
					throw new ArgumentNullException("type");
			
				ReadOnlyCollection<Type> result;
				lock(_orderedInterfaces)
					result = _orderedInterfaces.GetValueOrDefault(type);
					
				if (result != null)
					return result;
				
				List<Type> orderedInterfacesList = new List<Type>();
				HashSet<Type> allInterfaces = new HashSet<Type>(type.GetInterfaces());
				HashSet<Type> interfacesToRemove = new HashSet<Type>();
				while(allInterfaces.Count > 0)
				{
					interfacesToRemove.Clear();
					
					foreach(var interfaceType in allInterfaces)
						foreach(var interfaceToRemove in interfaceType.GetInterfaces())
							interfacesToRemove.Add(interfaceToRemove);
					
					HashSet<Type> copy = new HashSet<Type>(allInterfaces);
					foreach(var interfaceType in interfacesToRemove)
						copy.Remove(interfaceType);
						
					foreach(var interfaceType in copy)
					{
						orderedInterfacesList.Add(interfaceType);
						allInterfaces.Remove(interfaceType);
					}
				}
				
				var orderedInterfacesArray = orderedInterfacesList.ToArray();
				result = new ReadOnlyCollection<Type>(orderedInterfacesArray);
				lock(_orderedInterfaces)
					_orderedInterfaces[type] = result;
				
				return result;
			}
		#endregion
		
		#region GetInterfaceProperties
			/// <summary>
			/// If this type is an interface, gets all the properties from this
			/// it's base interfaces to this interface.
			/// If this is not an interface, uses the custom GetProperty.
			/// </summary>
			public static IEnumerable<PropertyInfo> GetInterfaceProperties(this Type type)
			{
				if (type == null)
					throw new ArgumentNullException("type");
					
				if (type.IsInterface)
				{
					var interfaceTypes = type.GetOrderedInterfaces();
					for (int i=interfaceTypes.Count-1; i>=0; i--)
					{
						Type interfaceType = interfaceTypes[i];

						foreach(var propertyInfo in interfaceType.GetProperties())
							yield return propertyInfo;
					}

					foreach(var property in type.GetProperties())
						yield return property;

					yield break;
				}

				List<Type> types = new List<Type>();
				while(type != null)
				{
					types.Add(type);
					type = type.BaseType;
				}

				HashSet<string> alreadyReturnedProperties = new HashSet<string>();
				int count = types.Count;
				for (int i=count-1; i>=0; i--)
				{
					type = types[i];
					foreach(var property in type.GetProperties(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public))
						if (alreadyReturnedProperties.Add(property.Name))
							yield return property;
				}
			}
		#endregion
		#region TryGetInterfaceProperty
			/// <summary>
			/// Tries to get a property by it's name.
			/// If this is an interface, it also looks all the base interfaces to find
			/// the property.
			/// </summary>
			public static PropertyInfo TryGetInterfaceProperty(this Type type, string propertyName)
			{
				if (type == null)
					throw new ArgumentNullException("type");
				
				if (propertyName == null)
					throw new ArgumentNullException("propertyName");
				
				var result = type.GetProperty(propertyName);
				if (result != null)
					return result;
				
				if (type.IsInterface)
				{
					foreach(var interfaceType in type.GetOrderedInterfaces())
					{
						result = interfaceType.GetProperty(propertyName);
						if (result != null)
							return result;
					}
				}
				
				return null;
			}
		#endregion
		#region GetInterfaceProperty
			/// <summary>
			/// Gets a property by it's name.
			/// If this type is an interface, search in it's base interfaces.
			/// Throws an exception if no such property is found.
			/// </summary>
			public static PropertyInfo GetInterfaceProperty(this Type type, string propertyName)
			{
				if (type == null)
					throw new ArgumentNullException("type");

				PropertyInfo result = TryGetInterfaceProperty(type, propertyName);
				
				if (result == null)
					throw new ArgumentException("Property \"" + propertyName + "\" was not found in type " + type.FullName + ".");
				
				return result;
			}
		#endregion

		#region GetFinalInterfaces
			/// <summary>
			/// Gets a reduced list of interfaces, removing Interfaces that are requisites from other interfaces already
			/// in the list.
			/// For example, an object that implemented IAdvancedDisposable must also implement IDisposable. Getting
			/// the list of interfaces will get both (IDisposable and IAdvancedDisposable) while this method
			/// will only return IAdvancedDisposable.
			/// </summary>
			public static Type[] GetFinalInterfaces(this Type type)
			{
				if (type == null)
					throw new ArgumentNullException("type");

				HashSet<Type> interfaceTypes = new HashSet<Type>(type.GetInterfaces());
				HashSet<Type> interfacesToRemove = new HashSet<Type>();
				foreach(var interfaceType in interfaceTypes)
				{
					if (interfaceType.IsPublic)
					{
						foreach(var baseInterface in interfaceType.GetInterfaces())
							interfacesToRemove.Add(baseInterface);
					}
					else
						interfacesToRemove.Add(interfaceType);
				}

				foreach(var baseInterface in interfacesToRemove)
					interfaceTypes.Remove(baseInterface);

				Type[] result = new Type[interfaceTypes.Count];
				interfaceTypes.CopyTo(result);
				return result;
			}
		#endregion

		#region GetCompatibleConstructor
			/// <summary>
			/// Tries to get a constructor with the given parameters.
			/// If an exact match is not found, tries to search a compatible one.
			/// If acceptAConstructorCompatibleByCast is true, you can ask for a constructor(object) and
			/// receive a constructor(int), because with a cast that should be valid.
			/// If none is found, return null.
			/// </summary>
			public static ConstructorInfo GetCompatibleConstructor(this Type type, bool acceptAConstructorCompatibleByCast, bool areStringsAndEnumsCompatible, params Type[] parameterTypes)
			{
				if (type == null)
					throw new ArgumentNullException("type");

				if (parameterTypes == null)
					throw new ArgumentNullException("parameterTypes");

				ConstructorInfo result = type.GetConstructor(parameterTypes);
				if (result != null)
					return result;

				var constructors = type.GetConstructors();
				return _CompareParameters(null, acceptAConstructorCompatibleByCast, areStringsAndEnumsCompatible, parameterTypes, constructors);
			}
		#endregion
		#region GetCompatibleMethod
			/// <summary>
			/// Tries to get a method with the given name and parameters.
			/// If an exact match is not found, tries to search a compatible one.
			/// If acceptAMethodCompatibleByCast is true, you can ask for a Method(object) and
			/// receive a Method(int), because with a cast that should be valid.
			/// If none is found, return null.
			/// </summary>
			public static MethodInfo GetCompatibleMethod(this Type type, string name, BindingFlags bindingFlags, bool acceptAMethodCompatibleByCast, bool areStringsAndEnumsCompatible, params Type[] parameterTypes)
			{
				if (type == null)
					throw new ArgumentNullException("type");

				if (name == null)
					throw new ArgumentNullException("name");

				if (parameterTypes == null)
					throw new ArgumentNullException("parameterTypes");

				var method = type.GetMethod(name, bindingFlags, null, parameterTypes, null);
				if (method != null)
					return method;

				var methods = type.GetMethods(bindingFlags);
				return _CompareParameters(name, acceptAMethodCompatibleByCast, areStringsAndEnumsCompatible, parameterTypes, methods);
			}
		#endregion
		#region _CompareParameters
			private static T _CompareParameters<T>(string name, bool acceptAMethodCompatibleByCast, bool areStringsAndEnumsCompatible, Type[] parameterTypes, T[] methods)
			where
				T: MethodBase
			{
				foreach (var methodInfo in methods)
				{
					if (name != null)
						if (methodInfo.Name != name)
							continue;

					var methodParameterTypes = methodInfo.GetParameterTypes();
					if (methodParameterTypes.Length != parameterTypes.Length)
						continue;

					bool ok = true;
					for (int i = 0; i < parameterTypes.Length; i++)
					{
						var methodParameterType = methodParameterTypes[i];
						var parameterType = parameterTypes[i];

						if (parameterType.IsGenericParameter || methodParameterType.IsGenericParameter)
							continue;

						if (!methodParameterType.IsAssignableFrom(parameterType))
						{
							if (acceptAMethodCompatibleByCast)
							{
								if (parameterType.IsAssignableFrom(methodParameterType))
									continue;

								if (methodParameterType.IsByRef && parameterType.IsByRef)
								{
									var element1 = parameterType.GetElementType();
									var element2 = methodParameterType.GetElementType();

									if (element1.IsGenericParameter || element2.IsGenericParameter)
										continue;

									if (element1.IsAssignableFrom(element2) || element2.IsAssignableFrom(element1))
										continue;
								}
							}

							if (areStringsAndEnumsCompatible)
							{
								if (methodParameterType == typeof(string) && parameterType.IsEnum)
									continue;

								if (parameterType == typeof(string) && methodParameterType.IsEnum)
									continue;
							}

							ok = false;
							break;
						}
					}

					if (ok)
						return methodInfo;
				}

				return null;
			}
		#endregion
	}
}
