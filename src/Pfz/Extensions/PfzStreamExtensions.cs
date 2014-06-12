using System;
using System.IO;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds overloads to the stream Read method and adds the FullRead method,
	/// which will continue to read until it reads everything that was requested,
	/// or throws an IOException.
	/// </summary>
	public static class PfzStreamExtensions
	{
		/// <summary>
		/// Calls read using the full given buffer.
		/// </summary>
		public static int Read(this Stream stream, byte[] buffer)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			return stream.Read(buffer, 0, buffer.Length);
		}
		
		/// <summary>
		/// Calls read using the given buffer and the initialIndex.
		/// </summary>
		public static int Read(this Stream stream, byte[] buffer, int initialIndex)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			return stream.Read(buffer, initialIndex, buffer.Length - initialIndex);
		}
		
		/// <summary>
		/// Writes all the bytes in the given buffer.
		/// </summary>
		public static void Write(this Stream stream, byte[] buffer)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			stream.Write(buffer, 0, buffer.Length);
		}
		
		/// <summary>
		/// Writes the bytes from the given buffer, beginning at the given beginIndex.
		/// </summary>
		public static void Write(this Stream stream, byte[] buffer, int initialIndex)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			stream.Write(buffer, initialIndex, buffer.Length - initialIndex);
		}

		/// <summary>
		/// Will read the given buffer to the end.
		/// Throws an exception if it's not possible to read the full buffer.
		/// </summary>
		public static void FullRead(this Stream stream, byte[] buffer)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			stream.FullRead(buffer, 0, buffer.Length);
		}
		
		/// <summary>
		/// Full reads the stream over the given buffer, but only at the given
		/// initialIndex. If the requested length can't be read, throws an 
		/// IOException.
		/// </summary>
		public static void FullRead(this Stream stream, byte[] buffer, int initialIndex)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			stream.FullRead(buffer, initialIndex, buffer.Length - initialIndex);
		}
		
		/// <summary>
		/// Reads the buffer in the requested area, but throws an exception if
		/// can't read the full requested area.
		/// </summary>
		public static void FullRead(this Stream stream, byte[] buffer, int initialIndex, int count)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (buffer == null)
				throw new ArgumentNullException("buffer");

			int position = initialIndex;
			int end = initialIndex + count;
			
			while(position < end)
			{
				int read = stream.Read(buffer, position, end-position);
				
				if (read == 0)
					throw new IOException("End of Stream or Stream Closed before reading all needed information.");
				
				position += read;
			}
		}
		
		/// <summary>
		/// Copies data from one stream to another, using the given buffer for each
		/// operation and calling an action, if provided, to tell how the progress
		/// is going.
		/// </summary>
		/// <param name="sourceStream">The stream to read data from.</param>
		/// <param name="destinationStream">The stream to write data to.</param>
		/// <param name="blockBuffer">To buffer to use for read and write operations. The buffer does not need to be of the size of the streamed data, as many read/writes are done if needed.</param>
		/// <param name="onProgress">The action to be executed as each block is successfully copied. The value passed as parameter is the number of bytes read this time (not the total). This parameter can be null.</param>
		public static void CopyTo(this Stream sourceStream, Stream destinationStream, byte[] blockBuffer, Action<int> onProgress)
		{
			if (sourceStream == null)
				throw new ArgumentNullException("sourceStream");
			
			if (destinationStream == null)
				throw new ArgumentNullException("destinationStream");
			
			if (blockBuffer == null)
				throw new ArgumentNullException("blockBuffer");
			
			int length = blockBuffer.Length;
			while(true)
			{
				int read = sourceStream.Read(blockBuffer, 0, length);
				if (read == 0)
					return;
				
				destinationStream.Write(blockBuffer, 0, read);
				destinationStream.Flush();
				
				if (onProgress != null)
					onProgress(read);
			}
		}
	}
}
