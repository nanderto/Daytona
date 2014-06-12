using System;
using System.Diagnostics.CodeAnalysis;

namespace Pfz.Threading
{
	/// <summary>
	/// A ThreadSafeDisposable inheritor capable of keeping information of why
	/// it was disposed, considering it is disposed by an exception. This is 
	/// largelly used in the Remoting framework, as some exceptions can
	/// dispose the objects, but for the user is better to know the original
	/// exception, not the ObjectDisposedException.
	/// </summary>
	[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
	public abstract class ThreadSafeExceptionAwareDisposable:
		ThreadSafeDisposable,
		IExceptionAwareDisposable
	{
		/// <summary>
		/// Disposes the actual object, setting the DisposeException property.
		/// </summary>
		public void Dispose(Exception exception)
		{
			DisposeException = exception;
			Dispose();
		}
		
		/// <summary>
		/// Gets the Exception that caused this object to be disposed, or null.
		/// </summary>
		public Exception DisposeException { get; private set; }
		
		/// <summary>
		/// New version of CheckUndisposed, which will throw the appropriate
		/// exception instead of ObjectDisposedException if this object was
		/// disposed by another exception.
		/// </summary>
		public new void CheckUndisposed()
		{
			var disposeException = DisposeException;
			if (disposeException != null)
				throw new ObjectDisposedException("Object disposed: " + GetType().FullName, disposeException);
				
			base.CheckUndisposed();
		}
	}
}
