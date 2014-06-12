using System;

namespace Pfz.Remoting
{
	/// <summary>
	/// Interface that must be implemented by Channellers (objects that
	/// creates many channels of communication).
	/// Actually, the implementers are StreamChanneller, which creates many
	/// channels over a stream and NamedPipeChanneller, which creates many
	/// channels by creating new named-pipes.
	/// </summary>
	public interface IChanneller:
		IExceptionAwareDisposable
	{
		/// <summary>
		/// Creates a new channel.
		/// </summary>
		ExceptionAwareStream CreateChannel();
		
		/// <summary>
		/// Creates a new channel, passing the given data as the initial
		/// parameter.
		/// </summary>
		ExceptionAwareStream CreateChannel(object createData);
		
		/// <summary>
		/// Event invoked when the channeller is disposed.
		/// </summary>
		event EventHandler Disposed;
		
		/// <summary>
		/// Event invoked when a channel is created on the remote side.
		/// </summary>
		event EventHandler<ChannelCreatedEventArgs> RemoteChannelCreated;
	}
}
