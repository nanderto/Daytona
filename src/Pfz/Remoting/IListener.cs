using System;
using System.IO;
using Pfz.DataTypes;

namespace Pfz.Remoting
{
	/// <summary>
	/// Interface used by listener to accepts new connections.
	/// </summary>
	public interface IListener:
		IDisposable
	{
		/// <summary>
		/// Starts the listener.
		/// </summary>
		void Start();
	
		/// <summary>
		/// Accepts a new connection.
		/// If connectionBox is different than null, it's value will be set
		/// to the real connection that created the stream (TcpClient, for example).
		/// </summary>
		Stream Accept(Box<object> connectionBox=null);
		
		/// <summary>
		/// Creates an apropriate channeller over a stream created by this listener.
		/// </summary>
		IChanneller CreateChanneller(Stream stream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated);
	}
}
