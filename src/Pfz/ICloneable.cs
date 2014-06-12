using System;

namespace Pfz
{
	/// <summary>
	/// Typed version of ICloneable interface.
	/// </summary>
	public interface ICloneable<out T>:
		ICloneable
	{
		/// <summary>
		/// Returns a typed clone of this object.
		/// </summary>
		new T Clone();
	}
}
