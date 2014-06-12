using System;
using System.IO;
using System.Net.Sockets;
using Pfz.DataTypes;

namespace Pfz.Remoting
{
	/// <summary>
	/// Wraps a TcpListener class so it can be viewed as an IListener interface.
	/// </summary>
	public sealed class TcpListenerWrapper:
		IListener
	{
		private TcpListener _listener;
		
		/// <summary>
		/// Creates a new wrapper over the given TcpListener.
		/// </summary>
		public TcpListenerWrapper(TcpListener listener, bool canThrow)
		{
			if (listener == null)
				throw new ArgumentNullException("listener");
		
			_listener = listener;
			CanThrow = canThrow;
		}
		
		/// <summary>
		/// Stops the listener.
		/// </summary>
		public void Dispose()
		{
			_listener.Stop();
		}
		
		/// <summary>
		/// Used as the CanThrow parameter of the stream channeller.
		/// </summary>
		public bool CanThrow { get; private set; }

		/// <summary>
		/// Accepts a new socket.
		/// </summary>
		public Stream Accept(Box<object> connectionBox)
		{
			var connection = _listener.AcceptTcpClient();
			connection.NoDelay = true;

			if (connectionBox != null)
				connectionBox.Value = connection;

			return connection.GetStream();
		}
		

		/// <summary>
		/// Starts listening.
		/// </summary>
		public void Start()
		{
			_listener.Start();
		}
		
		/// <summary>
		/// Creates a stream channeler.
		/// </summary>
		public IChanneller CreateChanneller(Stream stream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated)
		{
			return new StreamChanneller(stream, remoteChannelCreated, 8 * 1024, CanThrow);
		}
	}
}
