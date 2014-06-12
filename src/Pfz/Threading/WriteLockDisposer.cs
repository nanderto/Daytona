using System;
using System.Diagnostics;
using System.Threading;

namespace Pfz.Threading
{
	/// <summary>
	/// Class returned by the ReaderWriterLockExtensions when a
	/// write-lock is obtained.
	/// </summary>
	public sealed class WriteLockDisposer:
		IDisposable
	{
		#region Fields
			internal ReaderWriterLockSlim _readerWriterLock;
			#if DEBUG
				private Thread _thread;
				private StackTrace _stackTrace;
			#endif
		#endregion
		
		#region Constructor
			internal WriteLockDisposer(ReaderWriterLockSlim readerWriterLock, int timeout)
			{
				#if DEBUG
					GC.SuppressFinalize(this);
					_thread = Thread.CurrentThread;
					_stackTrace = new StackTrace(true);
					GC.ReRegisterForFinalize(this);

					#if CHECK_FOR_DEAD_LOCKS
						LockConfiguration.AddLock(readerWriterLock);
					#endif
				#endif

				_readerWriterLock = readerWriterLock;

				if (!readerWriterLock.TryEnterWriteLock(timeout))
				{
					GC.SuppressFinalize(this);
					_readerWriterLock = null;
				}
			}
		#endregion
		#region Destructor
			/// <summary>
			/// Throws an exception, as we can simple release the lock (destructors runs from another thread).
			/// </summary>
			~WriteLockDisposer()
			{
				LockConfiguration.ThrowLockDisposerException
				(
					"WriteLock"
					
					#if DEBUG
						,
						_thread,
						_stackTrace
					#endif
				);
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases the lock if it is still hold.
			/// </summary>
			public void Dispose()
			{
				ReaderWriterLockSlim lockObject = _readerWriterLock;
				if (lockObject != null)
				{
					lockObject.ExitWriteLock();
					_readerWriterLock = null;

					#if CHECK_FOR_DEAD_LOCKS
						LockConfiguration.RemoveLock(lockObject);
					#endif

					GC.SuppressFinalize(this);
				}
			}
		#endregion

		#region SwitchLock
			/// <summary>
			/// First locks the new object and then releases the old lock.
			/// This is NOT abort-safe.
			/// </summary>
			public void SwitchLock(ReaderWriterLockSlim newReaderWriterLock)
			{
				if (newReaderWriterLock == null)
					throw new ArgumentNullException("newReaderWriterLock");

				newReaderWriterLock.EnterWriteLock();
				_readerWriterLock.ExitWriteLock();
				_readerWriterLock = newReaderWriterLock;
			}
		#endregion
	}
}
