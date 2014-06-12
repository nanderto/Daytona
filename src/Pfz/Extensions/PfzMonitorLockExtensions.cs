using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Pfz.Threading;

namespace Pfz.Extensions.MonitorLockExtensions
{
	/// <summary>
	/// Adds methods to lock any object using Monitor methods easily and
	/// always with time-out, so you can avoid dead-locks.
	/// See PfzLockConfiguration class if you want to log dead-locks.
	/// </summary>
	public static class PfzMonitorLockExtensions
	{
		#region LockWithTimeout - Disposable
			/// <summary>
			/// Tries to acquire a lock on the given object, using the default lock-timeout.
			/// In case of failure, it logs the error, but does not generates an exception. Instead, it returns
			/// null.
			/// </summary>
			/// <typeparam name="T">The type of class to lock.</typeparam>
			/// <param name="item">The item to lock.</param>
			/// <returns>A disposable object, so you can release the lock, or null if the lock was not acquired.</returns>
			public static MonitorLockDisposer TryLockWithTimeout<T>(this T item)
			where
				T: class
			{
				return TryLockWithTimeout(item, LockConfiguration.DefaultLockTimeout);
			}
			
			/// <summary>
			/// Tries to acquire a lock on the given object, using the given time-out.
			/// In case of failure, it logs the error, but does not generates an exception. Instead, it returns
			/// null.
			/// </summary>
			/// <typeparam name="T">The type of class to lock.</typeparam>
			/// <param name="item">The item to lock.</param>
			/// <param name="timeoutInMilliseconds">The timeout value while trying to acquire the lock.</param>
			/// <returns>A disposable object, so you can release the lock, or null if the lock was not acquired.</returns>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public static MonitorLockDisposer TryLockWithTimeout<T>(this T item, int timeoutInMilliseconds)
			{
				MonitorLockDisposer result = new MonitorLockDisposer(item, timeoutInMilliseconds);
				if (result._lockTaken)
					return result;

				return null;
			}
		#endregion
		#region LockWithTimeout - Action
			/// <summary>
			/// Tries to lock an object and then execute an action.
			/// Returns if the lock was obtained and the action fully executed.
			/// Be careful, as the lock is already released when the method returns.
			/// </summary>
			public static bool TryLockWithTimeout<T>(this T item, Action action)
			where
				T: class
			{
				return TryLockWithTimeout(item, LockConfiguration.DefaultLockTimeout, action);
			}

			/// <summary>
			/// Tries to lock an object and then execute an action.
			/// Returns if the lock was obtained and the action fully executed.
			/// Be careful, as the lock is already released when the method returns.
			/// </summary>
			public static bool TryLockWithTimeout<T>(this T item, int timeoutInMilliseconds, Action action)
			where
				T: class
			{
				return _TryLockWithTimeout(item, timeoutInMilliseconds, action);
			}

			private static bool _TryLockWithTimeout(object item, int timeoutInMilliseconds, Action action)
			{
				if (action == null)
					throw new ArgumentNullException("action");

				if (timeoutInMilliseconds < 0)
					throw new ArgumentException("timeout can't be less than zero.", "timeoutInMilliseconds");
					
				#if DEBUG
					if (item is ReaderWriterLock || item is ReaderWriterLockSlim)
						throw new ArgumentException("You are trying to use a monitor lock over a ReaderWriterLock. You must use its methods to lock it.");
				#endif

				#if CHECK_FOR_DEAD_LOCKS
					bool addedToLockConfiguration = false;
					try
					{
						try
						{
						}
						finally
						{
							LockConfiguration.AddLock(item);
							addedToLockConfiguration = true;
						}
				#endif

				bool lockAcquired = false;
				try
				{
					Monitor.TryEnter(item, timeoutInMilliseconds, ref lockAcquired);
				}
				finally
				{
					if (lockAcquired)
					{
						try
						{
							action();
						}
						finally
						{
							Monitor.Exit(item);
						}
					}
				}

				return lockAcquired;
				#if CHECK_FOR_DEAD_LOCKS
					}
					finally
					{
						if (addedToLockConfiguration)
							LockConfiguration.RemoveLock(item);
					}
				#endif
			}
		#endregion
		
		#region Lock
			/// <summary>
			/// Locks the actual object and returns a Disposable object.
			/// You can, then, dispose it before the end of the block is
			/// achieved with using, but without calling excessive exits.
			/// This is not abort-safe.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public static MonitorLockDisposer Lock(this object item)
			{
				MonitorLockDisposer result = new MonitorLockDisposer(item, Timeout.Infinite);
				return result;
			}

			/// <summary>
			/// Locks an object and executes the given action.
			/// The only advantage of this command over the "lock" reserved word is that, in debug,
			/// it also checks for dead-locks.
			/// </summary>
			public static void Lock(this object objectToLock, Action action)
			{
				if (objectToLock == null)
					throw new ArgumentNullException("objectToLock");
				
				if (action == null)
					throw new ArgumentNullException("action");
			
				#if CHECK_FOR_DEAD_LOCKS
					bool addedToLockConfiguration = false;
					try
					{
						try
						{
						}
						finally
						{
							LockConfiguration.AddLock(objectToLock);
							addedToLockConfiguration = true;
						}
				#endif

				lock(objectToLock)
					action();

				#if CHECK_FOR_DEAD_LOCKS
					}
					finally
					{
						if (addedToLockConfiguration)
							LockConfiguration.RemoveLock(objectToLock);
					}
				#endif
			}
			
			/// <summary>
			/// Locks an object and executes the given action in an unabortable manner.
			/// Aborts will happen or before the lock acquisition, or after the full block
			/// is executed and the lock is released.
			/// </summary>
			public static void UnabortableLock(this object objectToLock, Action action)
			{
				if (objectToLock == null)
					throw new ArgumentNullException("objectToLock");
					
				if (action == null)
					throw new ArgumentNullException("action");
				
				#if CHECK_FOR_DEAD_LOCKS
					bool addedToLockConfiguration = false;
					try
					{
						try
						{
						}
						finally
						{
							LockConfiguration.AddLock(objectToLock);
							addedToLockConfiguration = true;
						}
				#endif

				lock(objectToLock)
					AbortSafe.Run(action);
					
				#if CHECK_FOR_DEAD_LOCKS
					}
					finally
					{
						if (addedToLockConfiguration)
							LockConfiguration.RemoveLock(objectToLock);
					}
				#endif
			}
			
		#endregion
	}
}
