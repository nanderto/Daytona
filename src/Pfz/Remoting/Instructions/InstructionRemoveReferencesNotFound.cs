using System;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionRemoveReferencesNotFound:
		Instruction
	{
		public long[] ReferencedIds;

		public override void Run(RemotingClient client, ThreadData threadData)
		{
			var objectsUsedByTheOtherSide = client._objectsUsedByTheOtherSide;

			objectsUsedByTheOtherSide.InvalidateIds(ReferencedIds);

			threadData.SerializeLastData();
		}
	}
}
