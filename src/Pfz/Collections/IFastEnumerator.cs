using System;

namespace Pfz.Collections
{
	/// <summary>
	/// A typed version of FastEnumerator. Only reference types are valid.
	/// </summary>
	/// <typeparam name="T">The type of the items to enumerate.</typeparam>
	public interface IFastEnumerator<out T>:
		IDisposable
	where
		T: class
	{
		/// <summary>
		/// Gets the next value, or null if there are no more values.
		/// </summary>
		/// <returns>The next value, or null if there are no more values.</returns>
		T GetNext();
	}
}
