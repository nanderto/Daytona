using System;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class RemotingResult
	{
		public object Value;
		public object[] OutValues;
		public Exception Exception;
	}
}
