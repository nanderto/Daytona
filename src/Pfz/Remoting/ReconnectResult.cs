
namespace Pfz.Remoting
{
	/// <summary>
	/// Enum returned by RemotingClient.TryReconnectIfNeeded method.
	/// </summary>
	public enum ReconnectResult
	{
		/// <summary>
		/// The object is already connected, so there is no need to reconnect.
		/// </summary>
		None,

		/// <summary>
		/// The object was sucessfully reconnected.
		/// </summary>
		Reconnected,

		/// <summary>
		/// The object is reconnectable, but the reconnection failed.
		/// </summary>
		ReconnectionFailed,

		/// <summary>
		/// The object is not reconnectable.
		/// </summary>
		NotReconnectable
	}
}
