using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using Pfz.Extensions.MonitorLockExtensions;

namespace Pfz.Remoting
{
	/// <summary>
	/// Helper class to create the DuplexStream generic class.
	/// </summary>
	public static class DuplexStream
	{
		#region Create
			/// <summary>
			/// Creates a new Duplex stream using the given stream.
			/// Method useful when you don't want to type the full name of the
			/// stream.
			/// Example: var duplexStream = DuplexStream.Create(readStream, writeStream);
			/// </summary>
			public static DuplexStream<T> Create<T>(T readStream, T writeStream)
			where
				T: Stream
			{
				return new DuplexStream<T>(readStream, writeStream);
			}
		#endregion
	
		#region CreateNamedPipeServer
			/// <summary>
			/// Creates a full duplex named pipe server.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public static DuplexStream<NamedPipeServerStream> CreateNamedPipeServer(string pipeName, bool mustWaitForConnection)
			{
				NamedPipeServerStream readStream = null;
				NamedPipeServerStream writeStream = null;
				try
				{
					readStream = new NamedPipeServerStream(pipeName + "_ServerRead", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough);
					writeStream = new NamedPipeServerStream(pipeName + "_ServerWrite", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.WriteThrough);
					
					if (mustWaitForConnection)
					{
						readStream.WaitForConnection();
						writeStream.WaitForConnection();
					}
					
					var result = Create(readStream, writeStream);
					return result;
				}
				catch
				{
					if (readStream != null)
						readStream.Close();
					
					if (writeStream != null)
						writeStream.Close();
						
					throw;
				}
			}
		#endregion
		#region CreateNamedPipeClient
			/// <summary>
			/// Creates a full duplex named pipe client.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public static DuplexStream<NamedPipeClientStream> CreateNamedPipeClient(string serverName, string pipeName, bool mustConnect)
			{
				NamedPipeClientStream readStream = null;
				NamedPipeClientStream writeStream = null;
				try
				{
					readStream = new NamedPipeClientStream(serverName, pipeName + "_ServerWrite", PipeDirection.In, PipeOptions.WriteThrough);
					writeStream = new NamedPipeClientStream(serverName, pipeName + "_ServerRead", PipeDirection.Out, PipeOptions.WriteThrough);
					
					if (mustConnect)
					{
						readStream.Connect(60000);
						writeStream.Connect(60000);
					}
					
					var result = Create(readStream, writeStream);
					return result;
				}
				catch
				{
					if (readStream != null)
						readStream.Close();
					
					if (writeStream != null)
						writeStream.Close();
						
					throw;
				}
			}
		#endregion
	}

	/// <summary>
	/// This class creates a "duplex" stream using two other streams,
	/// which one must be capable of reading and the other of writing.
	/// This class is used when working with NamedPipes, as the custom
	/// named-pipe does not allow the server and the client to read at the
	/// same time.
	/// </summary>
	public class DuplexStream<T>:
		ExceptionAwareStream
	where
		T: Stream
	{
		#region Constructor
			/// <summary>
			/// Creates the duplex stream using one stream to read and one to write.
			/// The other side must use the stream in the opposite order.
			/// </summary>
			public DuplexStream(T readStream, T writeStream)
			{
				if (readStream == null)
					throw new ArgumentNullException("readStream");
					
				if (writeStream == null)
					throw new ArgumentNullException("writeStream");
			
				ReadStream = readStream;
				WriteStream = writeStream;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases the read and write streams.
			/// </summary>
			protected override void OnDispose(bool disposing)
			{
				if (disposing)
				{
					var readStream = ReadStream;
					if (readStream != null)
					{
						ReadStream = null;
						readStream.Dispose();
					}
					
					var writeStream = WriteStream;
					if (writeStream != null)
					{
						WriteStream = null;
						writeStream.Dispose();
					}
				}

				base.OnDispose(disposing);
			}
		#endregion
		
		#region Properties
			#region ReadStream
				/// <summary>
				/// Gets the stream used for reading.
				/// </summary>
				public T ReadStream { get; private set; }
			#endregion
			#region WriteStream
				/// <summary>
				/// Gets the stream used for writing.
				/// </summary>
				public T WriteStream { get; private set; }
			#endregion

			#region Always true
				/// <summary>
				/// Always returns true.
				/// </summary>
				public override bool CanRead
				{
					get
					{
						return true;
					}
				}

				/// <summary>
				/// Always returns true.
				/// </summary>
				public override bool CanWrite
				{
					get
					{
						return true;
					}
				}
			#endregion
			#region Always false
				/// <summary>
				/// Always returns false.
				/// </summary>
				public override bool CanSeek
				{
					get
					{
						return false;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region Flush
				/// <summary>
				/// Flushes the write stream.
				/// </summary>
				public override void Flush()
				{
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							WriteStream.Flush();
						}
					);
				}
			#endregion
			#region Read
				/// <summary>
				/// Reads from the ReadStream.
				/// </summary>
				public override int Read(byte[] buffer, int offset, int count)
				{
					CheckUndisposed();
					return ReadStream.Read(buffer, offset, count);
				}
			#endregion
			#region Write
				/// <summary>
				/// Writes to the WriteStream.
				/// </summary>
				public override void Write(byte[] buffer, int offset, int count)
				{
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							WriteStream.Write(buffer, offset, count);
						}
					);
				}
			#endregion
		#endregion
		
		#region Not Supported
			/// <summary>
			/// Throws NotSupportedException.
			/// </summary>
			public override long Length
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			/// <summary>
			/// Throws NotSupportedException.
			/// </summary>
			public override long Position
			{
				get
				{
					throw new NotSupportedException();
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			/// <summary>
			/// Throws NotSupportedException.
			/// </summary>
			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Throws NotSupportedException.
			/// </summary>
			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}
		#endregion
	}
}
