using System;

namespace Pfz
{
	/// <summary>
	/// Interface for disposable objects that can inform they are already
	/// disposed without throwing an exception.
	/// </summary>
	public interface IAdvancedDisposable:
		IDisposable
	{
		/// <summary>
		/// Gets a value indicating if the object was already disposed.
		/// </summary>
		bool WasDisposed { get; }
		
		/// <summary>
		/// Checks if this object was disposed. If it was, throws an exception.
		/// </summary>
		void CheckUndisposed();
	}
}
