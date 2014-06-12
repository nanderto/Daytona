using System;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionObjectsCollected:
		Instruction
	{
		public long[] ObjectIds;

		public override void Run(RemotingClient client, ThreadData threadData)
		{
			client._objectsUsedByTheOtherSide.RemoveIds(ObjectIds);
		}
	}
}
