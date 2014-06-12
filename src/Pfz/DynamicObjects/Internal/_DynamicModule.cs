using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;

namespace Pfz.DynamicObjects
{
	internal static class _DynamicModule
	{
		private static readonly ModuleBuilder _module;
		static _DynamicModule()
		{
			var assemblyName = new AssemblyName("Pfz.DynamicGeneratedModule.dll");
			var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			var module = dynamicAssembly.DefineDynamicModule("Pfz.DynamicGeneratedModule.dll");
			_module = module;
		}

		private static long _id;
		internal static TypeBuilder DefineType(string name, TypeAttributes typeAttributes, Type parent, Type[] interfaces)
		{
			return
				_module.DefineType
				(
					string.Concat(name, "_", Interlocked.Increment(ref _id)),
					typeAttributes,
					parent,
					interfaces
				);
		}
	}
}
