using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Pfz
{
	/// <summary>
	/// Stream class that has a Dispose receiving an Exception as the cause
	/// of the dispose. Calling any method after the dispose will throw a
	/// new ObjectDisposedException with that exception as the inner exception.
	/// </summary>
	public abstract class ExceptionAwareStream:
		Stream,
		IExceptionAwareDisposable
	{
		#region Dispose
			/// <summary>
			/// Disposes the stream and sets the DisposeException.
			/// </summary>
			public void Dispose(Exception exception)
			{
				DisposeException = exception;
				Dispose();
			}
		
			/// <summary>
			/// Implemented to guarantee that it will be executed only once.
			/// You must reimplement DoDispose, as this method is sealed.
			/// </summary>
			[SuppressMessage("Microsoft.Usage", "CA2215:Dispose methods should call base class dispose"), SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
			protected sealed override void Dispose(bool disposing)
			{
				if (WasDisposed)
					return;

				bool mustDispose = false;
				try
				{
					lock(DisposeLock)
					{
						if (WasDisposed)
							return;

						try
						{
						}
						finally
						{
							mustDispose = true;
							WasDisposed = true;
						}
					}
				}
				finally
				{
					if (mustDispose)
						OnDispose(disposing);
				}
			}
		#endregion
		#region OnDispose
			/// <summary>
			/// Does the effective dispose.
			/// </summary>
			protected virtual void OnDispose(bool disposing)
			{
				base.Dispose(disposing);
			}
		#endregion
		#region CheckUndisposed
			/// <summary>
			/// Checks if this object was disposed. If it is, throws an exception.
			/// </summary>
			public void CheckUndisposed()
			{
				var disposeException = DisposeException;
				if (disposeException != null)
					throw new ObjectDisposedException("Object disposed: " + GetType().FullName, disposeException);
			
				if (WasDisposed)
					throw new ObjectDisposedException(GetType().FullName);
			}
		#endregion

		#region Properties
			#region DisposeLock
				private object _disposeLock = new object();
				/// <summary>
				/// Gets the lock that should be used by the object.
				/// </summary>
				public object DisposeLock
				{
					get
					{
						return _disposeLock;
					}
				}
			#endregion
			#region DisposeException
				/// <summary>
				/// Gets the exception that caused the Dispose, if any.
				/// </summary>
				public Exception DisposeException { get; private set; }
			#endregion
			#region WasDisposed
				/// <summary>
				/// Gets a value indicating if this object was already disposed.
				/// </summary>
				public bool WasDisposed { get; private set; }
			#endregion
		#endregion
	}
}
