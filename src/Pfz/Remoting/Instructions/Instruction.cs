using System;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal abstract class Instruction
	{
		public abstract void Run(RemotingClient client, ThreadData threadData);
		public virtual object ReRun(RemotingClient client, object target)
		{
			throw new NotSupportedException();
		}
	}
}
