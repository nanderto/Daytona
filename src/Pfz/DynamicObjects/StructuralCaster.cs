using System;
using System.Collections.Generic;
using System.Reflection;
using Pfz.DynamicObjects.Internal;
using Pfz.Extensions;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Class responsable for casting objects to an interface type that they don't support, but have equivalent methods.
	/// </summary>
	public static class StructuralCaster
	{
		#region Cast
			private static HashSet<KeyValuePair<Type, Type>> _validatedTypes = new HashSet<KeyValuePair<Type, Type>>();

			/// <summary>
			/// Casts an object to a given type.
			/// </summary>
			public static T Cast<T>(object source, object securityToken=null)
			{
				if (source == null)
					return default(T);

				if (source is T)
				{
					T result = (T)source;
					return result;
				}

				var baseDuck = source as BaseDuck;
				if (baseDuck != null)
					return baseDuck.StructuralCast<T>(securityToken);

				if (!typeof(T).IsInterface)
					throw new ArgumentException("Cast to " + typeof(T).FullName + " is not valid.");

				return _Cast<T>(source, securityToken);
			}

			internal static T _Cast<T>(object source, object securityToken)
			{
				Type type = source.GetType();
				var pair = new KeyValuePair<Type, Type>(typeof(T), type);

				lock (_validatedTypes)
				{
					if (!_validatedTypes.Contains(pair))
					{
						_Validate(false, typeof(T), type);

						foreach (var baseInterface in typeof(T).GetInterfaces())
							_Validate(false, baseInterface, type);

						_validatedTypes.Add(pair);
					}
				}

				return DuckCaster._FinalCast<T>(source, type, securityToken);
			}
		#endregion
		#region GetStaticInterface
			private static readonly Dictionary<KeyValuePair<Type, Type>, object> _staticInterfaces = new Dictionary<KeyValuePair<Type, Type>, object>();

			/// <summary>
			/// Gets the static methods of a type into an interface.
			/// </summary>
			public static T GetStaticInterface<T>(Type typeWithStaticMembers)
			{
				if (typeWithStaticMembers == null)
					throw new ArgumentNullException("typeWithStaticMembers");

				if (!typeof(T).IsInterface)
					throw new ArgumentException(typeof(T).FullName + " is not an interface type.");

				T result;
				var key = new KeyValuePair<Type, Type>(typeof(T), typeWithStaticMembers);
				lock(_staticInterfaces)
				{
					object untypedResult;
					if (_staticInterfaces.TryGetValue(key, out untypedResult))
						return (T)untypedResult;

					// First we need to validate the static properties, methods and events, so
					// we at least have when we try to get the object, if the static class is invalid, instead
					// of having an exception when calling a method or property.
					_Validate(true, typeof(T), typeWithStaticMembers);

					foreach(var baseInterface in typeof(T).GetInterfaces())
						_Validate(true, baseInterface, typeWithStaticMembers);

					result = (T)DuckCaster._GetStaticInterface<T>(typeWithStaticMembers, key);
					_staticInterfaces.Add(key, result);
				}

				return result;
			}
		#endregion

		#region _Validate
			private static void _Validate(bool expectStatic, Type interfaceType, Type toType)
			{
				if (!expectStatic)
					if (interfaceType.IsAssignableFrom(toType))
						return;

				foreach(var propertyInfo in interfaceType.GetProperties())
				{
					string propertyName = propertyInfo.Name;
					var toProperty = toType.GetProperty(propertyName);

					Type propertyType = propertyInfo.PropertyType;

					if (toProperty == null)
					{
						var staticField = toType.GetField(propertyName);

						if (staticField == null)
							throw new ArgumentException(toType.FullName + " does not have a property or field named " + propertyName + ".");

						if (staticField.IsStatic != expectStatic)
						{
							if (expectStatic)
								throw new ArgumentException("Field " + propertyName + " of type " + toType.FullName + " is not static.");
							else
								throw new ArgumentException("Field " + propertyName + " of type " + toType.FullName + " is static.");
						}

						if (!propertyType.IsAssignableFrom(staticField.FieldType) && !staticField.FieldType.IsAssignableFrom(propertyType))
							throw new ArgumentException("Field " + propertyName + " of type " + toType.FullName + " does not has a return type compatible with " + propertyType.FullName + ".");

						if (staticField.IsInitOnly || staticField.IsLiteral)
						{

							if (propertyInfo.CanWrite)
								throw new ArgumentException("Field " + propertyName + " of type " + toType.FullName + " is read-only.");
						}

						if (propertyInfo.GetIndexParameters().Length != 0)
							throw new ArgumentException("Field " + propertyName + " of type " + toType.FullName + " can't be used to implement an indexed property.");

						continue;
					}

					if (toProperty.GetAccessors()[0].IsStatic != expectStatic)
					{
						if (expectStatic)
							throw new ArgumentException("Property " + propertyName + " of type " + toType.FullName + " is not static.");
						else
							throw new ArgumentException("Property " + propertyName + " of type " + toType.FullName + " is static.");
					}

					Type staticPropertyType = toProperty.PropertyType;
					if (!propertyType.IsAssignableFrom(staticPropertyType) && !staticPropertyType.IsAssignableFrom(propertyType))
						if (!((propertyType.IsEnum && staticPropertyType == typeof(string)) || (staticPropertyType.IsEnum && propertyType == typeof(string))))
							throw new ArgumentException("Property " + propertyName + " of type " + toType.FullName + " does not has a return type compatible with " + propertyType.FullName + ".");

					if (propertyInfo.CanRead && !toProperty.CanRead)
						throw new ArgumentException("Property " + propertyName + " of type " + toType.FullName + " does not have a read accessor.");

					if (propertyInfo.CanWrite && !toProperty.CanWrite)
						throw new ArgumentException("Property " + propertyName + " of type " + toType.FullName + " does not have a write accessor.");

					var parameters1 = propertyInfo.GetIndexParameters();
					var parameters2 = toProperty.GetIndexParameters();

					int length = parameters1.Length;
					if (length != parameters2.Length)
						throw new ArgumentException("Property " + propertyName + " of type " + toType.FullName + " has an invalid index parameter count.");

					for(int i=0; i<length; i++)
					{
						var parameterInfo1 = parameters1[i];
						var parameterInfo2 = parameters2[i];

						if (!parameterInfo2.ParameterType.IsAssignableFrom(parameterInfo1.ParameterType) && !parameterInfo1.ParameterType.IsAssignableFrom(parameterInfo2.ParameterType))
							throw new ArgumentException("Indexed parameter " + i + " of property " + propertyName + " of type " + toType.FullName + " has an invalid type.");
					}
				}
				foreach(var eventInfo in interfaceType.GetEvents())
				{
					string eventName = eventInfo.Name;
					var toEvent = toType.GetEvent(eventName);

					if (toEvent == null)
						throw new ArgumentException(toType.FullName + " does not have an event named " + eventName + ".");

					if (toEvent.GetAddMethod().IsStatic != expectStatic)
					{
						if (expectStatic)
							throw new ArgumentException("Event " + eventName + " of type " + toType.FullName + " is not static.");
						else
							throw new ArgumentException("Event " + eventName + " of type " + toType.FullName + " is static.");
					}

					Type eventType = eventInfo.EventHandlerType;
					if (eventType != toEvent.EventHandlerType)
						throw new ArgumentException("Event " + eventName + " of type " + toType.FullName + " is not of type " + eventType.FullName + ".");
				}

				foreach(var fromMethodInfo in interfaceType.GetMethods())
				{
					if (fromMethodInfo.IsSpecialName)
						continue;

					string methodName = fromMethodInfo.Name;

					Type[] parameterTypes = fromMethodInfo.GetParameterTypes();

					Type otherReturnType;

					MethodBase toMethod;
					if (fromMethodInfo.ContainsCustomAttribute<CallConstructorAttribute>())
					{
						if (!expectStatic)
							throw new ArgumentException("Method " + methodName + " of type " + toType.FullName + " must call a constructor, but this is not valid for Casts.");

						if (fromMethodInfo.IsGenericMethodDefinition)
							throw new ArgumentException("Method " + methodName + " of type " + toType.FullName + " is a generic method definition, that must call a constructor. Generic method definitions can't call constructors.");

						var constructor = toType.GetCompatibleConstructor(true, true, parameterTypes);

						if (constructor == null)
							throw new ArgumentException(toType.FullName + " does not have a compatible constructor for " + methodName + ".");

						otherReturnType = toType;

						toMethod = constructor;
					}
					else
					{
						BindingFlags bindingFlags;
						if (expectStatic)
							bindingFlags = BindingFlags.Public | BindingFlags.Static;
						else
							bindingFlags = BindingFlags.Public | BindingFlags.Instance;

						MethodInfo toMethodInfo = toType.GetCompatibleMethod(methodName, bindingFlags, true, true, parameterTypes);;

						if (toMethodInfo == null)
							throw new ArgumentException(toType.FullName + " does not have a method " + methodName + " with compatible parameter types.");

						if (toMethodInfo.IsGenericMethodDefinition)
							if (DuckCaster._MakeGenericMethod(fromMethodInfo, toMethodInfo) == null)
								throw new ArgumentException("Method " + methodName + " of interface " + interfaceType.FullName + " can't call the method with the same name on " + toType.FullName + ".");

						otherReturnType = toMethodInfo.ReturnType;
						toMethod = toMethodInfo;

						if (toMethod.IsStatic != expectStatic)
						{
							if (expectStatic)
								throw new ArgumentException("Method " + methodName + " of type " + toType.FullName + " is not static.");
							else
								throw new ArgumentException("Method " + methodName + " of type " + toType.FullName + " is static.");
						}

					}

					Type[] toParameterTypes = toMethod.GetParameterTypes();
					for (int i=0; i<toParameterTypes.Length; i++)
					{
						var fromParameterType = parameterTypes[i];
						var toParameterType = toParameterTypes[i];

						if (fromParameterType.IsByRef != toParameterType.IsByRef)
							throw new ArgumentException("Parameter at index " + i + " from method " + methodName + " of type " + interfaceType.FullName + " has a ByRef different than the real class.");
					}

					Type returnType = fromMethodInfo.ReturnType;
					if (fromMethodInfo.ContainsCustomAttribute<CastAttribute>())
					{
						if (returnType == typeof(void))
							throw new ArgumentException("Method " + methodName + " of interface type " + interfaceType.FullName + " does not returns a value, but is marked with [CastResultAttribute].");

						if (!returnType.IsInterface)
							throw new ArgumentException("Method " + methodName + " of interface type " + interfaceType.FullName + " is marked with [CastResultAttribute], but its result is not an interface type.");
					}
					else
					{
						if (returnType != typeof(void))
							if (!returnType.IsGenericParameter && !otherReturnType.IsGenericParameter)
								if (!returnType.IsAssignableFrom(otherReturnType) && !otherReturnType.IsAssignableFrom(returnType))
									throw new ArgumentException("Method " + methodName + " of type " + toType.FullName + " does not have a compatible return type.");
					}
				}
			}
		#endregion
	}
}
