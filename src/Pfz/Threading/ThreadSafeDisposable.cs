﻿using System;
using System.Diagnostics.CodeAnalysis;
using Pfz.Extensions;
using Pfz.Extensions.MonitorLockExtensions;

namespace Pfz.Threading
{
	/// <summary>
	/// This class is useful if you need to implement the dispose pattern
	/// in a thread-safe manner.
	/// It guarantees that dispose will be called only once, even if many 
	/// threads try to call Dispose at once. In your code, you must lock 
	/// DisposeLock, check if the object is disposed and call anything that 
	/// must be guaranteed to run before dispose inside the lock block.
	/// </summary>
	public abstract class ThreadSafeDisposable:
		IAdvancedDisposable
	{
		#region Destructor
			/// <summary>
			/// Calls Dispose(false);
			/// </summary>
			~ThreadSafeDisposable()
			{
				Dispose(false);
			}
		#endregion
		#region Dispose
			#region Without parameters
				/// <summary>
				/// Calls Dispose(true) to release all resources.
				/// Guarantees that only a single call to Dispose(true) is done 
				/// even if  this method is invoked multiple times or by many 
				/// different threads at the same time.
				/// </summary>
				[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly")]
				public void Dispose()
				{
					if (_wasDisposed)
						return;

					bool mustDispose = false;
					try
					{
						lock(_disposeLock)
						{
							if (_wasDisposed)
								return;

							try
							{
							}
							finally
							{
								_wasDisposed = true;
								mustDispose = true;
							}
						}
					}
					finally
					{
						if (mustDispose)
						{
							Dispose(true);
							GC.SuppressFinalize(this);
						}
					}
				}
			#endregion
			#region Protected - bool disposing
				/// <summary>
				/// Implement this method to release all resources used by this object.
				/// </summary>
				/// <param name="disposing">
				/// This parameter is true if it was called by a user call to Dispose(),
				/// and false if it was called by a destructor. If false, you don't need
				/// to release managed resources (in general, you don't need to set any
				/// variables to null or even reference other objects, only freeing 
				/// "unsafe" pointers).
				/// </param>
				protected virtual void Dispose(bool disposing)
				{
				}
			#endregion
		#endregion
		
		#region Properties
			#region WasDisposed
				private volatile bool _wasDisposed;
				
				/// <summary>
				/// Returns true if a call to Dispose was already done (or still in
				/// progress in another thread). If it is true, you can't continue
				/// to use your object.
				/// </summary>
				public bool WasDisposed
				{
					get
					{
						return _wasDisposed;
					}
				}
			#endregion
			#region DisposeLock
				private readonly object _disposeLock = new object();
				
				/// <summary>
				/// This is the lock used during dispose. You may want to lock
				/// some of your code against dispose using this lock.
				/// The preferred way to use it is: AbortSafe.Lock
				/// (
				///		DisposeLock,
				///		delegate
				///		{
				///			CheckUndisposed();
				///		
				///			... your protected code here ...
				///		}
				///	);
				/// </summary>
				public object DisposeLock
				{
					get
					{
						return _disposeLock;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region CheckUndisposed
				/// <summary>
				/// Checks if the objects is disposed. If it is, throws an 
				/// ObjectDisposedException. Call this as the first method inside a 
				/// AbortSafe.Lock
				/// (
				///		DisposeLock,
				///		delegate
				///		{
				///			CheckUndisposed(); 
				///			... your protected code here ...
				///		}
				///	);
				/// 
				/// or simple call it without any lock if you only want to throw
				/// an exception if the object is already disposed but such a call
				/// is read-only.
				/// </summary>
				public void CheckUndisposed()
				{
					if (_wasDisposed)
						throw new ObjectDisposedException(GetType().FullName);
				}
			#endregion
		#endregion
	}
}
