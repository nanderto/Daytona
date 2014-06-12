using System;
using System.Reflection;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal abstract class InstructionEvent:
		Instruction
	{
		public long ObjectId;
		public EventInfo EventInfo;
		public Delegate Handler;
	}
}
