using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Pfz.Caching;
using Pfz.Collections;
using Pfz.DataTypes;
using Pfz.Extensions;

namespace Pfz.Threading
{
	/// <summary>
	/// This class only configures what to do when a dead-lock occurs while
	/// acquiring a lock with PfzMonitorLockExtensions or with 
	/// PfzReaderWriterLockExtensions.
	/// </summary>
	public static class LockConfiguration
	{
		#region DefaultLockTimeout
			private static int _defaultLockTimeout = 60 * 1000;
			
			/// <summary>
			/// Gets or sets the default timeout for LockWithTimeout method.
			/// This value is in milliseconds.
			/// </summary>
			public static int DefaultLockTimeout
			{
				get
				{
					return _defaultLockTimeout;
				}
				set
				{
					if (value < 0)
						throw new ArgumentException("DefaultLockTimeout must be greater or equal to zero.");
						
					_defaultLockTimeout = value;
				}
			}
		#endregion
		#region DeadLockLogPath
			private static string _deadLockLogPath = "C:\\Temp\\Pfz.Threading.Lock_Dead\\";
			
			/// <summary>
			/// Gets or sets a directory path in with log files will be generated when/if
			/// LockWithTimeout times out.
			/// </summary>
			[SuppressMessage("Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength")]
			public static string DeadLockLogPath
			{
				get
				{
					return _deadLockLogPath;
				}
				set
				{
					if (value != null)
					{
						if (value == "")
							throw new ArgumentException("DeadLockLogPath can't be an empty string.");
							
						if (value[value.Length-1] != '\\')
							value += '\\';
					}
					
					_deadLockLogPath = value;
				}
			}
		#endregion
		
		#region DeadLock detections - CHECK_FOR_DEAD_LOCKS must be set
			#if CHECK_FOR_DEAD_LOCKS
				internal static void _LockValidationException(string message, bool showOurStackTrace)
				{
					string logPath = _deadLockLogPath;
					string filePath = null;
					if (logPath != null)
					{
						try
						{
							if (!Directory.Exists(logPath))
								Directory.CreateDirectory(logPath);
							
							DateTime now = DateTime.Now;
							filePath = logPath + now.ToString("yyyy-MM-dd") + ".log";
							
							if (showOurStackTrace)
							{
								var stackTrace = new StackTrace(true);
								File.AppendAllText(filePath, now.ToString("HH:mm:ss") + " " + message + "\r\nOur stack trace:" + stackTrace.ToString() + "\r\n\r\n");
							}
							else
								File.AppendAllText(filePath, now.ToString("HH:mm:ss") + " " + message + "\r\n\r\n");
						}
						catch
						{
						}
					}
				
					if (filePath != null && message.Length > 980)
						message = message.Substring(0, 950) + "\r\n\r\n***To see complete details, see the log file: " + filePath;
						
					throw new ArgumentException(message);
				}
				
				private struct LockPath
				{
					public readonly ImmutableArray<long> Array;
					public readonly StackTrace StackTrace;
					
					public LockPath(ImmutableArray<long> array, StackTrace stackTrace)
					{
						Array = array;
						StackTrace = stackTrace;
					}

					public override int GetHashCode()
					{
						return Array.GetHashCode();
					}
					public override bool Equals(object obj)
					{
						return ((LockPath)obj).Array.Equals(Array);
					}
				}
				
				// Can't use a WeakDictionary here, as it will end-up calling those methods.
				private static readonly Dictionary<long, WeakReference> _objectIds2 = new Dictionary<long, WeakReference>();

				private static readonly ConditionalWeakTable<object, Box<long>> _objectIds = new ConditionalWeakTable<object, Box<long>>();
				private static readonly Dictionary<Thread, HashSet<LockPath>> _allLocksByThread = new Dictionary<Thread, HashSet<LockPath>>();
				
