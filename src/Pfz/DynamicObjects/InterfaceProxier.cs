using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Pfz.Collections;
using Pfz.DynamicObjects.Internal;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Class capable of implementing interfaces, redirecting all their methods to a proxy object.
	/// </summary>
	public static class InterfaceProxier
	{
		#region Fields
			private static readonly Type[] _proxyObjectType = new Type[]{typeof(IProxyObject)};
			internal static readonly ConstructorInfo _baseImplementedProxyConstructorInfo = typeof(BaseImplementedProxy).GetConstructor(new Type[]{typeof(object)});
			internal static readonly FieldInfo _baseImplementedProxyFieldInfo = ReflectionHelper<BaseImplementedProxy>.GetField((obj) => obj._proxyObject);

			private static readonly MethodInfo _onMethodCall_MethodInfo = ReflectionHelper<IProxyObject>.GetMethod((obj) => obj.InvokeMethod(null, null, null));
			private static readonly MethodInfo _onPropertyGet_MethodInfo = ReflectionHelper<IProxyObject>.GetMethod((obj) => obj.InvokePropertyGet(null, null));
			private static readonly MethodInfo _onPropertySet_MethodInfo = ReflectionHelper<IProxyObject>.GetMethod((obj) => obj.InvokePropertySet(null, null, null));
			private static readonly MethodInfo _onEventAdd_MethodInfo = ReflectionHelper<IProxyObject>.GetMethod((obj) => obj.InvokeEventAdd(null, null));
			private static readonly MethodInfo _onEventRemove_MethodInfo = ReflectionHelper<IProxyObject>.GetMethod((obj) => obj.InvokeEventRemove(null, null));

			private static readonly MethodInfo _getTypeFromHandle = ReflectionHelper.GetMethod(() => Type.GetTypeFromHandle(new RuntimeTypeHandle()));
			private static readonly MethodInfo _getMethodFromHandle = ReflectionHelper.GetMethod(() => MethodInfo.GetMethodFromHandle(new RuntimeMethodHandle()));
			private static readonly MethodInfo _getMethodFromHandle2 = ReflectionHelper.GetMethod(() => MethodInfo.GetMethodFromHandle(new RuntimeMethodHandle(), new RuntimeTypeHandle()));
			private static readonly MethodInfo _getProperty_MethodInfo = ReflectionHelper.GetMethod(() => typeof(ICollection<int>).GetProperty("Count"));
			private static readonly MethodInfo _getEvent_MethodInfo = ReflectionHelper.GetMethod(() => typeof(ICollection<int>).GetEvent("SomeEvent"));

			private static Dictionary<ImmutableArray<Type>, ConstructorInfo> _implementations = new Dictionary<ImmutableArray<Type>, ConstructorInfo>();
			private static ReaderWriterLockSlim _implementationsLock = new ReaderWriterLockSlim();
		#endregion

		#region Proxy
			/// <summary>
			/// Implements an object that proxies the calls to interface T to the proxyObject.
			/// </summary>
			public static T Proxy<T>(IProxyObject proxyObject)
			{
				return (T)Proxy(proxyObject, typeof(T));
			}

			/// <summary>
			/// Implements one or more interfaces, calling the given proxyObject for each call.
			/// Returns an object with all the given interfaces implemented.
			/// </summary>
			[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
			[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
			public static object Proxy(IProxyObject proxyObject, params Type[] interfaceTypes)
			{
				if (proxyObject == null)
					throw new ArgumentNullException("proxyObject");

				if (interfaceTypes == null)
					throw new ArgumentNullException("interfaceTypes");

				if (interfaceTypes.Length == 0)
					return new object();

				var interfaceTypesCopy = (Type[])interfaceTypes.Clone();
				int index = -1;
				foreach(var interfaceType in interfaceTypesCopy)
				{
					index++;

					if (interfaceType == null)
						throw new ArgumentException("interfaceTypes can't contain null values. null value at index (" + index + ").", "interfaceTypes");

					if (!interfaceType.IsInterface)
						throw new ArgumentException("interfaceTypes contains a type which is not an interface (" + interfaceType.FullName + ").", "interfaceTypes");
				}

				HashSet<Type> allInterfaces = new HashSet<Type>();
				foreach(var interfaceType in interfaceTypesCopy)
				{
					if (!interfaceType.IsPublic)
						continue;

					allInterfaces.Add(interfaceType);

					foreach(var subInterfaceType in interfaceType.GetInterfaces())
						allInterfaces.Add(subInterfaceType);
				}

				interfaceTypesCopy = allInterfaces.ToArray();
				Array.Sort
				(
					interfaceTypesCopy,
					(a, b) => a.FullName.CompareTo(b.FullName)
				);

				var immutableInterfaceTypes = new ImmutableArray<Type>(interfaceTypesCopy);

				ConstructorInfo constructorInfo = null;

				_implementationsLock.ReadLock
				(
					() => _implementations.TryGetValue(immutableInterfaceTypes, out constructorInfo)
				);

				if (constructorInfo == null)
				{
					_implementationsLock.UpgradeableLock
					(
						() =>
						{
							_implementations.TryGetValue(immutableInterfaceTypes, out constructorInfo);
							if (constructorInfo != null)
								return;

							var type = _DynamicModule.DefineType
							(
								string.Concat("Pfz.DynamicObjects.InterfaceImplementations._", immutableInterfaceTypes.Length, immutableInterfaceTypes[0].Name),
								TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class,
								typeof(BaseImplementedProxy),
								interfaceTypesCopy
							);

							var staticConstructor = 
								type.DefineConstructor
								(
									MethodAttributes.Static,
									CallingConventions.Standard,
									null
								);

							var constructor = 
								type.DefineConstructor
								(
									MethodAttributes.Public,
									CallingConventions.Standard,
									_proxyObjectType
								);

							var staticConstructorGenerator = staticConstructor.GetILGenerator();
							var constructorGenerator = constructor.GetILGenerator();

							// needed to call object constructor.
							constructorGenerator.Emit(OpCodes.Ldarg_0);
							constructorGenerator.Emit(OpCodes.Ldarg_1);
							constructorGenerator.Emit(OpCodes.Call, _baseImplementedProxyConstructorInfo);

							// Now we do fProxyObject = proxyObject
							constructorGenerator.Emit(OpCodes.Ldarg_0);
							constructorGenerator.Emit(OpCodes.Ldarg_1);
							constructorGenerator.Emit(OpCodes.Stfld, _baseImplementedProxyFieldInfo);
							constructorGenerator.Emit(OpCodes.Ret);

							foreach(var interfaceType in interfaceTypesCopy)
							{
								int methodIndex = -1;
								foreach(var methodInfo in interfaceType.GetMethods())
								{
									if (methodInfo.IsSpecialName)
										continue;

									methodIndex ++;

									var staticField = 
										type.DefineField
										(
											"_fieldForMethod" + methodIndex,
											typeof(MethodInfo),
											FieldAttributes.InitOnly | FieldAttributes.Private | FieldAttributes.Static
										);


									staticConstructorGenerator.Emit(OpCodes.Ldtoken, methodInfo);

									if (!interfaceType.IsGenericType)
										staticConstructorGenerator.Emit(OpCodes.Call, _getMethodFromHandle);
									else
									{
										staticConstructorGenerator.Emit(OpCodes.Ldtoken, interfaceType);
										staticConstructorGenerator.Emit(OpCodes.Call, _getMethodFromHandle2);
									}

									staticConstructorGenerator.Emit(OpCodes.Stsfld, staticField);

									_CreateMethod(staticField, type, "Method" + methodIndex, methodInfo, _onMethodCall_MethodInfo, true, true);
							
								}

								int propertyIndex = -1;
								foreach(var propertyInfo in interfaceType.GetProperties())
								{
									propertyIndex ++;

									var staticField = 
										type.DefineField
										(
											"_fieldForProperty" + propertyIndex,
											typeof(PropertyInfo),
											FieldAttributes.InitOnly | FieldAttributes.Private | FieldAttributes.Static
										);

									staticConstructorGenerator.Emit(OpCodes.Ldtoken, interfaceType);
									staticConstructorGenerator.Emit(OpCodes.Call, _getTypeFromHandle);
									staticConstructorGenerator.Emit(OpCodes.Ldstr, propertyInfo.Name);
									staticConstructorGenerator.Emit(OpCodes.Call, _getProperty_MethodInfo);
									staticConstructorGenerator.Emit(OpCodes.Stsfld, staticField);

									if (propertyInfo.CanRead)
									{
										var methodInfo = propertyInfo.GetGetMethod();
										_CreateMethod(staticField, type, "PropertyGet" + propertyIndex, methodInfo, _onPropertyGet_MethodInfo, false, true);
									}

									if (propertyInfo.CanWrite)
									{
										var methodInfo = propertyInfo.GetSetMethod();
										_CreateMethod(staticField, type, "PropertySet" + propertyIndex, methodInfo, _onPropertySet_MethodInfo, false, true);
									}
								}

								int eventIndex = -1;
								foreach(var eventInfo in interfaceType.GetEvents())
								{
									eventIndex ++;

									var staticField = 
										type.DefineField
										(
											"_fieldForEvent" + propertyIndex,
											typeof(EventInfo),
											FieldAttributes.InitOnly | FieldAttributes.Private | FieldAttributes.Static
										);

									staticConstructorGenerator.Emit(OpCodes.Ldtoken, interfaceType);
									staticConstructorGenerator.Emit(OpCodes.Call, _getTypeFromHandle);
									staticConstructorGenerator.Emit(OpCodes.Ldstr, eventInfo.Name);
									staticConstructorGenerator.Emit(OpCodes.Call, _getEvent_MethodInfo);
									staticConstructorGenerator.Emit(OpCodes.Stsfld, staticField);

									_CreateMethod(staticField, type, "EventAdd" + eventIndex, eventInfo.GetAddMethod(), _onEventAdd_MethodInfo, false, false);
									_CreateMethod(staticField, type, "EventRemove" + eventIndex, eventInfo.GetRemoveMethod(), _onEventRemove_MethodInfo, false, false);
								}
							}

							staticConstructorGenerator.Emit(OpCodes.Ret);

							var realType = type.CreateType();
							constructorInfo = realType.GetConstructors()[0];

							_implementationsLock.WriteLock
							(
								() => _implementations.Add(immutableInterfaceTypes, constructorInfo)
							);
						}
					);
				}

				return constructorInfo.Invoke(new object[]{proxyObject});
			}
		#endregion

		#region _CreateMethod
			private static void _CreateMethod(FieldBuilder staticField, TypeBuilder type, string methodName, MethodInfo methodInfo, MethodInfo proxyMethod, bool canHaveGenericArguments, bool createArray)
			{
				var parameters = methodInfo.GetParameters();
				var method = type.DefineMethod
					(
						methodName,
						MethodAttributes.Private | MethodAttributes.Virtual,
						methodInfo.CallingConvention,
						methodInfo.ReturnType,
						methodInfo.GetParameterTypes()
					);

				var methodGenerator = method.GetILGenerator();

				int parameterCount = parameters.Length;
				int genericIndex = 0;
				if (parameterCount > 0)
				{
					methodGenerator.DeclareLocal(typeof(object[]));
					genericIndex++;
				}

				GenericTypeParameterBuilder[] genericTypes = null;
				if (canHaveGenericArguments)
				{
					if (methodInfo.IsGenericMethod)
					{
						var genericArguments = methodInfo.GetGenericArguments();
						genericTypes = method.DefineGenericParameters(_GetNames(genericArguments));

						methodGenerator.DeclareLocal(typeof(Type[]));

						int count = genericArguments.Length;
						methodGenerator.Emit(OpCodes.Ldc_I4, count);
						methodGenerator.Emit(OpCodes.Newarr, typeof(Type));
						methodGenerator.Emit(OpCodes.Stloc, genericIndex);

						for(int i=0; i<count; i++)
						{
							methodGenerator.Emit(OpCodes.Ldloc, genericIndex);
							methodGenerator.Emit(OpCodes.Ldc_I4, i);
							methodGenerator.Emit(OpCodes.Ldtoken, genericTypes[i]);
							methodGenerator.Emit(OpCodes.Call, _getTypeFromHandle);
							methodGenerator.Emit(OpCodes.Stelem_Ref);
						}
					}
				}

				methodGenerator.Emit(OpCodes.Ldarg_0);
				methodGenerator.Emit(OpCodes.Ldfld, _baseImplementedProxyFieldInfo);

				methodGenerator.Emit(OpCodes.Ldsfld, staticField);

				if (canHaveGenericArguments)
				{
					if (genericTypes == null)
						methodGenerator.Emit(OpCodes.Ldnull);
					else
						methodGenerator.Emit(OpCodes.Ldloc, genericIndex);
				}

				if (createArray)
				{
					if (proxyMethod == _onPropertySet_MethodInfo)
						parameterCount --;

					if (parameterCount == 0)
						methodGenerator.Emit(OpCodes.Ldnull);
					else
					{
						methodGenerator.Emit(OpCodes.Ldc_I4, parameterCount);
						methodGenerator.Emit(OpCodes.Newarr, typeof(object));
						methodGenerator.Emit(OpCodes.Stloc_0);
						for (int i = 0; i < parameterCount; i++)
						{
							var parameter = parameters[i];
							if (parameter.IsOut)
								continue;

							methodGenerator.Emit(OpCodes.Ldloc_0);
							methodGenerator.Emit(OpCodes.Ldc_I4, i);
							methodGenerator.Emit(OpCodes.Ldarg, i + 1);

							var parameterType = parameter.ParameterType;
							var effectiveType = parameterType;

							if (parameterType.IsByRef)
							{
								effectiveType = parameterType.GetElementType();
								methodGenerator.Emit(OpCodes.Ldobj, effectiveType);
							}

							methodGenerator.Emit(OpCodes.Box, effectiveType);

							methodGenerator.Emit(OpCodes.Stelem_Ref);
						}

						methodGenerator.Emit(OpCodes.Ldloc_0);
					}

					if (proxyMethod == _onPropertySet_MethodInfo)
					{
						methodGenerator.Emit(OpCodes.Ldarg, parameterCount + 1);

						Type paramType = parameters[parameterCount].ParameterType;
						if (paramType.IsValueType)
							methodGenerator.Emit(OpCodes.Box, paramType);
					}
				}
				else
					methodGenerator.Emit(OpCodes.Ldarg_1);

				methodGenerator.EmitCall(OpCodes.Callvirt, proxyMethod, null);

				for (int i = 0; i < parameterCount; i++)
				{
					var parameter = parameters[i];
					var parameterType = parameter.ParameterType;

					if (!parameterType.IsByRef)
						continue;

					parameterType = parameterType.GetElementType();

					methodGenerator.Emit(OpCodes.Ldarg, i + 1);
					methodGenerator.Emit(OpCodes.Ldloc_0);
					methodGenerator.Emit(OpCodes.Ldc_I4, i);
					methodGenerator.Emit(OpCodes.Ldelem_Ref);

					methodGenerator.Emit(OpCodes.Unbox_Any, parameterType);

					methodGenerator.Emit(OpCodes.Stobj, parameterType);
				}

				var returnType = methodInfo.ReturnType;
				if (returnType == typeof(void))
				{
					if (proxyMethod.ReturnType != typeof(void))
						methodGenerator.Emit(OpCodes.Pop);
				}
				else
					methodGenerator.Emit(OpCodes.Unbox_Any, returnType);

				methodGenerator.Emit(OpCodes.Ret);

				type.DefineMethodOverride(method, methodInfo);
			}
		#endregion
		#region _GetNames
			private static string[] _GetNames(Type[] genericArguments)
			{
				int count = genericArguments.Length;
				string[] result = new string[count];
				for(int i=0; i<count; i++)
					result[i] = genericArguments[i].Name;

				return result;
			}
		#endregion
	}
}
