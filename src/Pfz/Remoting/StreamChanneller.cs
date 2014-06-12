using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Pfz.Caching;
using Pfz.Extensions;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Serialization;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// Class responsible for creating many channels inside another stream.
	/// This is used by the remoting framework, so each thread has it's own
	/// channel inside a single tcp/ip connection.
	/// </summary>
	public sealed class StreamChanneller:
		ThreadSafeExceptionAwareDisposable,
		IChanneller
	{
		#region Private and internal fields
			private Stream _stream;
			
			private object _channelsLock = new object();
			private volatile Dictionary<int, Channel> _channels = new Dictionary<int, Channel>();
			
			private object _awaitingChannelsLock = new object();
			private volatile Dictionary<int, ManualResetEvent> _awaitingChannels = new Dictionary<int, ManualResetEvent>();
			
			private Channel _mainChannel;
			
			internal int _channelBufferSize;
			internal Queue<byte[]> _buffersToSend = new Queue<byte[]>();
			
			private int _nextChannelId;
			
			private bool _canThrow;
			
			internal ActionRunner<KeyValuePair<int, int>> _runRemoveChannel = new ActionRunner<KeyValuePair<int, int>>();
		#endregion
		
		#region Constructors
			/// <summary>
			/// Creates the channeller for the specified stream.
			/// </summary>
			/// <param name="stream">The stream to channel.</param>
			/// <param name="remoteChannelCreated">
			/// Handler to invoke when a channel is created as a request from the other side.
			/// </param>
			public StreamChanneller(Stream stream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated):
				this(stream, remoteChannelCreated, 8 * 1024)
			{
			}
			
			/// <summary>
			/// Creates the channeller for the specified stream and allows you to
			/// specify the buffer size. For tcp/ip stream, use the bigger value
			/// between receive an send buffer size.
			/// </summary>
			/// <param name="stream">The stream to channel.</param>
			/// <param name="remoteChannelCreated">
			/// Handler to invoke when a channel is created as a request from the other side.
			/// </param>
			/// <param name="bufferSizePerChannel">The buffer size used when receiving and sending to each channel.</param>
			public StreamChanneller(Stream stream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated, int bufferSizePerChannel):
				this(stream, remoteChannelCreated, bufferSizePerChannel, true)
			{
			}
			
			/// <summary>
			/// Creates the channeller for the specified stream and allows you to
			/// specify the buffer size. For tcp/ip stream, use the bigger value
			/// between receive an send buffer size.
			/// </summary>
			/// <param name="stream">The stream to channel.</param>
			/// <param name="remoteChannelCreated">
			/// Handler to invoke when a channel is created as a request from the other side.
			/// It is invoked in a separate exclusive thread. You don't need to create one.
			/// </param>
			/// <param name="canThrow">
			/// If true (the default value) can throw exception while reading.
			/// If false, only disposes the object but does not throw an exception.
			/// </param>
			/// <param name="bufferSizePerChannel">The buffer size used when receiving and sending to each channel.</param>
			/// <param name="disposed">The handler to the disposed event to be immediatelly set.</param>
			public StreamChanneller(Stream stream, EventHandler<ChannelCreatedEventArgs> remoteChannelCreated, int bufferSizePerChannel, bool canThrow, EventHandler disposed = null)
			{
				if (stream == null)
					throw new ArgumentNullException("stream");
					
				if (remoteChannelCreated == null)
					throw new ArgumentNullException("remoteChannelCreated");
					
				if (bufferSizePerChannel < 256)
					throw new ArgumentException("bufferSizePerChannel can't be less than 256 bytes", "bufferSizePerChannel");

				Disposed = disposed;
					
				_canThrow = canThrow;
					
				_channelBufferSize = bufferSizePerChannel;
				RemoteChannelCreated = remoteChannelCreated;
					
				Channel mainChannel = new Channel(this);
				_mainChannel = mainChannel;
				_channels.Add(0, mainChannel);
					
				_stream = stream;
				Thread threadReader = new Thread(_Reader);
				threadReader.IsBackground = true;
				threadReader.Name = "StreamChanneller reader.";
				threadReader.Start();
				
				Thread threadWriter = new Thread(_Writer);
				threadWriter.IsBackground = true;
				threadWriter.Name = "StreamChanneller writer.";
				threadWriter.Start();
				
				Thread threadMainChannel = new Thread(_MainChannel);
				threadMainChannel.IsBackground = true;
				threadMainChannel.Name = "StreamChanneller main channel.";
				threadMainChannel.Start(mainChannel);
				
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Disposes the channeller and the stream.
			/// </summary>
			/// <param name="disposing">true if called from Dispose() and false if called from destructor.</param>
			[SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_writerEvent")]
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					GCUtils.Collected -= _Collected;
						
					var stream = _stream;
					if (stream != null)
					{
						_stream = null;
						stream.Dispose();
					}
					
					var runRemoveChannel = _runRemoveChannel;
					if (runRemoveChannel != null)
					{
						_runRemoveChannel = null;
						runRemoveChannel.Dispose();
					}
					
					var channels = _channels;
					if (channels != null)
					{
						_channels = null;
						
						channels.Lock
						(
							delegate
							{
								foreach(Channel channel in channels.Values)
									channel.Dispose(DisposeException);
							}
						);
					}
					
					Dictionary<int, ManualResetEvent> awaitingChannels = null;
					_awaitingChannelsLock.UnabortableLock
					(
						delegate
						{
							awaitingChannels = _awaitingChannels;
							_awaitingChannels = null;
						}
					);
					
					if (awaitingChannels != null)
						foreach(ManualResetEvent mre in awaitingChannels.Values)
							mre.Set();
					
					var writerEvent = _writerEvent;
					if (writerEvent != null)
					{
						try
						{
							writerEvent.Set();
						}
						catch
						{
						}
					}
				}
					
 				base.Dispose(disposing);
	 			
 				if (disposing)
 					if (Disposed != null)
 						Disposed(this, EventArgs.Empty);
			}
		#endregion
		#region _Collected
			private void _Collected()
			{
				try
				{
					if (WasDisposed)
					{
						GCUtils.Collected -= _Collected;
						return;
					}

					_CollectAwaitingChannels();
					_CollectChannels();
					_CollectBuffersToSend();
				}
				catch
				{
					// ignore any exceptions, as the lists are kept intact if there
					// is no memory.
				}
			}

			private void _CollectAwaitingChannels()
			{
				_awaitingChannelsLock.Lock
				(
					() => 
					{
						var awaitingChannels = _awaitingChannels;
						if (awaitingChannels != null)
							_awaitingChannels = new Dictionary<int, ManualResetEvent>(awaitingChannels);
					}
				);
			}
			private void _CollectChannels()
			{
				_channelsLock.Lock
				(
					() => _channels = new Dictionary<int, Channel>(_channels)
				);
			}
			private void _CollectBuffersToSend()
			{
				var buffersToSend = _buffersToSend;
				buffersToSend.UnabortableLock
				(
					() => buffersToSend.TrimExcess()
				);
			}
		#endregion
		
		#region Methods
			#region CreateChannel
				/// <summary>
				/// Creates a channel, sending the serializableData parameter to the
				/// other side, so it can decide what to do with this channel before it
				/// gets used (this avoids an extra tcp/ip packet for small information).
				/// </summary>
				/// <param name="serializableData">Data to send to the other side.</param>
				/// <returns>A new channel.</returns>
				public Channel CreateChannel(object serializableData = null)
				{
					try
					{
						int channelId = Interlocked.Increment(ref _nextChannelId);
						Channel channel = new Channel(this);
						channel._id = channelId;
						
						_channelsLock.Lock
						(
							() => _channels.Add(channelId, channel)
						);
						
						ChannelCreated channelCreated = new ChannelCreated();
						channelCreated.SenderChannelId = channelId;
						channelCreated.Data = serializableData;

						ManualResetEvent manualResetEvent = null;
						try
						{
							AbortSafe.Run(()=> manualResetEvent = new ManualResetEvent(false));

							_awaitingChannelsLock.Lock
							(
								() => 
								{
									CheckUndisposed();
									_awaitingChannels.Add(channelId, manualResetEvent);
								}
							);
								
							try
							{
								var serializer = _CreateSerializer();
								var mainChannel = _mainChannel;
								mainChannel.Lock
								(
									() =>
									{
										serializer.Serialize(mainChannel, channelCreated);
										mainChannel.Flush();
									}
								);
								
								manualResetEvent.WaitOne();
								
								CheckUndisposed();
							}
							finally
							{
								_awaitingChannelsLock.Lock
								(
									() => 
									{
										var awaitingChannels = _awaitingChannels;
										if (awaitingChannels != null)
											awaitingChannels.Remove(channelId);
									}
								);
							}
						}
						finally
						{
							manualResetEvent.CheckedDispose();
						}
						
						return channel;
					}
					catch(Exception exception)
					{
						if (!WasDisposed)
							Dispose(exception);
							
						throw;
					}
				}
			#endregion
			#region _RemoveChannel
				internal void _RemoveChannel(KeyValuePair<int, int> pair)
				{
					try
					{
						int id = pair.Key;
						int remoteId = pair.Value;

						bool mustReturn = true;

						_channelsLock.Lock
						(
							delegate
							{
								var channels = _channels;
								
								if (channels == null)
									return;
									
								channels.Remove(id);
								mustReturn = false;
							}
						);
						
						if (mustReturn)
							return;
						
						BinarySerializer serializer = _CreateSerializer();
						ChannelRemoved channelRemoved = new ChannelRemoved();
						channelRemoved.ReceiverChannelId = remoteId;
						
						var mainChannel = _mainChannel;
						mainChannel.Lock
						(
							() =>
							{
								serializer.Serialize(mainChannel, channelRemoved);
								mainChannel.Flush();
							}
						);
					}
					catch
					{
					}
				}
			#endregion
		
			#region _Reader
				private void _Reader()
				{
					try
					{
						byte[] headerBuffer = new byte[8];
						while(true)
						{
							_SetReadTimeOut(Timeout.Infinite);
							_Read(headerBuffer, 8);
							
							int channelId = BitConverter.ToInt32(headerBuffer, 0);
							int messageSize = BitConverter.ToInt32(headerBuffer, 4);
							
							Channel channel = null;
							_channelsLock.Lock
							(
								() => _channels.TryGetValue(channelId, out channel)
							);
								
							_SetReadTimeOut(60000);
								
							if (channel == null)
							{
								_Discard(messageSize);
								continue;
							}

							int bytesLeft = messageSize;
							while (bytesLeft > 0)
							{
								if (WasDisposed)
									break;
							
								int count = bytesLeft;
								if (bytesLeft > _channelBufferSize)
									count = _channelBufferSize;
								
								byte[] messageBuffer;
								try
								{
									messageBuffer = new byte[count];
								}
								catch(Exception exception)
								{
									channel.Dispose(exception);
									channel._inMessages = null;
									
									continue;
								}
								
								_Read(messageBuffer, count);
								bytesLeft -= count;
								
								var channelMessages = channel._inMessages;
								channelMessages.Lock
								(
									delegate
									{
										try
										{
											channelMessages.Enqueue(messageBuffer);
										}
										catch(Exception exception)
										{
											channel.Dispose(exception);
											channel._inMessages = null;
											
											return;
										}

										var waitEvent = channel._waitEvent;
										if (waitEvent != null)
											waitEvent.Set();
									}
								);
							}
						}
					}
					catch(Exception exception)
					{
						if (!WasDisposed)
							Dispose(exception);
					}
				}
			#endregion
			#region _Read
				private void _Read(byte[] buffer, int count)
				{
					int totalRead = 0;
					while(totalRead < count)
					{
						int read = _stream.Read(buffer, totalRead, count-totalRead);
						
						if (read == 0)
						{
							var exception = new RemotingException("Stream closed.");
							Dispose(exception);
							throw exception;
						}
						
						totalRead += read;
					}
				}
			#endregion
			#region _Discard
				private void _Discard(int bytesToDiscard)
				{
					int bufferSize = Math.Min(bytesToDiscard, _channelBufferSize);
					byte[] discardBuffer = new byte[bufferSize];

					int bytesLeft = bytesToDiscard;
					while(bytesLeft > 0)
					{
						if (bytesLeft < bufferSize)
						{
							_Read(discardBuffer, bytesLeft);
							break;
						}
						
						_Read(discardBuffer, bufferSize);
						bytesLeft -= bufferSize;
					}
				}
			#endregion

			#region _Writer
				internal ManualResetEvent _writerEvent = new ManualResetEvent(false);
				
				private void _Writer()
				{
					var writerEvent = _writerEvent;
					try
					{
						try
						{
							var buffersToSend = _buffersToSend;
							while(true)
							{
								_SetWriteTimeOut(Timeout.Infinite);
								writerEvent.WaitOne();
								
								if (WasDisposed)
								{
									_writerEvent = null;
									return;
								}
								writerEvent.Reset();
								
								_SetWriteTimeOut(60000);
								
								while(true)
								{
									bool mustBreak = false;
									bool mustReturn = false;

									byte[] buffer = null;
									buffersToSend.UnabortableLock
									(
										delegate
										{
											if (buffersToSend.Count == 0)
											{
												mustBreak = true;
												return;
											}

											if (WasDisposed)
											{
												mustReturn = true;
												return;
											}

											buffer = buffersToSend.Dequeue();
										}
									);

									if (mustBreak)
										break;
									
									if (mustReturn)
										return;
									
									_stream.Write(buffer, 0, buffer.Length);
								}
								
								_stream.Flush();
							}
						}
						catch(Exception exception)
						{
							if (!WasDisposed)
							{
								Dispose(exception);
								
								if (_canThrow)
									throw;
							}
						}
					}
					finally
					{
						_writerEvent = null;
						writerEvent.Close();
					}
				}
			#endregion
			
			#region _MainChannel
				private void _MainChannel(object mainChannelAsObject)
				{
					Channel mainChannel = (Channel)mainChannelAsObject;
					try
					{
						BinarySerializer serializer = _CreateSerializer();
						while(true)
						{
							object obj = serializer.Deserialize(mainChannel);
							
							ChannelCreated channelCreated = obj as ChannelCreated;
							if (channelCreated != null)
							{
								int localChannelId = Interlocked.Increment(ref _nextChannelId);
								
								Channel channel = new Channel(this);
								channel._id = localChannelId;
								channel._remoteId = channelCreated.SenderChannelId;
								_channelsLock.Lock
								(
									() => _channels.Add(localChannelId, channel)
								);
								
								ChannelAssociated associated = new ChannelAssociated();
								associated.SenderChannelId = localChannelId;
								associated.ReceiverChannelId = channelCreated.SenderChannelId;
								
								mainChannel.Lock
								(
									() =>
									{
										serializer.Serialize(mainChannel, associated);
										mainChannel.Flush();
									}
								);
								
								ChannelCreatedEventArgs args = new ChannelCreatedEventArgs();
								args.Channel = channel;
								args.Data = channelCreated.Data;
								
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
							else
							{
								ChannelRemoved channelRemoved = obj as ChannelRemoved;
								if (channelRemoved != null)
								{
									Channel channel = null;
									_channelsLock.Lock
									(
										() => _channels.TryGetValue(channelRemoved.ReceiverChannelId, out channel)
									);
										
									if (channel != null)
										channel._BeginDispose();
								}
								else
								{
									ChannelAssociated channelAssociated = (ChannelAssociated)obj;
									
									Channel channel = null;
									_channelsLock.Lock
									(
										() => channel = _channels[channelAssociated.ReceiverChannelId]
									);
									
									channel._remoteId = channelAssociated.SenderChannelId;
									
									_awaitingChannelsLock.Lock
									(
										() =>
										{
											CheckUndisposed();
											_awaitingChannels[channel._id].Set();
										}
									);
								}
							}
						}
					}
					catch(Exception exception)
					{
						if (!WasDisposed)
						{
							Dispose(exception);
							
							if (_canThrow)
								throw;
						}
					}
				}
			#endregion
			
			#region _CreateSerializer
				private static BinarySerializer _CreateSerializer()
				{
					BinarySerializer serializer = new BinarySerializer();
					serializer.AddDefaultType(typeof(ChannelCreated));
					serializer.AddDefaultType(typeof(ChannelAssociated));
					serializer.AddDefaultType(typeof(ChannelRemoved));
					return serializer;
				}
			#endregion
			
			#region _SetReadTimeOut
				private void _SetReadTimeOut(int timeout)
				{
					if (_stream.CanTimeout)
						_stream.ReadTimeout = timeout;
				}
			#endregion
			#region _SetWriteTimeOut
				private void _SetWriteTimeOut(int timeout)
				{
					if (_stream.CanTimeout)
						_stream.WriteTimeout = timeout;
				}
			#endregion
		#endregion
		#region Events
			/// <summary>
			/// Event called when Dispose() has just finished.
			/// </summary>
			public event EventHandler Disposed;
			
			/// <summary>
			/// Event that is invoked when the remote side creates a new channel.
			/// </summary>
			public event EventHandler<ChannelCreatedEventArgs> RemoteChannelCreated;
		#endregion
		
		#region Nested classes
			[Serializable]
			private sealed class ChannelCreated
			{
				internal int SenderChannelId;
				internal object Data;
			}
			
			[Serializable]
			private sealed class ChannelRemoved
			{
				internal int ReceiverChannelId;
			}
			
			[Serializable]
			private sealed class ChannelAssociated
			{
				internal int ReceiverChannelId;
				internal int SenderChannelId;
			}
		#endregion

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