				[ThreadStatic]
				private static ImmutableArray<long> _lockPath = new ImmutableArray<long>();
				static LockConfiguration()
				{
					Thread thread = new Thread(_DeadLockMonitor);
					thread.Name = "Pfz.Threading.LockConfiguration - Dead Lock Monitor";
					thread.Start();
				}
				private static void _DeadLockMonitor()
				{
					StringBuilder errorMessage = new StringBuilder();
					var thread = Thread.CurrentThread;
					while(true)
					{
						thread.IsBackground = true;
						Thread.Sleep(10000);
						thread.IsBackground = false;
						
						List<HashSet<LockPath>> allLockPathsHash;
						lock(_allLocksByThread)
							allLockPathsHash = _allLocksByThread.Values.ToList();
						
						HashSet<long> idsHash = new HashSet<long>();
						int lockPathsCount = allLockPathsHash.Count;
						List<List<LockPath>> allLockPaths = new List<List<LockPath>>(lockPathsCount);
						foreach(var paths in allLockPathsHash)
						{
							List<LockPath> list = new List<LockPath>(paths.Count);

							lock(paths)
							{
								foreach(var oldLockPath in paths)
								{
									idsHash.Clear();
									foreach(long id in oldLockPath.Array)
										idsHash.Add(id); // this will guarantee that lock obtained more tha once will appear only once.
										
									if (idsHash.Count < 2)
										continue;
									
									LockPath newLockPath = new LockPath
									(
										new ImmutableArray<long>(idsHash),
										oldLockPath.StackTrace
									);
									
									list.Add(newLockPath);
								}
							}
								
							allLockPaths.Add(list);
						}
							
						for(int lockPathsIndex1=0; lockPathsIndex1<lockPathsCount; lockPathsIndex1++)
						{
							List<LockPath> list1 = allLockPaths[lockPathsIndex1];
							for(int lockPathsIndex2=lockPathsIndex1; lockPathsIndex2<lockPathsCount; lockPathsIndex2++)
							{
								List<LockPath> list2 = allLockPaths[lockPathsIndex2];
								foreach(var path1 in list1)
								{
									var array1 = path1.Array;
									foreach(var path2 in list2)
									{
										var array2 = path2.Array;
										
										int count = array2.Length;
										int countMinus1 = count-1;
										for (int i=0; i<countMinus1; i++)
										{
											long firstLock = array2[i];
											
											int indexInFirst = array1.IndexOf(firstLock);
											if (indexInFirst == -1)
												continue;
											
											for(int j=i+1; j<count; j++)
											{
												long secondLock = array2[j];
												
												int otherIndexInFirst = array1.IndexOf(secondLock);
												if (otherIndexInFirst == -1)
													continue;
												
												if (otherIndexInFirst < indexInFirst)
												{
													errorMessage.Append("DeadLock\r\nStack Trace 1:");
													errorMessage.Append(path1.StackTrace);
													errorMessage.Append("Stack Trace2:");
													errorMessage.Append(path2.StackTrace);
												}
											}
										}
									}
								}
							}
						}
						
						if (errorMessage.Length > 0)
						{
							string error = errorMessage.ToString();
							_LockValidationException(error, false);
						}
							
						List<LockPath> pathsToRemove = new List<LockPath>();
						lock(_objectIds)
						{
							foreach(var paths in allLockPathsHash)
							{
								pathsToRemove.Clear();
							
								lock(paths)
								{
									foreach(var path in paths)
									{
										int length = path.Array.Length;
										for(int i=0; i<length; i++)
										{
											long id = path.Array[i];

											WeakReference reference;
											if (_objectIds2.TryGetValue(id, out reference))
											{
												if (reference.Target == null)
												{
													_objectIds2.Remove(id);
													pathsToRemove.Add(path);
													break;
												}
											}
											else
											{
												pathsToRemove.Add(path);
												break;
											}
										}
									}

									foreach(var path in pathsToRemove)
										paths.Remove(path);
								}
							}
						}
						
						lock(_allLocksByThread)
						{
							List<Thread> deadThreads = new List<Thread>();
							foreach(var possibleDead in _allLocksByThread.Keys)
								if (!possibleDead.IsAlive)
									deadThreads.Add(possibleDead);
							
							foreach(var deadThread in deadThreads)
								_allLocksByThread.Remove(deadThread);
						}
					}
				}
				
