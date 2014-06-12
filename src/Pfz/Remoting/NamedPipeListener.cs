using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using Pfz.DataTypes;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// This class works as a TcpListener, but uses named pipes to work.
	/// </summary>
	public sealed class NamedPipeListener:
		ThreadSafeDisposable,
		IListener
	{
		private string _pipeName;
		private NamedPipeServerStream _stream;
		
		/// <summary>
		/// Creates a new NamedPipeListener using the specified pipeName.
		/// Do not put a server name, as this class must only be used for local
		/// connections.
		/// </summary>
		public NamedPipeListener(string pipeName)
		{
			if (pipeName == null)
				throw new ArgumentNullException("pipeName");
			
			_pipeName = pipeName + '_';
			string listenerName = _pipeName + "Listener";
			_stream = new NamedPipeServerStream(listenerName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough);
		}
		
		/// <summary>
		/// Closes the listener stream.
		/// This does not affect accepted streams.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				var stream = _stream;
				if (stream != null)
				{
					_stream = null;
					stream.Close();
				}
			}
		
			base.Dispose(disposing);
		}

		private int _count = 0;
		/// <summary>
		/// Accepts a new connection and return the stream for it.
		/// </summary>
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public DuplexStream<NamedPipeServerStream> Accept(Box<object> connectionBox)
		{
			_count++;
			
			DisposeLock.Lock
			(
				delegate
				{
					CheckUndisposed();
					
					_stream.WaitForConnection();
				}
			);
			
			DuplexStream<NamedPipeServerStream> result;
			try
			{
				string newName = _pipeName + _count;
				result = DuplexStream.CreateNamedPipeServer(newName, false);
				
				var bytes = BitConverter.GetBytes(_count);
				_stream.Write(bytes, 0, 4);
				_stream.Flush();
				_stream.WaitForPipeDrain();
			}
			finally
			{
				_stream.Disconnect();
				_stream.Dispose();
				
				string listenerName = _pipeName + "Listener";
				_stream = new NamedPipeServerStream(listenerName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough);
			}
			
			result.ReadStream.WaitForConnection();
			result.WriteStream.WaitForConnection();
			
			if (connectionBox != null)
				connectionBox.Value = result;
			
			return result;
		}

		/// <summary>
		/// Creates a NamedPipeChanneller over the given stream.
		/// </summary>
		public IChanneller CreateChanneller(Stream stream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated)
		{
			return new NamedPipeChanneller(stream, remoteChannelCreated);
		}
		
		#region IListener Members
			Stream IListener.Accept(Box<object> connectionBox)
			{
				return Accept(connectionBox);
			}
			void IListener.Start()
			{
			}
		#endregion
	}
}
