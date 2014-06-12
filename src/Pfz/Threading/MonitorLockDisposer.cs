using System;
using System.Threading;

namespace Pfz.Threading
{
	/// <summary>
	/// Class returned by MonitorLockExtensions.
	/// </summary>
	public sealed class MonitorLockDisposer:
		IDisposable
	{
		#region Fields
			internal object _item;
			internal bool _lockTaken;
		#endregion
		
		#region Constructor
			internal MonitorLockDisposer(object item, int timeout)
			{
				#if DEBUG
					if (item is ReaderWriterLock || item is ReaderWriterLockSlim)
						throw new ArgumentException("You are trying to use a monitor lock over a ReaderWriterLock. You must use its methods to lock it.");
				#endif
			
				#if CHECK_FOR_DEAD_LOCKS
					LockConfiguration.AddLock(item);
				#endif
			
				_item = item;

				try
				{
					Monitor.TryEnter(item, timeout, ref _lockTaken);
				}
				finally
				{
					if (!_lockTaken)
						GC.SuppressFinalize(this);
				}
			}
		#endregion
		#region Destructor
			/// <summary>
			/// Throws an exception, as we can simple release the lock (destructors runs from another thread).
			/// </summary>
			~MonitorLockDisposer()
			{
				throw new SynchronizationLockException("A disposable lock hold was not correctly disposed. Unfortunatelly, it is impossible to free the lock.");
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases the lock.
			/// </summary>
			public void Dispose()
			{
				if (_lockTaken)
				{
					Monitor.Exit(_item);
					_item = null;
					_lockTaken = false;
				
					#if CHECK_FOR_DEAD_LOCKS
						LockConfiguration.RemoveLock(_item);
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
			public void SwitchLock(object newObjectToLock)
			{
				if (!_lockTaken)
					throw new ArgumentException("Can't switch the lock, because the lock is not held.");

				Monitor.Enter(newObjectToLock);
				Monitor.Exit(_item);
				_item = newObjectToLock;
			}
		#endregion
	}
}
