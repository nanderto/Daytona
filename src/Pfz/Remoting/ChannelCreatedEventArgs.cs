using System;

namespace Pfz.Remoting
{
	/// <summary>
	/// Argument class used as the parameter by the StreamChanneller, when
	/// a channel is created as a request from the remote site.
	/// </summary>
	public sealed class ChannelCreatedEventArgs:
		EventArgs
	{
		/// <summary>
		/// Creates a new instance of ChannelCreatedEventArgs.
		/// </summary>
		public ChannelCreatedEventArgs()
		{
			CanDisposeChannel = true;
		}
	
		/// <summary>
		/// The channel that was just created.
		/// </summary>
		public ExceptionAwareStream Channel { get; internal set; }
		
		/// <summary>
		/// Data sent by the other side when asking for a new channel.
		/// </summary>
		public object Data { get; internal set; }
		
		/// <summary>
		/// Gets or sets a flag allowing to dispose the channel as soon the events
		/// are all processed. By default this value is true, as the channel is useless
		/// after the thread that processes it finishes. But, if the user wants to keep
		/// the channel open to use it by another thread, it must set this to false.
		/// </summary>
		public bool CanDisposeChannel { get; set; }
	}
}
