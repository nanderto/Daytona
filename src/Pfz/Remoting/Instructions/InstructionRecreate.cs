using System;
using System.Reflection;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionRecreate:
		Instruction
	{
		internal Instruction[] ReconnectPath;
		internal string RecreateAssembly;
		internal string RecreateTypeName;
		internal object RecreateData;

		internal static readonly Type[] _objectTypeArray = new Type[]{typeof(object)};
		public override void Run(RemotingClient client, ThreadData threadData)
		{
			threadData._Action
			(
				false,
				() =>
				{
					object result;
					if (RecreateData != null)
					{
						Assembly assembly = null;

						foreach(Assembly loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
						{
							if (loadedAssembly.FullName == RecreateAssembly)
							{
								assembly = loadedAssembly;
								break;
							}
						}

						if (assembly == null)
							return (long)-1;

						var type = assembly.GetType(RecreateTypeName, true);
						var methodInfo = 
							type.GetMethod
							(
								"Recreate", 
								BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
								null,
								_objectTypeArray,
								null
							);

						if (methodInfo == null)
							throw new RemotingException("Type " + type.FullName + " does not have a Recreate(object) static method.");

						result = methodInfo.Invoke(null, new object[]{RecreateData});
						if (result == null)
							return (long)-1;
					}
					else
					{
						object lastObject = null;
						foreach(var instruction in ReconnectPath)
						{
							lastObject = instruction.ReRun(client, lastObject);

							if (lastObject == null)
								return (long)-1;
						}

						result = lastObject;
					}

					var wrap = client._objectsUsedByTheOtherSide.GetOrWrap(result);
					return wrap.Id;
				}
			);
		}
	}
}
