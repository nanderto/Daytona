using System;
using System.Collections.Generic;
using System.IO;
using Pfz.Caching;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Serialization;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// Channeller that uses NamedPipes to create new channels.
	/// </summary>
	public sealed class NamedPipeChanneller:
		ThreadSafeExceptionAwareDisposable,
		IChanneller
	{
		private object _streamsLock = new object();
		private List<ExceptionAwareStream> _streams = new List<ExceptionAwareStream>();
		
		/// <summary>
		/// Creates a new NamedPipeChanneller using the given stream as the
		/// "listener" stream.
		/// </summary>
		public NamedPipeChanneller(Stream baseStream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated)
		{
			if (baseStream == null)
				throw new ArgumentNullException("baseStream");
				
			BaseStream = baseStream;
			GCUtils.Collected += _Collected;
			RemoteChannelCreated = remoteChannelCreated;
			UnlimitedThreadPool.Run(_Reader);
		}
		private void _Collected()
		{
			if (BaseStream == null)
			{
				GCUtils.Collected -= _Collected;
				return;
			}
			
			try
			{
				_streamsLock.Lock
				(
					delegate
					{
						var oldStreams = _streams;
						var newStreams = new List<ExceptionAwareStream>(oldStreams.Count);
						
						foreach(var stream in oldStreams)
						{
							if (!stream.WasDisposed)
								newStreams.Add(stream);
						}
						
						_streams = newStreams;
					}
				);
			}
			catch
			{
			}
		}
		
		/// <summary>
		/// Disposes the base stream.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GCUtils.Collected -= _Collected;
			
				var baseStream = BaseStream;
				if (baseStream != null)
				{
					BaseStream = null;
					
					var disposable = baseStream as IExceptionAwareDisposable;
					if (disposable != null)
						disposable.Dispose(DisposeException);
					else
						baseStream.Dispose();
				}
				
				if (_streamsLock != null)
				{
					_streamsLock.Lock
					(
						delegate
						{
							if (_streams != null)
								foreach(var stream in _streams)
									stream.Dispose();
						}
					);
				}
			}
		
			base.Dispose(disposing);

			if (disposing)
			{
				var disposed = Disposed;
				if (disposed != null)
					disposed(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Gets the base (listener) stream.
		/// </summary>
		public Stream BaseStream { get; private set; }
	
		/// <summary>
		/// Creates a new channel.
		/// </summary>
		public ExceptionAwareStream CreateChannel()
		{
			return CreateChannel(null);
		}
		
		/// <summary>
		/// Creates a new channel sending initial data to it.
		/// </summary>
		public ExceptionAwareStream CreateChannel(object createData)
		{
			string name = "Pfz.Remoting.NamedPipeChanneller.NamedPipeChannel_" + Guid.NewGuid();
			var result = DuplexStream.CreateNamedPipeServer(name, false);
			
			BaseStream.Lock
			(
				delegate
				{
					var serializer = new BinarySerializer();
					serializer.AddDefaultTypeRecursive(typeof(KeyValuePair<string, object>));
					
					serializer.Serialize(BaseStream, new KeyValuePair<string, object>(name, createData));
					BaseStream.Flush();
				}
			);
			
			result.ReadStream.WaitForConnection();
			result.WriteStream.WaitForConnection();
			
			_streamsLock.UnabortableLock
			(
				() => _streams.Add(result)
			);

			return result;
		}
		private void _Reader()
		{
			var stream = BaseStream;
			var serializer = new BinarySerializer();
			serializer.AddDefaultType(typeof(KeyValuePair<string, object>));
			try
			{
				while(true)
				{
					var deserializedData = serializer.Deserialize(BaseStream);
					var pair = (KeyValuePair<string, object>)deserializedData;
					var args = new ChannelCreatedEventArgs();
					
					var channel = DuplexStream.CreateNamedPipeClient(".", pair.Key, true);
					args.Channel = channel;
					args.Data = pair.Value;
					
					_streamsLock.UnabortableLock
					(
						() => _streams.Add(channel)
					);
					
					UnlimitedThreadPool.Run
					(
						() =>
						{
							Exception exception = null;
							try
							{
								RemoteChannelCreated(this, args);
							}
							catch(Exception caughtException)
							{
								exception = caughtException;
							}
							finally
							{
								if (args.CanDisposeChannel)
									args.Channel.Dispose(exception);
							}
						}
					);
				}
			}
			catch
			{
				if (!WasDisposed)
					throw;
			}
		}

		/// <summary>
		/// Event invoked just after this channeller is disposed.
		/// </summary>
		public event EventHandler Disposed;

		/// <summary>
		/// Event invoked when a channel is created by the remote side.
		/// </summary>
		public event EventHandler<ChannelCreatedEventArgs> RemoteChannelCreated;

		#region IChanneller Members
			ExceptionAwareStream IChanneller.CreateChannel()
			{
				return CreateChannel();
			}
			ExceptionAwareStream IChanneller.CreateChannel(object createData)
			{
				return CreateChannel(createData);
			}
		#endregion
	}
}
