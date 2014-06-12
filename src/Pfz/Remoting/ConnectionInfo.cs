using System;
using System.IO;

namespace Pfz.Remoting
{
	/// <summary>
	/// Structs that holds the real connection, not considering any specific type for it, and
	/// its stream, which is the one really used by the remoting framework.
	/// </summary>
	public struct ConnectionInfo
	{
		/// <summary>
		/// Creates a new ConnectionInfo, using the given connection and stream.
		/// </summary>
		public ConnectionInfo(object connection, Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			_connection = connection;
			_stream = stream;
		}

		private object _connection;
		/// <summary>
		/// Gets the Connection used to communicate.
		/// In some cases, this can be null if only the stream is known.
		/// </summary>
		public object Connection
		{
			get
			{
				return _connection;
			}
		}

		private Stream _stream;
		/// <summary>
		/// Gets the stream used for communication.
		/// </summary>
		public Stream Stream
		{
			get
			{
				return _stream;
			}
		}
	}
}
