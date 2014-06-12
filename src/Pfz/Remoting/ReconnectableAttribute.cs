using System;

namespace Pfz.Remoting
{
	/// <summary>
	/// Attribute used in remote methods to tell that they should always generate the same result
	/// if given the same parameters. This way, objects can be "recreated" when the connection is lost.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class ReconnectableAttribute:
		Attribute
	{
	}
}
