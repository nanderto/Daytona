using System;
using System.Reflection;
using Pfz.Extensions;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionInvokeStaticMethod:
		Instruction
	{
		public string MethodName;
		public object[] Parameters;

		public override void Run(RemotingClient client, ThreadData threadData)
		{
			bool canReconnect = false;

			MethodInfo methodInfo;
			if (client._parameters._registeredStaticMethods.TryGetValue(MethodName, out methodInfo))
				canReconnect = methodInfo.ContainsCustomAttribute<ReconnectableAttribute>();

			threadData._Action
			(
				canReconnect,
				() =>
				{
					if (methodInfo == null)
						throw new RemotingException("Can't invoke static method " + MethodName + ".");

					var parameters = Parameters;
					return methodInfo.Invoke(null, parameters);
				}
			);
		}
		public override object ReRun(RemotingClient client, object target)
		{
			MethodInfo methodInfo = client._parameters._registeredStaticMethods[MethodName];
			return methodInfo.Invoke(null, Parameters);
		}
	}
}
