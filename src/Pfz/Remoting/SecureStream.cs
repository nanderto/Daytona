using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Pfz.Remoting
{
	/// <summary>
	/// This is a class that transforms a simple (non-encrypted) stream into an
	/// encrypted one. <br/>
	/// It does: Create an assymetric private key (on server), sends the public
	/// part to the client and, then, the client creates a new symmetric key that 
	/// is known is sent to the server using the asmmetric key.<br/>
	/// After that, only the symmetric algorithm is used, as it is faster,
	/// but it is guaranteed that only the server and the client knows the key.<br/>
	/// This cryptography guarantees that no one "sniffing" the network would be
	/// able to interpret the messages, but does not guarantees that the requested
	/// host is really the host it should be. To that additional verification,
	/// you would probably need to deal with certificates and the SslStream.
	/// </summary>
	public class SecureStream:
		Stream
	{
		#region Fields
			private static readonly byte[] _emptyReadBuffer = new byte[0];
			private MemoryStream _writeBuffer;
		#endregion

		#region Constructors
			/// <summary>
			/// Creates a new secure stream (stream that uses an assymetric key to
			/// initialize and then a symmetric key to continue it's work) over another
			/// stream, without any other parameters, so, running as client.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public SecureStream(Stream baseStream):
				this(baseStream, new RSACryptoServiceProvider(), SymmetricAlgorithm.Create(), false)
			{
			}

			/// <summary>
			/// Creates a new secure stream (stream that uses an assymetric key to
			/// initialize and then a symmetric key to continue it's work) over another
			/// stream, specifying if running as client or server, but without changing
			/// the default symmetric or assymetric class/algorithm..
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public SecureStream(Stream baseStream, bool runAsServer):
				this(baseStream, new RSACryptoServiceProvider(), SymmetricAlgorithm.Create(), runAsServer)
			{
			}
		
			/// <summary>
			/// Creates a new secure stream (stream that uses an assymetric key to
			/// initialize and then a symmetric key to continue it's work) over another
			/// stream. <br/>
			/// Species the symmetricAlgorithm to use.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public SecureStream(Stream baseStream, SymmetricAlgorithm symmetricAlgorithm):
				this(baseStream, new RSACryptoServiceProvider(), symmetricAlgorithm, false, 2048)
			{
			}
		
			/// <summary>
			/// Creates a new secure stream (stream that uses an assymetric key to
			/// initialize and then a symmetric key to continue it's work) over another
			/// stream. <br/>
			/// Species the symmetricAlgorithm to use and if it runs as a client or server.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public SecureStream(Stream baseStream, SymmetricAlgorithm symmetricAlgorithm, bool runAsServer):
				this(baseStream, new RSACryptoServiceProvider(), symmetricAlgorithm, runAsServer, 2048)
			{
			}

			/// <summary>
			/// Creates a new secure stream (stream that uses an assymetric key to
			/// initialize and then a symmetric key to continue it's work) over another
			/// stream. <br/>
			/// Specifies the assymetric and the symmetric algorithm to use, and if it 
			/// must run as client or server.
			/// </summary>
			public SecureStream(Stream baseStream, RSACryptoServiceProvider rsa, SymmetricAlgorithm symmetricAlgorithm, bool runAsServer):
				this(baseStream, rsa, symmetricAlgorithm, runAsServer, 2048)
			{
			}

			/// <summary>
			/// Creates a new secure stream (stream that uses an assymetric key to
			/// initialize and then a symmetric key to continue it's work) over another
			/// stream. <br/>
			/// Specifies the assymetric and the symmetric algorithm to use, if it 
			/// must run as client or server and the writeBufferInitialLength.
			/// </summary>
			public SecureStream(Stream baseStream, RSACryptoServiceProvider rsa, SymmetricAlgorithm symmetricAlgorithm, bool runAsServer, int writeBufferInitialLength)
			{
				if (baseStream == null)
					throw new ArgumentNullException("baseStream");
			
				if (rsa == null)
					throw new ArgumentNullException("rsa");
				
				if (symmetricAlgorithm == null)
					throw new ArgumentNullException("symmetricAlgorithm");
				
				if (writeBufferInitialLength < 0)
					throw new ArgumentException("writeBufferInitialLength must can't be negative.", "writeBufferInitialLength");
		
				BaseStream = baseStream;
				SymmetricAlgorithm = symmetricAlgorithm;

				string symmetricTypeName = symmetricAlgorithm.GetType().ToString();
				byte[] symmetricTypeBytes = Encoding.UTF8.GetBytes(symmetricTypeName);
				if (runAsServer)
				{
					byte[] sizeBytes = BitConverter.GetBytes(symmetricTypeBytes.Length);
					baseStream.Write(sizeBytes, 0, sizeBytes.Length);
					baseStream.Write(symmetricTypeBytes, 0, symmetricTypeBytes.Length);
			
					byte[] bytes = rsa.ExportCspBlob(false);
					sizeBytes = BitConverter.GetBytes(bytes.Length);
					baseStream.Write(sizeBytes, 0, sizeBytes.Length);
					baseStream.Write(bytes, 0, bytes.Length);
				
					symmetricAlgorithm.Key = _ReadWithLength(rsa);;
					symmetricAlgorithm.IV = _ReadWithLength(rsa);
				}
				else
				{
					// ok. We run as the client, so first we first check the
					// algorithm types and then receive the assymetric
					// key from the server.
				
					// symmetricAlgorithm
					var sizeBytes = new byte[4];
					_ReadDirect(sizeBytes);
					var stringLength = BitConverter.ToInt32(sizeBytes, 0);
				
					if (stringLength != symmetricTypeBytes.Length)
						throw new ArgumentException("Server and client must use the same SymmetricAlgorithm class.");
				
					var stringBytes = new byte[stringLength];
					_ReadDirect(stringBytes);
					var str = Encoding.UTF8.GetString(stringBytes);
					if (str != symmetricTypeName)
						throw new ArgumentException("Server and client must use the same SymmetricAlgorithm class.");

					// public key.
					sizeBytes = new byte[4];
					_ReadDirect(sizeBytes);
					int asymmetricKeyLength = BitConverter.ToInt32(sizeBytes, 0);
					byte[] bytes = new byte[asymmetricKeyLength];
					_ReadDirect(bytes);
					rsa.ImportCspBlob(bytes);
				
					// Now that we have the asymmetricAlgorithm set, and considering
					// that the symmetricAlgorithm initializes automatically, we must
					// only send the key.
					_WriteWithLength(rsa, symmetricAlgorithm.Key);
					_WriteWithLength(rsa, symmetricAlgorithm.IV);
				}
			
				// After the object initialization being done, be it a client or a
				// server, we can dispose the assymetricAlgorithm.
				rsa.Clear();
			
				Decryptor = symmetricAlgorithm.CreateDecryptor();
				Encryptor = symmetricAlgorithm.CreateEncryptor();
			
				_readBuffer = _emptyReadBuffer;
				_writeBuffer = new MemoryStream(writeBufferInitialLength);
 			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases the buffers, the basestream and the cryptographic classes.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					var writeBuffer = _writeBuffer;
					if (writeBuffer != null)
					{
						_writeBuffer = null;
						writeBuffer.Dispose();
					}
				
					var encryptor = this.Encryptor;
					if (encryptor != null)
					{
						Encryptor = null;
						encryptor.Dispose();
					}
				
					var decryptor = this.Decryptor;
					if (decryptor != null)
					{
						Decryptor = null;
						decryptor.Dispose();
					}
				
					var symmetricAlgorithm = SymmetricAlgorithm;
					if (symmetricAlgorithm != null)
					{
						SymmetricAlgorithm = null;
						symmetricAlgorithm.Clear();
					}
				
					var baseStream = this.BaseStream;
					if (baseStream != null)
					{
						BaseStream = null;
						baseStream.Dispose();
					}
				
					_readBuffer = null;
				}
		
				base.Dispose(disposing);
			}
		#endregion

		#region Properties
			/// <summary>
			/// Gets the original stream that created this asymmetric crypto stream.
			/// </summary>
			public Stream BaseStream { get; private set; }
		
			/// <summary>
			/// Gets the symmetric algorithm being used.
			/// </summary>
			public SymmetricAlgorithm SymmetricAlgorithm { get; private set; }
		
			/// <summary>
			/// Gets the encryptor being used.
			/// </summary>
			public ICryptoTransform Decryptor { get; private set; }
		
			/// <summary>
			/// Gets the decryptor being used.
			/// </summary>
			public ICryptoTransform Encryptor { get; private set; }
		
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
			/// Always returns false.
			/// </summary>
			public override bool CanSeek
			{
				get
				{
					return false;
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


			/// <summary>
			/// Throws a NotSupportedException.
			/// </summary>
			public override long Length
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			/// <summary>
			/// Throws a NotSupportedException.
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
			/// Gets or sets the time-out for reads.
			/// </summary>
			public override int ReadTimeout
			{
				get
				{
					return BaseStream.ReadTimeout;
				}
				set
				{
					BaseStream.ReadTimeout = value;
				}
			}

			/// <summary>
			/// Gets or sets the time-out for writes.
			/// </summary>
			public override int WriteTimeout
			{
				get
				{
					return BaseStream.WriteTimeout;
				}
				set
				{
					BaseStream.WriteTimeout = value;
				}
			}

			/// <summary>
			/// Gets a value indicating if this stream supports timed-out operations.
			/// </summary>
			public override bool CanTimeout
			{
				get
				{
					return BaseStream.CanTimeout;
				}
			}
		#endregion
		#region Methods
			#region Read
				private readonly byte[] _sizeBytes = new byte[5];
				private int _readPosition;
				private byte[] _readBuffer;
				private byte _readCRC;
				/// <summary>
				/// Reads and decryptographs the given number of bytes from the buffer.
				/// </summary>
				public override int Read(byte[] buffer, int offset, int count)
				{
					if (_readPosition == _readBuffer.Length)
					{
						_ReadDirect(_sizeBytes);
						int readLength = BitConverter.ToInt32(_sizeBytes, 0);
				
						if (_readBuffer.Length < readLength)
							_readBuffer = new byte[readLength];
					
						_FullReadDirect(_readBuffer, readLength);
						_readBuffer = Decryptor.TransformFinalBlock(_readBuffer, 0, readLength);
				
						// here we check if our actual CRC is the same as the message CRC.
						byte crc = _sizeBytes[4];
						if (crc != _readCRC)
							throw new IOException("Invalid CRC.");
					
						// And after we decrypt the message with such crc.
						int readBufferLength = _readBuffer.Length;
						for(int i=0; i<readBufferLength; i++)
						{
							byte newCrc = _readBuffer[i];
							_readBuffer[i] ^= crc;
							crc = newCrc;
						}
				
						_readCRC = crc;
						_readPosition = 0;
					}
			
					int diff = _readBuffer.Length - _readPosition;
					if (count > diff)
						count = diff;
			
					Buffer.BlockCopy(_readBuffer, _readPosition, buffer, offset, count);
					_readPosition += count;
					return count;
				}
			#endregion
			#region Write
				/// <summary>
				/// Encrypts and writes the given bytes.
				/// </summary>
				public override void Write(byte[] buffer, int offset, int count)
				{
					_writeBuffer.Write(buffer, offset, count);
				}
			#endregion
			#region Flush
				private int _maxLength;
				private int _collectionNumber = GC.CollectionCount(GC.MaxGeneration);
				private byte _writeCRC;
				/// <summary>
				/// Sends all the buffered data.
				/// </summary>
				public override void Flush()
				{
					int length = (int)_writeBuffer.Length;
					if (length > 0)
					{
						// here we pre-encrypt the block and generate a CRC. We do this so
						// two identical blocks will, in fact, be different.
						var writeBuffer = _writeBuffer.GetBuffer();
						byte crc = _writeCRC;
						for (int i=0; i<length; i++)
						{
							crc ^= writeBuffer[i];
							writeBuffer[i] = crc;
						}
			
						var encryptedBuffer = Encryptor.TransformFinalBlock(writeBuffer, 0, length);
						var size = BitConverter.GetBytes(encryptedBuffer.Length);
						BaseStream.Write(size, 0, size.Length);
						BaseStream.WriteByte(_writeCRC);
						BaseStream.Write(encryptedBuffer, 0, encryptedBuffer.Length);
						BaseStream.Flush();
				
						_writeCRC = crc;
						_writeBuffer.SetLength(0);
				
						int collectionNumber = GC.CollectionCount(GC.MaxGeneration);
						if (collectionNumber == _collectionNumber)
						{
							if (length > _maxLength)
								_maxLength = length;
						}
						else
						{
							if (_maxLength != 0)
							{
								int halfLength = _writeBuffer.Capacity / 2;
								if (_maxLength < halfLength)
									_writeBuffer.Capacity = _maxLength + (_maxLength / 2);
							
								_maxLength = 0;
							}
					
							_collectionNumber = collectionNumber;
						}
					}
				}
			#endregion

			#region _ReadDirect
				private void _ReadDirect(byte[] bytes)
				{
					_FullReadDirect(bytes, bytes.Length);
				}
				private void _FullReadDirect(byte[] bytes, int length)
				{
					int read = 0;
					while(read < length)
					{
						int readResult = BaseStream.Read(bytes, read, length - read);
				
						if (readResult == 0)
							throw new IOException("The stream was closed by the remote side.");
				
						read += readResult;
					}
				}
			#endregion
			#region _ReadWithLength
				private byte[] _ReadWithLength(RSACryptoServiceProvider rsa)
				{
					byte[] size = new byte[4];
					_ReadDirect(size);
		
					int count = BitConverter.ToInt32(size, 0);
					var bytes = new byte[count];
					_ReadDirect(bytes);
			
					return rsa.Decrypt(bytes, false);
				}
			#endregion
			#region _WriteWithLength
				private void _WriteWithLength(RSACryptoServiceProvider rsa, byte[] bytes)
				{
					bytes = rsa.Encrypt(bytes, false);
					byte[] sizeBytes = BitConverter.GetBytes(bytes.Length);
					BaseStream.Write(sizeBytes, 0, sizeBytes.Length);
					BaseStream.Write(bytes, 0, bytes.Length);
				}
			#endregion

			#region Not Supported Methods
				/// <summary>
				/// Throws a NotSupportedException.
				/// </summary>
				public override long Seek(long offset, SeekOrigin origin)
				{
					throw new NotSupportedException();
				}

				/// <summary>
				/// Throws a NotSupportedException.
				/// </summary>
				public override void SetLength(long value)
				{
					throw new NotSupportedException();
				}
			#endregion
		#endregion
	}
}
