using System;
using System.Collections.Generic;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionInformReferencesNotFound:
		Instruction
	{
		public InstructionInformReferencesNotFound(HashSet<long> ids)
		{
			Ids = new long[ids.Count];
			ids.CopyTo(Ids);
		}

		public long[] Ids;

		public override void Run(RemotingClient client, ThreadData threadData)
		{
			var instruction = new InstructionRemoveReferencesNotFound();
			instruction.ReferencedIds = Ids;
			threadData.Serialize(false, instruction);
		}
	}
}
