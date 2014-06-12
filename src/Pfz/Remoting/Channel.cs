using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Pfz.Caching;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Remoting
{
	/// <summary>
	/// Represents a "Channel" inside a StreamChanneller. This is used by the remoting
	/// mechanism to separate each thread communication channel inside a single tcp/ip
	/// connection.
	/// </summary>
	public sealed class Channel:
		ExceptionAwareStream
	{
		#region Private and internal fields
			internal int _id;
			internal int _remoteId;
			
			internal Queue<byte[]> _inMessages = new Queue<byte[]>();
			private volatile bool _disposeBegan;
			private byte[] _actualMessage;
			private int _positionInActualMessage;
			
			internal ManualResetEvent _waitEvent = new ManualResetEvent(false);
			private byte[] _sendBuffer;
			private int _sendBufferPosition;
		#endregion
		
		#region Constructor
			internal Channel(StreamChanneller channeller)
			{
				_channeller = channeller;
				_sendBuffer = new byte[channeller._channelBufferSize];
				
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Frees all needed resources and informs the remote side.
			/// </summary>
			/// <param name="disposing">True if called from Dispose()</param>
			protected override void OnDispose(bool disposing)
			{
				GCUtils.Collected -= _Collected;

				var waitEvent = _waitEvent;
				if (waitEvent != null)
				{
					_waitEvent = null;
					waitEvent.Set();
				}
				
				var channeller = _channeller;
				if (channeller != null)
				{
					_channeller = null;
					
					var runRemoveChannel = channeller._runRemoveChannel;
					if (runRemoveChannel != null)
						runRemoveChannel.Run(channeller._RemoveChannel, new KeyValuePair<int, int>(_id, _remoteId));
				}
			
				base.OnDispose(disposing);
			}
		#endregion
		#region BeginDispose
			internal void _BeginDispose()
			{
				var inMessages = _inMessages;
				if (inMessages == null)
					return;
				
				lock(inMessages)
				{
					if (inMessages.Count == 0)
						Dispose();
					else
						_disposeBegan = true;
				}
			}
		#endregion
		#region _Collected
			private void _Collected()
			{
				try
				{
					var inMessages = _inMessages;

					if (WasDisposed || inMessages == null)
					{
						GCUtils.Collected -= _Collected;
						return;
					}
				
					inMessages.UnabortableLock
					(
						() => inMessages.TrimExcess()
					);
				}
				catch
				{
				}
			}
		#endregion
		
		#region Properties
			#region Id
				/// <summary>
				/// Gets the Id given to this channel locally.
				/// </summary>
				public int Id
				{
					get
					{
						return _id;
					}
				}
			#endregion
			#region RemoteId
				/// <summary>
				/// Gets the Id given to this channel by the remote host.
				/// </summary>
				public int RemoteId
				{
					get
					{
						return _remoteId;
					}
				}
			#endregion
		
			#region Channeller
				internal StreamChanneller _channeller;
				
				/// <summary>
				/// Gets the channeller to which this channel belongs to.
				/// </summary>
				public StreamChanneller Channeller
				{
					get
					{
						return _channeller;
					}
				}
			#endregion
			
			#region Length
				/// <summary>
				/// Property from Stream. Always returns -1.
				/// </summary>
				public override long Length
				{
					get { return -1; }
				}
			#endregion
			#region Position
				/// <summary>
				/// Property from Stream. Always returns -1 and throws a NotSupportedException
				/// if set.
				/// </summary>
				public override long Position
				{
					get
					{
						return -1;
					}
					set
					{
						throw new NotSupportedException();
					}
				}
			#endregion
			
			#region CanRead
				/// <summary>
				/// Property from Stream. Always return true.
				/// </summary>
				public override bool CanRead
				{
					get { return true; }
				}
			#endregion
			#region CanSeek
				/// <summary>
				/// Property from Stream. Always return false.
				/// </summary>
				public override bool CanSeek
				{
					get { return false; }
				}
			#endregion
			#region CanWrite
				/// <summary>
				/// Property from Stream. Always return true.
				/// </summary>
				public override bool CanWrite
				{
					get { return true; }
				}
			#endregion

			#region CanTimeout
				/// <summary>
				/// Always returns true, indicating that this stream supports time-outs.
				/// </summary>
				public override bool CanTimeout
				{
					get
					{
						return true;
					}
				}
			#endregion
			#region ReadTimeout
				private int _readTimeout = Timeout.Infinite;

				/// <summary>
				/// Gets or sets the read-timout of this channel.
				/// </summary>
				public override int ReadTimeout
				{
					get
					{
						return base.ReadTimeout;
					}
					set
					{
						base.ReadTimeout = value;
					}
				}
			#endregion
			#region WriteTimeout
				private int _writeTimeout = Timeout.Infinite;

				/// <summary>
				/// Gets or sets the write-timout of this channel.
				/// </summary>
				public override int WriteTimeout
				{
					get
					{
						return base.WriteTimeout;
					}
					set
					{
						base.WriteTimeout = value;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region Flush
				/// <summary>
				/// Sends all buffered data to the stream.
				/// </summary>
				/// 
				public override void Flush()
				{
					if (_disposeBegan)
						throw new ObjectDisposedException("The stream is already disposing. It is only possible to read the remaining bytes.");
						
					int count = _sendBufferPosition;
					
					if (count == 0)
						return;
						
					try
					{
						_sendBufferPosition = 0;
							
						byte[] bufferCopy = new byte[count + 8];
						BitConverter.GetBytes(_id).CopyTo(bufferCopy, 0);
						BitConverter.GetBytes(count).CopyTo(bufferCopy, 4);
						
						Buffer.BlockCopy(_sendBuffer, 0, bufferCopy, 8, count);
						
						var buffersToSend = _channeller._buffersToSend;
						
						bool lockTaken = false;
						try
						{
							Monitor.TryEnter(buffersToSend, _writeTimeout, ref lockTaken);
							if (!lockTaken)
							{
								try
								{
									throw new TimeoutException("Channel.Flush() timed-out.");
								}
								catch(Exception exception)
								{
									Dispose(exception);
									throw;
								}
							}

							try
							{
							}
							finally
							{
								buffersToSend.Enqueue(bufferCopy);
							}
						}
						finally
						{
							if (lockTaken)
								Monitor.Exit(buffersToSend);
						}
							
						_channeller._writerEvent.Set();
					}
					catch(Exception exception)
					{
						var channeller = _channeller;
						if (channeller != null)
							if (!channeller.WasDisposed)
								channeller.Dispose(exception);
							
						throw;
					}
				}
			#endregion
			#region Read
				/// <summary>
				/// Reads bytes from the channel.
				/// </summary>
				/// <param name="buffer">The buffer to store the read data.</param>
				/// <param name="offset">The initial position to store data in the buffer.</param>
				/// <param name="count">The number of bytes expected to read.</param>
				/// <returns>The number of bytes actually read.</returns>
				public override int Read(byte[] buffer, int offset, int count)
				{
					CheckUndisposed();

					if (count == 0)
						return 0;
				
					byte[] actualMessage = _actualMessage;
					if (actualMessage == null)
					{
						bool mustBreak = false;
						bool mustReturn0 = false;

						while (true)
						{
							_inMessages.UnabortableLock
							(
								delegate
								{
									if (_inMessages.Count > 0)
									{
										actualMessage = _inMessages.Dequeue();
										_actualMessage = actualMessage;
										_positionInActualMessage = 0;
										mustBreak = true;
									}
									else
									{
										if (_disposeBegan)
										{
											Dispose();
											mustReturn0 = true;
										}
									}
								}
							);
							
							if (mustReturn0)
								return 0;
							
							if (mustBreak)
								break;
							
							if (!_waitEvent.WaitOne(_readTimeout))
							{
								try
								{
									throw new TimeoutException("Channel.Read() timed-out.");
								}
								catch(Exception exception)
								{
									Dispose(exception);
									throw;
								}
							}
							
							CheckUndisposed();
							
							_waitEvent.Reset();
						}
					}
					
					int messageLength = actualMessage.Length;
					int positionInActualMessage = _positionInActualMessage;
					int remainingLength = messageLength - positionInActualMessage;
					
					if (remainingLength <= count)
					{
						count = remainingLength;
						_actualMessage = null;
					}
					else
						_positionInActualMessage += count;
					
					Buffer.BlockCopy(actualMessage, positionInActualMessage, buffer, offset, count);
					
					return count;
				}
			#endregion
			#region Write
				/// <summary>
				/// Writes bytes into this channel.
				/// </summary>
				/// <param name="buffer">The buffer to get bytes to write.</param>
				/// <param name="offset">The initial position in the buffer to send.</param>
				/// <param name="count">The number of bytes from the buffer to send.</param>
				public override void Write(byte[] buffer, int offset, int count)
				{
					if (buffer == null)
						throw new ArgumentNullException("buffer");

					if (_disposeBegan)
						throw new ObjectDisposedException("The stream is already disposing. It is only possible to read the remaining bytes.");
						
					int bufferSize = _sendBuffer.Length;
				
					int lastValue = offset + count;
					for (int i=offset; i<lastValue; i++)
					{
						_sendBuffer[_sendBufferPosition] = buffer[i];
						_sendBufferPosition++;
						
						if (_sendBufferPosition == bufferSize)
							Flush();
					}
				}
			#endregion

			#region Seek
				/// <summary>
				/// Method from Stream. Throws a NotSupportedException.
				/// </summary>
				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotSupportedException();
				}
			#endregion
			#region SetLength
				/// <summary>
				/// Method from Stream. Throws a NotSupportedException.
				/// </summary>
				public override void SetLength(long value)
				{
					throw new NotSupportedException();
				}
			#endregion
		#endregion
	}
}
