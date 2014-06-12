using System;

namespace Pfz.Remoting
{
	/// <summary>
	/// Arguments used by RemotingClientParameters.Connected event.
	/// </summary>
	[Serializable]
	public class ConnectedEventArgs:
		EventArgs
	{
		/// <summary>
		/// Creates a new instance of ConnectedEventArgs.
		/// </summary>
		public ConnectedEventArgs(bool isReconnection)
		{
			IsReconnection = isReconnection;
		}

		/// <summary>
		/// Gets a value indicating if this is the first connection (false), or a reconnection because
		/// of a connection lost (true).
		/// </summary>
		public bool IsReconnection { get; private set; }
	}
}
