using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Pfz.DynamicObjects.Internal;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Class responsible for creating instances of objects that implement a given interface for an object that
	/// has the needed characteristics but does not actually implement the interface (that is, the original object could
	/// be made to implement that interface, but it does not and, if you don't have a way to change the original object,
	/// this is the only solution).
	/// </summary>
	public static class DuckCaster
	{
		#region Cast
			/// <summary>
			/// Tries to cast the given object to the given interface type.
			/// If the cast is not valid, tries to implement a proxy interface to redirect to it.
			/// Throws an exception if the given object is not compatible with the interface.
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
					return baseDuck.DuckCast<T>(securityToken);

				if (!typeof(T).IsInterface)
					throw new ArgumentException("Cast to " + typeof(T).FullName + " is not valid.");

				return _Cast<T>(source, securityToken);
			}

			internal static T _Cast<T>(object source, object securityToken)
			{
				Type type = source.GetType();
				var pair = new KeyValuePair<Type, Type>(typeof(T), type);

				return _FinalCast<T>(source, type, securityToken);
			}

			internal static T _FinalCast<T>(object source, Type type, object securityToken)
			{
				Type resultType = _ImplementRedirector(typeof(T), type, true);
				ConstructorInfo constructor = resultType.GetConstructors()[0];
				object untypedResult = constructor.Invoke(new object[] { source, securityToken });
				T result = (T)untypedResult;
				return result;
			}
		#endregion
		#region GetStaticInterface
			private static readonly Dictionary<KeyValuePair<Type, Type>, object> _staticInterfaces = new Dictionary<KeyValuePair<Type, Type>, object>();
			/// <summary>
			/// This method will generate an implemented object for interface type T, redirecting
			/// its methods, properties and events to the static properties, methods and events of
			/// a given type. This way, you can have "virtual static methods".
			/// </summary>
			public static T GetStaticInterface<T>(Type typeWithStaticMembers)
			{
				if (typeWithStaticMembers == null)
					throw new ArgumentNullException("typeWithStaticMembers");

				if (!typeof(T).IsInterface)
					throw new ArgumentException(typeof(T).FullName + " is not an interface type.");

				object result;
				var key = new KeyValuePair<Type, Type>(typeof(T), typeWithStaticMembers);
				result = _GetStaticInterface<T>(typeWithStaticMembers, key);
				return (T)result;
			}

			internal static object _GetStaticInterface<T>(Type typeWithStaticMembers, KeyValuePair<Type, Type> key)
			{
				object result;
				lock (_staticInterfaces)
				{
					if (!_staticInterfaces.TryGetValue(key, out result))
					{
						Type resultType = _ImplementRedirector(typeof(T), typeWithStaticMembers, false);
						result = resultType.GetConstructor(Type.EmptyTypes).Invoke(null);

						_staticInterfaces.Add(key, result);
					}
				}
				return result;
			}
		#endregion

		#region _ImplementRedirector
			private static readonly Dictionary<KeyValuePair<Type, Type>, Type> _implementedRedirectors = new Dictionary<KeyValuePair<Type, Type>, Type>();

			private static readonly MethodInfo _methodGetType = ReflectionHelper<BaseDuckForTypes>.GetProperty((obj) => obj.Type).GetGetMethod();
			private static readonly ConstructorInfo _constructorNotSupportedException = ReflectionHelper.GetConstructor(() => new NotSupportedException());

			internal static Type _ImplementRedirector(Type interfaceType, Type toType, bool hasInstance)
			{
				var pair = new KeyValuePair<Type, Type>(interfaceType, toType);

				Type result;
				lock(_implementedRedirectors)
				{
					if (!_implementedRedirectors.TryGetValue(pair, out result))
					{
						var type = 
							_DynamicModule.DefineType
							(
								string.Concat("Pfz.DynamicTypes.Caster.From_", interfaceType.Name, "_To_", toType.Name),
								TypeAttributes.Public | TypeAttributes.Sealed,
								hasInstance?typeof(BaseDuckForInstances):typeof(BaseDuckForTypes),
								new Type[]{interfaceType}
							);

						if (hasInstance)
						{
							var constructor = 
								type.DefineConstructor
								(
									MethodAttributes.Public | MethodAttributes.Final,
									CallingConventions.Standard,
									new Type[]{toType, typeof(object)}
								);

							var constructorGenerator = constructor.GetILGenerator();
							constructorGenerator.EmitLoadArgument(0);
							constructorGenerator.EmitLoadArgument(1);
							constructorGenerator.EmitLoadArgument(2);
							constructorGenerator.Emit(OpCodes.Call, _constructorBaseStructuralForInstances);
							constructorGenerator.EmitReturn();
						}
						else
						{
							type.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.Final);
						}

						if (!hasInstance)
						{
							var getType = 
								type.DefineMethod
								(
									"get_Type",
									MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.Public,
									typeof(Type),
									Type.EmptyTypes
								);
							var getTypeGenerator = getType.GetILGenerator();
							getTypeGenerator.FullLoadToken(toType);
							getTypeGenerator.EmitReturn();
							type.DefineMethodOverride(getType, _methodGetType);
						}

						var usedNames = new HashSet<string>();
						var methodDictionary = new Dictionary<MethodInfo, MethodBuilder>();

						_ImplementRedirector(type, interfaceType, toType, hasInstance, usedNames, methodDictionary);
						foreach(var baseInterface in interfaceType.GetInterfaces())
							_ImplementRedirector(type, baseInterface, toType, hasInstance, usedNames, methodDictionary);

						result = type.CreateType();
					}
				}
				return result;
			}

			private static readonly ConstructorInfo _constructorBaseStructuralForTypes = typeof(BaseDuckForTypes).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
			private static readonly ConstructorInfo _constructorBaseStructuralForInstances = typeof(BaseDuckForInstances).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];
			private static readonly FieldInfo _fieldBaseStructuralForInstances = ReflectionHelper<BaseDuckForInstances>.GetField((obj) => (obj)._target);
			private static readonly Type[] _typeArrayOfTypes = new Type[]{typeof(Type)};
			private static readonly MethodInfo _methodGetField = ReflectionHelper<Type>.GetMethod((type) => type.GetField("someField"));
			private static readonly MethodInfo _methodGetValue = ReflectionHelper<FieldInfo>.GetMethod((fieldInfo) => fieldInfo.GetValue(null));
			private static void _ImplementRedirector(TypeBuilder type, Type interfaceType, Type toType, bool hasInstance, HashSet<string> usedNames, Dictionary<MethodInfo, MethodBuilder> methodDictionary)
			{
				methodDictionary.Clear();
				FieldInfo instanceField = null;

				if (hasInstance)
				{
					instanceField = _fieldBaseStructuralForInstances;

					if (interfaceType.IsAssignableFrom(toType))
					{
						_SimpleRedirect(type, interfaceType, toType, usedNames, methodDictionary);
						_ImplementPropertiesAndEvents(type, interfaceType, methodDictionary);
						return;
					}
				}

				ILGenerator staticConstructorGenerator = null;
				foreach(var oldMethod in interfaceType.GetMethods())
				{
					string name = oldMethod.Name;
					Type oldReturnType = oldMethod.ReturnType;
					var oldParameterTypes = oldMethod.GetParameterTypes();

					var newMethod = _DefineNewMethod(type, usedNames, oldMethod, oldReturnType, oldParameterTypes, methodDictionary);

					if (oldMethod.IsGenericMethodDefinition)
						_DefineGenericParameters(oldMethod, newMethod);

					type.DefineMethodOverride(newMethod, oldMethod);

					var generator = newMethod.GetILGenerator();

					MethodBase toMethod;
					MethodInfo toMethodInfo;
					ConstructorInfo toConstructorInfo;
					
					Type newReturnType = null;
					CastAttribute resultCastAttribute = oldMethod.GetCustomAttribute<CastAttribute>();
					if (oldMethod.ContainsCustomAttribute<CallConstructorAttribute>())
					{
						if (oldMethod.IsGenericMethodDefinition)
						{
							_EmitNotSupported(generator);
							continue;
						}

						toConstructorInfo = toType.GetCompatibleConstructor(true, true, oldParameterTypes);;
						toMethod = toConstructorInfo;
						toMethodInfo = null;
						newReturnType = toType;

						if (!_ValidateReturnType(toType, name, oldReturnType, newReturnType, resultCastAttribute, generator))
							continue;
					}
					else
					{
						toConstructorInfo = null;

						BindingFlags bindingFlags;
						if (hasInstance)
							bindingFlags = BindingFlags.Public | BindingFlags.Instance;
						else
							bindingFlags = BindingFlags.Public | BindingFlags.Static;

						toMethodInfo = toType.GetCompatibleMethod(name, bindingFlags, true, true, oldParameterTypes);

						if (toMethodInfo != null)
						{
							if (toMethodInfo.IsGenericMethodDefinition)
							{
								toMethodInfo = _MakeGenericMethod(oldMethod, toMethodInfo);
								if (toMethodInfo == null)
								{
									_EmitNotSupported(generator);
									continue;
								}
							}

							newReturnType = toMethodInfo.ReturnType;
							if (!_ValidateReturnType(toType, name, oldReturnType, newReturnType, resultCastAttribute, generator))
								continue;
						}

						toMethod = toMethodInfo;

						if (hasInstance)
						{
							generator.EmitLoadArgument(0);
							generator.EmitLoadField(instanceField);
						}

						if (toMethodInfo == null)
						{
							if (name.StartsWith("get_"))
							{
								FieldInfo field = toType.GetField(name.Substring(4));
								if (field == null)
								{
									_EmitNotSupported(generator);
									continue;
								}

								resultCastAttribute = _GetProperty(interfaceType, name).GetCustomAttribute<CastAttribute>();

								newReturnType = field.FieldType;
								if (!_ValidateReturnType(toType, name, oldReturnType, newReturnType, resultCastAttribute, generator))
									continue;

								if (hasInstance)
									generator.EmitLoadField(field);
								else
								{
									if (field.IsLiteral)
									{
										if (resultCastAttribute == null && !oldReturnType.IsAssignableFrom(newReturnType))
										{
											_EmitNotSupported(generator);
											continue;
										}

										if (staticConstructorGenerator == null)
										{
											var staticConstructor = 
												type.DefineConstructor
												(
													MethodAttributes.Static | MethodAttributes.Public,
													CallingConventions.Standard,
													Type.EmptyTypes
												);

											staticConstructorGenerator = staticConstructor.GetILGenerator();
										}

										staticConstructorGenerator.FullLoadToken(toType);
										staticConstructorGenerator.EmitLoadString(field.Name);
										staticConstructorGenerator.Emit(OpCodes.Callvirt, _methodGetField);
										staticConstructorGenerator.EmitLoadNull();
										staticConstructorGenerator.Emit(OpCodes.Callvirt, _methodGetValue);

										newReturnType = typeof(object);
										_EmitBoxOrCast(staticConstructorGenerator, resultCastAttribute, newReturnType, oldReturnType);

										var staticField = 
											type.DefineField
											(
												string.Concat("<<", toType.FullName, '.', name, ">>"),
												newReturnType,
												FieldAttributes.Public | FieldAttributes.InitOnly | FieldAttributes.Static
											);

										staticConstructorGenerator.EmitStoreStaticField(staticField);
										generator.EmitLoadStaticField(staticField);
										generator.EmitReturn();
										continue;
									}
									else
										generator.EmitLoadStaticField(field);
								}
							}
							else
							if (name.StartsWith("set_"))
							{
								FieldInfo field = toType.GetField(name.Substring(4));
								if (field == null)
								{
									_EmitNotSupported(generator);
									continue;
								}

								resultCastAttribute = _GetProperty(interfaceType, name).GetCustomAttribute<CastAttribute>();

								newReturnType = typeof(void);
								generator.EmitLoadArgument(1);

								var oldParameterType = oldParameterTypes[0];
								var newParameterType = field.FieldType;

								_EmitBoxOrCast(generator, resultCastAttribute, oldParameterType, newParameterType);

								if (hasInstance)
									generator.EmitStoreField(field);
								else
									generator.EmitStoreStaticField(field);
							}
							else
							{
								_EmitNotSupported(generator);
								continue;
							}
						}
					}

					if (toMethod != null)
					{
						var oldParameters = oldMethod.GetParameters();
						var newParameters = toMethod.GetParameters();
						List<LocalBuilder> localsForCast = null;
						for(int i=0; i<oldParameterTypes.Length; i++)
						{
							var oldParameter = oldParameters[i];
							var newParameter = newParameters[i];

							var oldParameterType = oldParameterTypes[i];
							var newParameterType = newParameter.ParameterType;

							CastAttribute castAttribute = oldParameters[i].GetCustomAttribute<CastAttribute>();
							bool emitted = false;
							if (oldParameterType != newParameterType)
							{
								if (oldParameterType.IsByRef != newParameterType.IsByRef)
								{
									_EmitNotSupported(generator);
									break;
								}

								if (newParameterType.IsByRef)
								{
									if (localsForCast == null)
										localsForCast = new List<LocalBuilder>();

									var oldElement = oldParameterType.GetElementType();
									var newElement = newParameterType.GetElementType();

									var localForCast = generator.DeclareLocal(newElement);
									localsForCast.Add(localForCast);

									if (!newParameter.IsOut && !oldParameter.IsOut)
									{
										generator.EmitLoadArgument(i+1);
										generator.Emit(OpCodes.Ldobj, oldElement);

										_EmitBoxOrCast(generator, castAttribute, oldElement, newElement);

										generator.EmitStoreLocal(localForCast);
									}

									generator.Emit(OpCodes.Ldloca, localForCast);
									emitted = true;
								}
							}
							
							if (!emitted)
							{
								generator.EmitLoadArgument(i+1);

								_EmitBoxOrCast(generator, castAttribute, oldParameterType, newParameterType);
							}
						}

						if (toMethodInfo != null)
							generator.Emit(OpCodes.Call, toMethodInfo);
						else
							generator.Emit(OpCodes.Newobj, toConstructorInfo);

						if (localsForCast != null)
						{
							int localIndex = -1;
							for(int i=0; i<oldParameterTypes.Length; i++)
							{
								var oldParameter = oldParameters[i];
								var newParameter = newParameters[i];

								var oldParameterType = oldParameterTypes[i];
								var newParameterType = newParameter.ParameterType;

								if (oldParameterType != newParameterType && oldParameterType.IsByRef)
								{
									localIndex++;
									var oldElement = oldParameterType.GetElementType();

									generator.EmitLoadArgument(i+1);
									generator.EmitLoadLocal(localsForCast[localIndex]);

									CastAttribute castAttribute = oldParameters[i].GetCustomAttribute<CastAttribute>();
									_EmitBoxOrCast(generator, castAttribute, newParameterType.GetElementType(), oldElement);

									generator.Emit(OpCodes.Stobj, oldElement);
								}
							}
						}
					}

					if (oldReturnType == typeof(void) && newReturnType != typeof(void))
						generator.Emit(OpCodes.Pop);
					else
					{
						if (resultCastAttribute != null)
						{
							if (oldReturnType == typeof(void))
								throw new ArgumentException("Method " + name + " of interface type " + interfaceType.FullName + " does not returns a value, but is marked with [CastAttribute].");

							if (!oldReturnType.IsInterface)
								throw new ArgumentException("Method " + name + " of interface type " + interfaceType.FullName + " is marked with [CastAttribute], but its result is not an interface type.");
						}

						_EmitBoxOrCast(generator, resultCastAttribute, newReturnType, oldReturnType);
					}

					generator.EmitReturn();
				}

				if (staticConstructorGenerator != null)
					staticConstructorGenerator.EmitReturn();

				_ImplementPropertiesAndEvents(type, interfaceType, methodDictionary);
			}
		#endregion
		#region _DefineNewMethod
			private static MethodBuilder _DefineNewMethod(TypeBuilder type, HashSet<string> usedNames, MethodInfo oldMethod, Type oldReturnType, Type[] oldParameterTypes, Dictionary<MethodInfo, MethodBuilder> methodDictionary)
			{
				string name = oldMethod.Name;

				bool isNew = usedNames.Add(name);
				if (!isNew)
					name = string.Concat(oldMethod.DeclaringType.FullName, ".", name);

				var result = type.DefineMethod
				(
					name,
					MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.Final,
					oldReturnType,
					oldParameterTypes
				);

				if (isNew)
					methodDictionary.Add(oldMethod, result);

				return result;
			}
		#endregion
		#region _ValidateReturnType
			private static bool _ValidateReturnType(Type toType, string name, Type oldReturnType, Type newReturnType, CastAttribute resultCastAttribute, ILGenerator generator)
			{
				if (resultCastAttribute != null)
					return true;

				if (oldReturnType == typeof(void))
					return true;

				if (newReturnType.IsGenericParameter)
					return true;

				if (oldReturnType.IsGenericParameter)
					return true;

				if (newReturnType.IsAssignableFrom(oldReturnType))
					return true;

				if (oldReturnType.IsAssignableFrom(newReturnType))
					return true;

				if (!((newReturnType.IsEnum && oldReturnType == typeof(string)) || (oldReturnType.IsEnum && newReturnType == typeof(string))))
				{
					_EmitNotSupported(generator);
					return false;
				}

				return true;
			}
		#endregion
		#region _ImplementPropertiesAndEvents
			private static void _ImplementPropertiesAndEvents(TypeBuilder type, Type interfaceType, Dictionary<MethodInfo, MethodBuilder> methodDictionary)
			{
				foreach(var oldProperty in interfaceType.GetProperties())
				{
					MethodBuilder getMethod = null;
					MethodBuilder setMethod = null;

					if (oldProperty.CanRead)
						if (!methodDictionary.TryGetValue(oldProperty.GetGetMethod(), out getMethod))
							continue;

					if (oldProperty.CanWrite)
						if (!methodDictionary.TryGetValue(oldProperty.GetSetMethod(), out setMethod))
							continue;

					string name = oldProperty.Name;
					Type returnType = oldProperty.PropertyType;
					var parameterTypes = oldProperty.GetIndexParameters().GetTypes();

					var newProperty = 
						type.DefineProperty
						(
							name,
							PropertyAttributes.None,
							returnType,
							parameterTypes
						);

					if (getMethod != null)
						newProperty.SetGetMethod(getMethod);

					if (setMethod != null)
						newProperty.SetSetMethod(setMethod);
				}

				foreach(var oldEvent in interfaceType.GetEvents())
				{
					MethodBuilder addMethod;
					MethodBuilder removeMethod;

					if (!methodDictionary.TryGetValue(oldEvent.GetAddMethod(), out addMethod))
						continue;

					if (!methodDictionary.TryGetValue(oldEvent.GetRemoveMethod(), out removeMethod))
						continue;

					string name = oldEvent.Name;
					Type eventType = oldEvent.EventHandlerType;

					var newEvent =
						type.DefineEvent
						(
							name,
							EventAttributes.None,
							eventType
						);

					newEvent.SetAddOnMethod(addMethod);
					newEvent.SetRemoveOnMethod(removeMethod);
				}
			}
		#endregion
		#region _GetProperty
			private static PropertyInfo _GetProperty(Type interfaceType, string name)
			{
				name = name.Substring(4);

				var result = interfaceType.GetProperty(name);
				if (result == null)
					throw new ArgumentException("Can't find property " + name + " in type " + interfaceType.FullName + ".");

				return result;
			}
		#endregion
		#region _EmitCast
			private static readonly MethodInfo _methodStructuralCast = ReflectionHelper.GetMethod(() => StructuralCaster.Cast<IDisposable>(null, null)).GetGenericMethodDefinition();
			private static readonly MethodInfo _methodDuckCast = ReflectionHelper.GetMethod(() => DuckCaster.Cast<IDisposable>(null, null)).GetGenericMethodDefinition();
			private static readonly FieldInfo _fieldSecurityToken = ReflectionHelper<BaseDuckForInstances>.GetField((obj) => obj._securityToken);
			private static void _EmitCast(ILGenerator generator, CastAttribute castAttribute, Type castType)
			{
				if (castAttribute.MustUseSecurityToken)
				{
					generator.EmitLoadArgument(0);
					generator.EmitLoadField(_fieldSecurityToken);
				}
				else
					generator.EmitLoadNull();

				MethodInfo method;
				if (castAttribute.MustValidateStructure)
					method = _methodStructuralCast;
				else
					method = _methodDuckCast;

				method = method.MakeGenericMethod(castType);

				generator.Emit(OpCodes.Call, method);
			}
		#endregion
		#region _EmitBoxOrCast
			private static readonly MethodInfo _methodToString = ReflectionHelper<object>.GetMethod((obj) => obj.ToString());
			private static readonly MethodInfo _methodEnumParserParse = ReflectionHelper.GetMethod(() => EnumParser.Parse<GCCollectionMode>(null)).GetGenericMethodDefinition();
			private static void _EmitBoxOrCast(ILGenerator generator, CastAttribute castAttribute, Type oldParameterType, Type newParameterType)
			{
				if (oldParameterType == newParameterType)
					return;

				if (oldParameterType.IsGenericParameter)
				{
					generator.Emit(OpCodes.Box, oldParameterType);

					if (newParameterType == typeof(object))
						return;

					oldParameterType = typeof(object);
				}

				if (oldParameterType.IsValueType == newParameterType.IsValueType)
				{
					if (!newParameterType.IsAssignableFrom(oldParameterType))
					{
						if (castAttribute != null)
							_EmitCast(generator, castAttribute, newParameterType);
						else
						if (newParameterType.IsGenericParameter)
							generator.Emit(OpCodes.Unbox_Any, newParameterType);
						else
							generator.Emit(OpCodes.Castclass, newParameterType);
					}
				}
				else
				{
					if (newParameterType.IsValueType)
					{
						if (oldParameterType == typeof(string) && newParameterType.IsEnum)
							generator.Emit(OpCodes.Call, _methodEnumParserParse.MakeGenericMethod(newParameterType));
						else
							generator.Emit(OpCodes.Unbox_Any, newParameterType);
					}
					else
					{
						if (!newParameterType.IsGenericParameter || castAttribute != null)
							generator.Emit(OpCodes.Box, oldParameterType);

						if (castAttribute != null)
							_EmitCast(generator, castAttribute, newParameterType);
						else
						if (newParameterType == typeof(string) && oldParameterType.IsEnum)
							generator.Emit(OpCodes.Callvirt, _methodToString);
					}
				}
			}
		#endregion
		#region _EmitNotSupported
			private static void _EmitNotSupported(ILGenerator generator)
			{
				generator.EmitNewObject(_constructorNotSupportedException);
				generator.Emit(OpCodes.Throw);
			}
		#endregion
		#region _SimpleRedirect
			private static void _SimpleRedirect(TypeBuilder type, Type interfaceType, Type toType, HashSet<string> usedNames, Dictionary<MethodInfo, MethodBuilder> methodDictionary)
			{
				foreach(var interfaceMethod in interfaceType.GetMethods())
				{
					string name = interfaceMethod.Name;
					Type returnType = interfaceMethod.ReturnType;
					Type[] parameterTypes = interfaceMethod.GetParameterTypes();

					MethodInfo toMethod = null;

					if (!type.IsGenericType && !interfaceMethod.IsGenericMethod)
					{
						string toMethodName = string.Concat(type.FullName, '.', name);

						toMethod = 
							toType.GetMethod
							(
								toMethodName,
								BindingFlags.NonPublic | BindingFlags.Instance,
								null,
								parameterTypes,
								null
							);
					}

					if (toMethod == null)
					{
						toMethod = 
							toType.GetMethod
							(
								name,
								parameterTypes
							);
					}

					var newMethod = _DefineNewMethod(type, usedNames, interfaceMethod, returnType, parameterTypes, methodDictionary);

					if (interfaceMethod.IsGenericMethodDefinition)
						_DefineGenericParameters(interfaceMethod, newMethod);

					var generator = newMethod.GetILGenerator();

					generator.EmitLoadArgument(0);
					generator.EmitLoadField(_fieldBaseStructuralForInstances);

					for(int i=1; i<=parameterTypes.Length; i++)
						generator.EmitLoadArgument(i);

					if (toMethod != null)
					{
						generator.Emit(OpCodes.Call, toMethod);
						_EmitBoxOrCast(generator, null, toMethod.ReturnType, interfaceMethod.ReturnType);
					}
					else
						generator.Emit(OpCodes.Callvirt, interfaceMethod);

					generator.EmitReturn();

					type.DefineMethodOverride(newMethod, interfaceMethod);
				}
			}
		#endregion
		#region _DefineGenericParameters
			private static void _DefineGenericParameters(MethodInfo interfaceMethod, MethodBuilder newMethod)
			{
				var arguments = interfaceMethod.GetGenericArguments();
				string[] names = new string[arguments.Length];

				for (int i = 0; i < arguments.Length; i++)
					names[i] = arguments[i].Name;

				newMethod.DefineGenericParameters(names);
			}
		#endregion
		#region _MakeGenericMethod
			internal static MethodInfo _MakeGenericMethod(MethodInfo fromMethodInfo, MethodInfo toMethodInfo)
			{
				Dictionary<Type, Type> dictionary = new Dictionary<Type, Type>();

				foreach(var genericArgument in toMethodInfo.GetGenericArguments())
					dictionary.Add(genericArgument, null);

				var fromParameters = fromMethodInfo.GetParameters();
				int index = -1;
				foreach(var toParameterInfo in toMethodInfo.GetParameters())
				{
					index++;
					var toParameterType = toParameterInfo.ParameterType;
					if (toParameterType.IsByRef)
						toParameterType = toParameterType.GetElementType();

					if (!toParameterType.IsGenericParameter)
						continue;

					Type fromParameterType = dictionary[toParameterType];

					if (fromParameterType == null)
					{
						fromParameterType = fromParameters[index].ParameterType;

						if (fromParameterType.IsByRef)
							fromParameterType = fromParameterType.GetElementType();

						dictionary[toParameterType] = fromParameterType;
					}
					else
					{
						if (fromParameterType != fromParameters[index].ParameterType)
							return null;
					}
				}

				if (toMethodInfo.ReturnType.IsGenericParameter)
				{
					var expectedResultType = dictionary[toMethodInfo.ReturnType];

					if (expectedResultType == null)
						dictionary[toMethodInfo.ReturnType] = fromMethodInfo.ReturnType;
					else
					if (expectedResultType != fromMethodInfo.ReturnType)
						return null;
				}

				foreach(var value in dictionary.Values)
					if (value == null)
						return null;

				return toMethodInfo.MakeGenericMethod(dictionary.Values.ToArray());
			}
		#endregion
	}
}
