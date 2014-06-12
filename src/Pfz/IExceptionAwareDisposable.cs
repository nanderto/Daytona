using System;

namespace Pfz
{
	/// <summary>
	/// Interface that must be implemented by objects that are disposable
	/// and also capables of registering the Exception that caused the Dispose.
	/// </summary>
	public interface IExceptionAwareDisposable:
		IAdvancedDisposable
	{
		/// <summary>
		/// Disposes the object setting the Exception that was responsible for
		/// it's disposal.
		/// </summary>
		void Dispose(Exception exception);
		
		/// <summary>
		/// Gets the Exception that caused the dispose of this object, or null.
		/// </summary>
		Exception DisposeException { get; }
	}
}
