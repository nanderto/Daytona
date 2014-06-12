using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Pfz.Threading;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds methods to use ReaderWriterLockSlim easily,
	/// always with time-outs to avoid dead-locks.
	/// See PfzLockConfiguration class if you want to log dead-locks.
	/// </summary>
	public static class PfzReaderWriterLockExtensions
	{
		#region IDisposable locks
			#region ReadLock
				/// <summary>
				/// Abort-unsafe version of read-lock.
				/// Must be disposed to release the lock.
				/// </summary>
				[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
				public static ReadLockDisposer ReadLock(this ReaderWriterLockSlim readerWriterLock)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					ReadLockDisposer result = new ReadLockDisposer(readerWriterLock, Timeout.Infinite);
					return result;
				}
			
				/// <summary>
				/// Tries to acquire a read-lock on the given object using the default timeout. 
				/// If it fails, returns null.
				/// </summary>
				/// <param name="readerWriterLock">The object to lock.</param>
				/// <returns>A disposable object to release the lock, or null.</returns>
				public static ReadLockDisposer TryReadLock(this ReaderWriterLockSlim readerWriterLock)
				{
					return TryReadLock(readerWriterLock, LockConfiguration.DefaultLockTimeout);
				}
				
				/// <summary>
				/// Tries to acquire a read-lock on the given object using the specified timeout. 
				/// If it fails, returns null.
				/// </summary>
				/// <param name="readerWriterLock">The object to lock.</param>
				/// <param name="timeoutInMilliseconds">The timeout to try for the lock.</param>
				/// <returns>A disposable object to release the lock, or null.</returns>
				[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
				public static ReadLockDisposer TryReadLock(this ReaderWriterLockSlim readerWriterLock, int timeoutInMilliseconds)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					ReadLockDisposer result = new ReadLockDisposer(readerWriterLock, timeoutInMilliseconds);
					if (result._readerWriterLock != null)
						return result;
					
					return null;
				}
			#endregion
			#region UpgradeableLock
				/// <summary>
				/// Abort-unsafe version of upgradeable-lock.
				/// Must be disposed to release the lock.
				/// </summary>
				[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
				public static UpgradeableLockDisposer UpgradeableLock(this ReaderWriterLockSlim readerWriterLock)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					UpgradeableLockDisposer result = new UpgradeableLockDisposer(readerWriterLock, -1);
					return result;
				}

				/// <summary>
				/// Tries to acquire an upgradeable lock on the given object, using the default timeout.
				/// If it fails, returns null.
				/// </summary>
				/// <param name="readerWriterLock">The object to try to lock.</param>
				/// <returns>An disposable object to release the lock, or null if the locks fails.</returns>
				public static UpgradeableLockDisposer TryUpgradeableLock(this ReaderWriterLockSlim readerWriterLock)
				{
					return TryUpgradeableLock(readerWriterLock, LockConfiguration.DefaultLockTimeout);
				}
				
				/// <summary>
				/// Tries to acquire an upgradeable lock on the given object, using the specified timeout.
				/// If it fails, returns null.
				/// </summary>
				/// <param name="readerWriterLock">The object to try to lock.</param>
				/// <param name="timeoutInMilliseconds">The maximum time to wait for the lock.</param>
				/// <returns>An disposable object to release the lock, or null if the locks fails.</returns>
				[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
				public static UpgradeableLockDisposer TryUpgradeableLock(this ReaderWriterLockSlim readerWriterLock, int timeoutInMilliseconds)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					UpgradeableLockDisposer result = new UpgradeableLockDisposer(readerWriterLock, timeoutInMilliseconds);
					if (result._readerWriterLock != null)
						return result;
					
					return null;
				}
			#endregion
			#region WriteLock
				/// <summary>
				/// Abort-unsafe version of write-lock.
				/// Must be disposed to release the lock.
				/// </summary>
				[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
				public static WriteLockDisposer WriteLock(this ReaderWriterLockSlim readerWriterLock)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					WriteLockDisposer result = new WriteLockDisposer(readerWriterLock, -1);
					return result;
				}

				/// <summary>
				/// Tries to acquire a write-lock on the given object using the default timeout.
				/// If it fails, returns null.
				/// </summary>
				/// <param name="readerWriterLock">The object to lock.</param>
				/// <returns>A disposable object to release the lock, or null.</returns>
				public static WriteLockDisposer TryWriteLock(this ReaderWriterLockSlim readerWriterLock)
				{
					return TryWriteLock(readerWriterLock, LockConfiguration.DefaultLockTimeout);
				}
				
				/// <summary>
				/// Tries to acquire a write-lock on the given object using the specified timeout.
				/// If it fails, returns null.
				/// </summary>
				/// <param name="readerWriterLock">The object to lock.</param>
				/// <param name="timeoutInMilliseconds">The maximum time to wait for the lock.</param>
				/// <returns>A disposable object to release the lock, or null.</returns>
				[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
				public static WriteLockDisposer TryWriteLock(this ReaderWriterLockSlim readerWriterLock, int timeoutInMilliseconds)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					WriteLockDisposer result = new WriteLockDisposer(readerWriterLock, timeoutInMilliseconds);
					if (result._readerWriterLock != null)
						return result;
					
					return null;
				}
			#endregion
		#endregion
		#region AbortSafe locks (use actions)
			#region ReadLock
				/// <summary>
				/// Acquires a read-lock and then runs the action.
				/// The lock is abort safe, not the executed block.
				/// </summary>
				public static void ReadLock(this ReaderWriterLockSlim readerWriterLock, Action action)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");
					
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
								LockConfiguration.AddLock(readerWriterLock);
								addedToLockConfiguration = true;
							}
					#endif

					bool lockAcquired = false;
					try
					{
						while(true)
						{
							try
							{
							}
							finally
							{
								lockAcquired = readerWriterLock.TryEnterReadLock(1000);
							}
							
							if (lockAcquired)
							{
								action();
								return;
							}
						}
					}
					finally
					{
						if (lockAcquired)
							readerWriterLock.ExitReadLock();
					}

					#if CHECK_FOR_DEAD_LOCKS
						}
						finally
						{
							if (addedToLockConfiguration)
								LockConfiguration.RemoveLock(readerWriterLock);
						}
					#endif
				}

				/// <summary>
				/// Tries to acquire a ReadLock using the default timeout.
				/// If the lock is acquired, it executes the action and returns only
				/// after releasing the lock. If not, it returns false.
				/// The lock acquisition is AbortSafe.
				/// </summary>
				public static bool TryReadLock(this ReaderWriterLockSlim readerWriterLock, Action action)
				{
					return TryReadLock(readerWriterLock, LockConfiguration.DefaultLockTimeout, action);
				}

				/// <summary>
				/// Tries to acquire a ReadLock using the given timeout.
				/// If the lock is acquired, it executes the action and returns only
				/// after releasing the lock. If not, it returns false.
				/// The lock acquisition is AbortSafe.
				/// </summary>
				public static bool TryReadLock(this ReaderWriterLockSlim readerWriterLock, int timeoutInMilliseconds, Action action)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					if (action == null)
						throw new ArgumentNullException("action");

					if (timeoutInMilliseconds < 0)
						throw new ArgumentException("timeout can't be less than zero.", "timeoutInMilliseconds");

					#if CHECK_FOR_DEAD_LOCKS
						bool addedToLockConfiguration = false;
						try
						{
							try
							{
							}
							finally
							{
								LockConfiguration.AddLock(readerWriterLock);
								addedToLockConfiguration = true;
							}
					#endif

					bool lockAcquired = false;
					try
					{
						while (timeoutInMilliseconds > 0)
						{
							try
							{
							}
							finally
							{
								int timeOutToUse = 1000;
								if (timeoutInMilliseconds < 1000)
									timeOutToUse = timeoutInMilliseconds;

								lockAcquired = readerWriterLock.TryEnterReadLock(timeOutToUse);
							}

							if (lockAcquired)
							{
								action();

								return true;
							}

							if (AbortSafe.WasAbortRequested)
								return false;

							timeoutInMilliseconds -= 1000;
						}
					}
					finally
					{
						if (lockAcquired)
							readerWriterLock.ExitReadLock();
					}

					#if CHECK_FOR_DEAD_LOCKS
						}
						finally
						{
							if (addedToLockConfiguration)
								LockConfiguration.RemoveLock(readerWriterLock);
						}
					#endif
					
					return false;
				}
			#endregion
			#region UpgradeableLock
				/// <summary>
				/// Acquires an upgradeable-lock and then runs the action.
				/// The lock is abort safe, not the action.
				/// </summary>
				public static void UpgradeableLock(this ReaderWriterLockSlim readerWriterLock, Action action)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

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
								LockConfiguration.AddLock(readerWriterLock);
								addedToLockConfiguration = true;
							}
					#endif

					bool lockAcquired = false;
					try
					{
						while (true)
						{
							try
							{
							}
							finally
							{
								lockAcquired = readerWriterLock.TryEnterUpgradeableReadLock(1000);
							}

							if (lockAcquired)
							{
								action();
								return;
							}
						}
					}
					finally
					{
						if (lockAcquired)
							readerWriterLock.ExitUpgradeableReadLock();
					}

					#if CHECK_FOR_DEAD_LOCKS
						}
						finally
						{
							if (addedToLockConfiguration)
								LockConfiguration.RemoveLock(readerWriterLock);
						}
					#endif
				}

				/// <summary>
				/// Tries to acquire a UpgradeableLock using the default timeout.
				/// If the lock is acquired, it executes the action and returns only
				/// after releasing the lock. If not, it returns false.
				/// The lock acquisition is AbortSafe.
				/// </summary>
				public static bool TryUpgradeableLock(this ReaderWriterLockSlim readerWriterLock, Action action)
				{
					return TryUpgradeableLock(readerWriterLock, LockConfiguration.DefaultLockTimeout, action);
				}

				/// <summary>
				/// Tries to acquire a UpgradeableLock using the given timeout.
				/// If the lock is acquired, it executes the action and returns only
				/// after releasing the lock. If not, it returns false.
				/// The lock acquisition is AbortSafe.
				/// </summary>
				public static bool TryUpgradeableLock(this ReaderWriterLockSlim readerWriterLock, int timeoutInMilliseconds, Action action)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					if (action == null)
						throw new ArgumentNullException("action");

					if (timeoutInMilliseconds < 0)
						throw new ArgumentException("timeout can't be less than zero.", "timeoutInMilliseconds");

					#if CHECK_FOR_DEAD_LOCKS
						bool addedToLockConfiguration = false;
						try
						{
							try
							{
							}
							finally
							{
								LockConfiguration.AddLock(readerWriterLock);
								addedToLockConfiguration = true;
							}
					#endif

					bool lockAcquired = false;
					try
					{
						while (timeoutInMilliseconds > 0)
						{
							try
							{
							}
							finally
							{
								int timeOutToUse = 1000;
								if (timeoutInMilliseconds < 1000)
									timeOutToUse = timeoutInMilliseconds;

								lockAcquired = readerWriterLock.TryEnterUpgradeableReadLock(timeOutToUse);
							}

							if (lockAcquired)
							{
								action();

								return true;
							}

							if (AbortSafe.WasAbortRequested)
								return false;

							timeoutInMilliseconds -= 1000;
						}
					}
					finally
					{
						if (lockAcquired)
							readerWriterLock.ExitUpgradeableReadLock();
					}

					#if CHECK_FOR_DEAD_LOCKS
						}
						finally
						{
							if (addedToLockConfiguration)
								LockConfiguration.RemoveLock(readerWriterLock);
						}
					#endif

					return false;
				}
			#endregion
			#region WriteLock
				/// <summary>
				/// Acquires a write-lock and then runs the action.
				/// The lock is abort safe, not the action.
				/// </summary>
				public static void WriteLock(this ReaderWriterLockSlim readerWriterLock, Action action)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

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
								LockConfiguration.AddLock(readerWriterLock);
								addedToLockConfiguration = true;
							}
					#endif

					bool lockAcquired = false;
					try
					{
						while (true)
						{
							try
							{
							}
							finally
							{
								lockAcquired = readerWriterLock.TryEnterWriteLock(1000);
							}

							if (lockAcquired)
							{
								action();
								return;
							}
						}
					}
					finally
					{
						if (lockAcquired)
							readerWriterLock.ExitWriteLock();
					}

					#if CHECK_FOR_DEAD_LOCKS
						}
						finally
						{
							if (addedToLockConfiguration)
								LockConfiguration.RemoveLock(readerWriterLock);
						}
					#endif
				}

				/// <summary>
				/// Tries to acquire a WriteLock using the default timeout.
				/// If the lock is acquired, it executes the action and returns only
				/// after releasing the lock. If not, it returns false.
				/// The lock acquisition is AbortSafe.
				/// </summary>
				public static bool TryWriteLock(this ReaderWriterLockSlim readerWriterLock, Action action)
				{
					return TryWriteLock(readerWriterLock, LockConfiguration.DefaultLockTimeout, action);
				}

				/// <summary>
				/// Tries to acquire a WriteLock using the given timeout.
				/// If the lock is acquired, it executes the action and returns only
				/// after releasing the lock. If not, it returns false.
				/// The lock acquisition is AbortSafe.
				/// </summary>
				public static bool TryWriteLock(this ReaderWriterLockSlim readerWriterLock, int timeoutInMilliseconds, Action action)
				{
					if (readerWriterLock == null)
						throw new ArgumentNullException("readerWriterLock");

					if (action == null)
						throw new ArgumentNullException("action");
					
					if (timeoutInMilliseconds < 0)
						throw new ArgumentException("timeout can't be less than zero.", "timeoutInMilliseconds");

					#if CHECK_FOR_DEAD_LOCKS
						bool addedToLockConfiguration = false;
						try
						{
							try
							{
							}
							finally
							{
								LockConfiguration.AddLock(readerWriterLock);
								addedToLockConfiguration = true;
							}
					#endif

					bool lockAcquired = false;
					try
					{
						while (timeoutInMilliseconds > 0)
						{
							try
							{
							}
							finally
							{
								int timeOutToUse = 1000;
								if (timeoutInMilliseconds < 1000)
									timeOutToUse = timeoutInMilliseconds;

								lockAcquired = readerWriterLock.TryEnterWriteLock(timeOutToUse);
							}

							if (lockAcquired)
							{
								action();

								return true;
							}

							if (AbortSafe.WasAbortRequested)
								return false;

							timeoutInMilliseconds -= 1000;
						}
					}
					finally
					{
						if (lockAcquired)
							readerWriterLock.ExitWriteLock();
					}

					#if CHECK_FOR_DEAD_LOCKS
						}
						finally
						{
							if (addedToLockConfiguration)
								LockConfiguration.RemoveLock(readerWriterLock);
						}
					#endif

					return false;
				}
			#endregion
		#endregion
	}
}
