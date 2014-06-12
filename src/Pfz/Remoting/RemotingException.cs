using System;
using System.Runtime.Serialization;

namespace Pfz.Remoting
{
	/// <summary>
	/// Exception thrown by the remoting framework when something wrong happens.
	/// </summary>
	[Serializable]
	public class RemotingException:
		Exception
	{
		/// <summary>
		/// Only following the Exception pattern.
		/// </summary>
		public RemotingException() { }

		/// <summary>
		/// Only following the Exception pattern.
		/// </summary>
		public RemotingException(string message) : base(message) { }

		/// <summary>
		/// Only following the Exception pattern.
		/// </summary>
		public RemotingException(string message, Exception inner) : base(message, inner) { }

		/// <summary>
		/// Only following the Exception pattern.
		/// </summary>
		protected RemotingException(SerializationInfo info, StreamingContext context):
			base(info, context)
		{
		}
	}
}
