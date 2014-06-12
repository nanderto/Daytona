using System;
using System.Collections.Generic;
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
	/// Class used to implement any delegate and call a proxy object to process the invokes.
	/// </summary>
	public static class DelegateProxier
	{
		#region Pair - Nested Class
			private sealed class Pair
			{
				public ConstructorInfo Constructor;
				public MethodInfo Method;
			}
		#endregion

		#region Fields
			private static readonly Dictionary<ImmutableArray<Type>, Pair> _implementations = new Dictionary<ImmutableArray<Type>, Pair>();
			private static readonly ReaderWriterLockSlim _implementationsLock = new ReaderWriterLockSlim();

			private static readonly MethodInfo _invokeMethod = typeof(IProxyDelegate).GetMethod("Invoke");
			private static readonly Type[] _proxyTypes = new Type[]{typeof(IProxyDelegate)};
		#endregion

		#region Proxy
			/// <summary>
			/// Implementes a delegate and calls the given proxy.
			/// </summary>
			public static Delegate Proxy(IProxyDelegate proxy, Type delegateType)
			{
				if (proxy == null)
					throw new ArgumentNullException("proxy");

				if (delegateType == null)
					throw new ArgumentNullException("delegateType");

				if (!delegateType.IsSubclassOf(typeof(Delegate)))
					throw new ArgumentException("delegateType must be a delegate.", "delegateType");

				var sourceInvoke = delegateType.GetMethod("Invoke");
				var returnType = sourceInvoke.ReturnType;

				List<Type> parameterTypes = new List<Type>();
				parameterTypes.Add(returnType);
				var parameters = sourceInvoke.GetParameters();
				int parameterCount = parameters.Length;
				foreach(var parameter in parameters)
					parameterTypes.Add(parameter.ParameterType);

				var immutableArray = new ImmutableArray<Type>(parameterTypes);

				Pair pair = null;
				_implementationsLock.ReadLock
				(
					() => _implementations.TryGetValue(immutableArray, out pair)
				);

				if (pair == null)
				{
					_implementationsLock.UpgradeableLock
					(
						() =>
						{
							if (_implementations.TryGetValue(immutableArray, out pair))
								return;

							var type = 
								_DynamicModule.DefineType
								(
									"Pfz.DynamicObjects.DelegateImplementations." + delegateType.Name,
									TypeAttributes.Public | TypeAttributes.Sealed,
									typeof(BaseImplementedProxy),
									Type.EmptyTypes
								);

							var constructor = 
								type.DefineConstructor
								(
									MethodAttributes.Public,
									CallingConventions.Standard,
									_proxyTypes
								);
							var constructorGenerator = constructor.GetILGenerator();
							constructorGenerator.Emit(OpCodes.Ldarg_0);
							constructorGenerator.Emit(OpCodes.Ldarg_1);
							constructorGenerator.Emit(OpCodes.Call, InterfaceProxier._baseImplementedProxyConstructorInfo);

							constructorGenerator.Emit(OpCodes.Ldarg_0);
							constructorGenerator.Emit(OpCodes.Ldarg_1);
							constructorGenerator.Emit(OpCodes.Stfld, InterfaceProxier._baseImplementedProxyFieldInfo);
							constructorGenerator.Emit(OpCodes.Ret);

							var method = 
								type.DefineMethod
								(
									"Invoke",
									MethodAttributes.Public,
									returnType,
									sourceInvoke.GetParameterTypes()
								);

							var methodGenerator = method.GetILGenerator();

							methodGenerator.Emit(OpCodes.Ldarg_0);
							methodGenerator.Emit(OpCodes.Ldfld, InterfaceProxier._baseImplementedProxyFieldInfo);
							if (parameters.Length == 0)
							{
								methodGenerator.Emit(OpCodes.Ldnull);
							}
							else
							{
								methodGenerator.DeclareLocal(typeof(object[]));
								methodGenerator.Emit(OpCodes.Ldc_I4, parameterCount);
								methodGenerator.Emit(OpCodes.Newarr, typeof(object));
								methodGenerator.Emit(OpCodes.Stloc_0);

								int parameterIndex = -1;
								foreach(var parameter in parameters)
								{
									parameterIndex++;

									methodGenerator.Emit(OpCodes.Ldloc_0);
									methodGenerator.Emit(OpCodes.Ldc_I4, parameterIndex);
									methodGenerator.Emit(OpCodes.Ldarg, parameterIndex+1);

									var parameterType = parameter.ParameterType;
									var effectiveType = parameterType;

									if (parameterType.IsByRef)
									{
										effectiveType = parameterType.GetElementType();
										methodGenerator.Emit(OpCodes.Ldobj, effectiveType);
									}

									if (parameterType.IsValueType)
										methodGenerator.Emit(OpCodes.Box, effectiveType);

									methodGenerator.Emit(OpCodes.Stelem_Ref);
								}

								methodGenerator.Emit(OpCodes.Ldloc_0);
							}

							methodGenerator.Emit(OpCodes.Callvirt, _invokeMethod);

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

								if (parameterType.IsValueType)
									methodGenerator.Emit(OpCodes.Unbox_Any, parameterType);

								methodGenerator.Emit(OpCodes.Stobj, parameterType);
							}

							if (returnType == typeof(void))
								methodGenerator.Emit(OpCodes.Pop);
							else
							if (returnType.IsValueType)
								methodGenerator.Emit(OpCodes.Unbox_Any, returnType);

							methodGenerator.Emit(OpCodes.Ret);

							var realType = type.CreateType();
							pair = new Pair();
							pair.Constructor = realType.GetConstructor(_proxyTypes);
							pair.Method = realType.GetMethod("Invoke");

							_implementationsLock.WriteLock
							(
								() => _implementations.Add(immutableArray, pair)
							);
						}
					);
				}

				var result = pair.Constructor.Invoke(new object[]{proxy});
				return Delegate.CreateDelegate(delegateType, result, pair.Method);
			}
		#endregion
	}
}
