using System;
using System.Reflection;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionCreateObject:
		Instruction
	{
		public string Name;
		public object[] Parameters;

		public override void Run(RemotingClient client, ThreadData threadData)
		{
			threadData._Action
			(
				true,
				() =>
				{
					ConstructorInfo constructorInfo;
					#if DEBUG
						if (!client._parameters._registeredTypes.TryGetValue(Name, out constructorInfo))
							throw new RemotingException("Can't find a registered type named " + Name + ".");
					#else
						constructorInfo = client._parameters._registeredTypes[Name];
					#endif
					var parameters = Parameters;

					return constructorInfo.Invoke(parameters);
				}
			);
		}
		public override object ReRun(RemotingClient client, object target)
		{
			ConstructorInfo constructorInfo;
			#if DEBUG
				if (!client._parameters._registeredTypes.TryGetValue(Name, out constructorInfo))
					throw new RemotingException("Can't find a registered type named " + Name + ".");
			#else
				constructorInfo = client._parameters._registeredTypes[Name];
			#endif
			var parameters = Parameters;

			return constructorInfo.Invoke(parameters);
		}
	}
}
