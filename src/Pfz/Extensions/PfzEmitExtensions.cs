using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds some methods to emit code more safely.
	/// </summary>
	public static class PfzEmitExtensions
	{
		private static readonly MethodInfo _getTypeFromHandle = ReflectionHelper.GetMethod(() => Type.GetTypeFromHandle(new RuntimeTypeHandle()));
		private static readonly MethodInfo _getFieldFromHandle = ReflectionHelper.GetMethod(() => FieldInfo.GetFieldFromHandle(new RuntimeFieldHandle()));
		private static readonly MethodInfo _getProperty = ReflectionHelper.GetMethod(() => typeof(string).GetProperty("Length"));

		/// <summary>
		/// Emits a Ldnull.
		/// </summary>
		public static void EmitLoadNull(this ILGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ldnull);
		}

		/// <summary>
		/// Emits a Ldloc, receiving a localBuilder.
		/// </summary>
		public static void EmitLoadLocal(this ILGenerator generator, LocalBuilder localBuilder)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ldloc, localBuilder);
		}

		/// <summary>
		/// Emits a Stloc, receiving a localBuilder.
		/// </summary>
		public static void EmitStoreLocal(this ILGenerator generator, LocalBuilder localBuilder)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Stloc, localBuilder);
		}

		/// <summary>
		/// Emits a Ldc_I4, receiving the value as parameter.
		/// </summary>
		public static void EmitLoadInt32(this ILGenerator generator, int value)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ldc_I4, value);
		}

		/// <summary>
		/// Emits Ldarg, receiving the index.
		/// </summary>
		public static void EmitLoadArgument(this ILGenerator generator, int argumentIndex)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ldarg, argumentIndex);
		}

		/// <summary>
		/// Emits a Newobj. You must give a valid constructor.
		/// </summary>
		public static void EmitNewObject(this ILGenerator generator, ConstructorInfo constructor)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Newobj, constructor);
		}

		/// <summary>
		/// Emits a Newarr. You must supply the elementType.
		/// So, for int[], pass the typeof(int).
		/// </summary>
		public static void EmitNewArray(this ILGenerator generator, Type elementType, int count)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.EmitLoadInt32(count);
			generator.Emit(OpCodes.Newarr, elementType);
		}

		/// <summary>
		/// Emits a Ldfld, and receives the FieldInfo of the field to load.
		/// </summary>
		public static void EmitLoadField(this ILGenerator generator, FieldInfo field)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ldfld, field);
		}

		/// <summary>
		/// Emits a Ldsfld, and receives the FieldInfo of the static field to load.
		/// </summary>
		public static void EmitLoadStaticField(this ILGenerator generator, FieldInfo field)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ldsfld, field);
		}

		/// <summary>
		/// Emits Stfld, and received the FieldInfo of the field to store.
		/// </summary>
		public static void EmitStoreField(this ILGenerator generator, FieldInfo field)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Stfld, field);
		}

		/// <summary>
		/// Emits Stfld, and received the FieldInfo of the static field to store.
		/// </summary>
		public static void EmitStoreStaticField(this ILGenerator generator, FieldInfo field)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Stsfld, field);
		}

		/// <summary>
		/// Emits Ret.
		/// </summary>
		public static void EmitReturn(this ILGenerator generator)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			generator.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits Ldstr. You must provide a valid string to it.
		/// Null is replaces by LoadNull.
		/// </summary>
		public static void EmitLoadString(this ILGenerator generator, string value)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			if (value == null)
				generator.EmitLoadNull();
			else
				generator.Emit(OpCodes.Ldstr, value);
		}

		/// <summary>
		/// Loads a field token and calls GetFieldFromHandle.
		/// </summary>
		public static void FullLoadToken(this ILGenerator generator, FieldInfo fieldInfo)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			if (fieldInfo == null)
			{
				generator.Emit(OpCodes.Ldnull);
				return;
			}

			generator.Emit(OpCodes.Ldtoken, fieldInfo);
			generator.Emit(OpCodes.Call, _getFieldFromHandle);
		}

		/// <summary>
		/// Loads a Type token and calls GetTypeFromHandle.
		/// </summary>
		public static void FullLoadToken(this ILGenerator generator, Type type)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			if (type == null)
			{
				generator.Emit(OpCodes.Ldnull);
				return;
			}

			generator.Emit(OpCodes.Ldtoken, type);
			generator.Emit(OpCodes.Call, _getTypeFromHandle);
		}

		/// <summary>
		/// Pushes the PropertyInfo to the stack.
		/// </summary>
		public static void FullLoadToken(this ILGenerator generator, PropertyInfo property)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			if (property == null)
			{
				generator.Emit(OpCodes.Ldnull);
				return;
			}

			generator.FullLoadToken(property.DeclaringType);
			generator.EmitLoadString(property.Name);
			generator.Emit(OpCodes.Callvirt, _getProperty);
		}
	}
}