				private static long _objectIdGenerator;
				internal static void AddLock(object lockObject)
				{
					if (lockObject == null)
						throw new ArgumentNullException("lockObject");

					long objectId;
					lock(_objectIds)
					{
						Box<long> box;
						if (_objectIds.TryGetValue(lockObject, out box))
							objectId = box.Value;
						else
						{
							objectId = Interlocked.Increment(ref _objectIdGenerator);
							_objectIds.Add(lockObject, new Box<long>(objectId));
							_objectIds2.Add(objectId, new WeakReference(lockObject));
						}
					}
					
					var newLockPath = _lockPath.Add(objectId);
					
					if (newLockPath.Length > 1)
					{
						var thread = Thread.CurrentThread;
						HashSet<LockPath> locksOfThisThread;
						lock(_allLocksByThread)
						{
							locksOfThisThread = _allLocksByThread.GetValueOrDefault(thread);
							if (locksOfThisThread == null)
							{
								locksOfThisThread = new HashSet<LockPath>();
								_allLocksByThread.Add(thread, locksOfThisThread);
							}
						}
						lock(locksOfThisThread)
						{
							locksOfThisThread.Remove
							(
								new LockPath
								(
									_lockPath,
									null
								)
							);
							
							locksOfThisThread.Add
							(
								new LockPath
								(
									newLockPath,
									new StackTrace(3, true)
								)
							);
						}
					}
					
					_lockPath = newLockPath;
				}
				internal static void RemoveLock(object lockObject)
				{
					if (lockObject == null)
						throw new ArgumentNullException("lockObject");
					
					if (_lockPath.Length == 0)
						_LockValidationException("You are trying to release a lock, but you hold none.", true);
					
					long objectId = 0;
					lock(_objectIds)
					{
						Box<long> box;
						if (_objectIds.TryGetValue(lockObject, out box))
							objectId = box.Value;
						else
							_LockValidationException("The object you are trying to unlock was never locked.", true);
					}

					if (_lockPath[_lockPath.Length-1] != objectId)
						_LockValidationException("You are trying to release a lock that is not your last lock.", true);
						
					_lockPath = _lockPath.RemoveLast();
				}
			#endif
		#endregion
		#region ThrowLockDisposerException - DEBUG only
			#if DEBUG
				internal static void ThrowLockDisposerException(string lockName, Thread thread, StackTrace stackTrace)
				{
					if (thread == null)
						return;

					switch(thread.ThreadState)
					{
						case System.Threading.ThreadState.AbortRequested:
						case System.Threading.ThreadState.Aborted:
							throw new SynchronizationLockException("Lock not released error on thread: \"" + thread.Name + "\".\r\nThis was probably caused by a Thread.Abort(). You must consider calling a version of " + lockName + "() that accepts an action to make your code abort-safe.\r\nStack Trace:\r\n" + stackTrace);

						default:
							throw new SynchronizationLockException(lockName + " not released error on thread: \"" + thread.Name + "\".\r\nYou probably forgot to Dispose() the returned lock-holder.\r\nStack Trace:\r\n" + stackTrace);
					}
				}
			#else
				internal static void ThrowLockDisposerException(string lockName)
				{
					throw new SynchronizationLockException(lockName + " not released error. This could be caused because you forgot to call Dispose() on the lock holder or by a Thread.Abort(). If it is the second case, consider using the " + lockName + "() that accepts an action, which is abort-safe.");
				}
			#endif
		#endregion
	}
}
