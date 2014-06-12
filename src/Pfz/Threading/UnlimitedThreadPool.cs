using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Pfz.Caching;
using Pfz.Extensions.MonitorLockExtensions;

namespace Pfz.Threading
{
	/// <summary>
	/// This is a thread pool that, different from System.Threading.ThreadPool,
	/// does not have a thread limit and the threads are not background while
	/// they run. Comparing to the system ThreadPool, it is better if the
	/// thread can enter in wait mode. This happens when one thread has a 
	/// "fast" process, but can only terminate after another thread returns.
	/// 
	/// This thread pool keeps threads alive for a certain number of generations.
	/// The default value is two. So, at the first use, it lives for one more
	/// generation. After the second use in that generation, it is marked to 
	/// live for two generations. Subsequent uses in this generation will not
	/// change anything. Do not use very high values, as this can cause memory
	/// usage problems.
	/// </summary>
	public static class UnlimitedThreadPool
	{
		#region Private fields
			private static object _lock = new object();
			private static volatile List<Parameters> _freeEvents = new List<Parameters>();
		#endregion
		
		#region Constructor
			[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
			static UnlimitedThreadPool()
			{
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region _Collected
			private static void _Collected()
			{
				try
				{
					int minimumCollectionNumber = GC.CollectionCount(GC.MaxGeneration);
					List<Parameters> freeEvents;
					
					_lock.UnabortableLock
					(
						delegate
						{
							freeEvents = _freeEvents;
							
							var newFreeEvents = new List<Parameters>();
							foreach(Parameters p in freeEvents)
							{
								if (p.KeepAliveUntilCollectionOfNumber >= minimumCollectionNumber)
									newFreeEvents.Add(p);
								else
								{
									p.Action = null;
									p.WaitEvent.Set();
								}
							}
							_freeEvents = newFreeEvents;
						}
					);
				}
				catch
				{
				}
			}
		#endregion
		
		#region Properties
			#region CollectionsToKeepAlive
				private static volatile int _collectionsToKeepAlive = 2;
				
				/// <summary>
				/// Gets or sets the maximum number of collections to keep a Thread from this pool
				/// alive. The default value is two.
				/// </summary>
				[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
				public static int CollectionsToKeepAlive
				{
					get
					{
						return _collectionsToKeepAlive;
					}
					set
					{
						if (value <= 0)
							throw new ArgumentException("value must be greater than zero.", "UnlimitedThreadPool.CollectionsToKeepAlive");
							
						_collectionsToKeepAlive = value;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region Run
				/// <summary>
				/// Runs an action in another thread. Uses an existing thread if one is 
				/// available or creates a new one if none are available, so this call will
				/// not hang if there are no available threads.
				/// </summary>
				/// <param name="action">The function to execute.</param>
				public static void Run(Action action)
				{
					Run
					(
						delegate(object obj)
						{
							action();
						},
						null
					);
				}
				
				/// <summary>
				/// Runs an action in another thread. Uses an existing thread if one is 
				/// available or creates a new one if none are available, so this call will
				/// not hang if there are no available threads.
				/// </summary>
				/// <param name="action">The function to execute.</param>
				/// <param name="parameter">The object passed as parameter to the action.</param>
				public static void Run(Action<object> action, object parameter)
				{
					if (action == null)
						throw new ArgumentNullException("action");
				
					Parameters p = null;
					_lock.Lock
					(
						delegate
						{
							int count = _freeEvents.Count;
							if (count > 0)
							{
								int index = count - 1;
								p = _freeEvents[index];
								_freeEvents.RemoveAt(index);
							}
						}
					);
							
					if (p == null)
					{
						p = new Parameters();
						p.WaitEvent = new ManualResetEvent(false);
						Thread thread = new Thread(_RunThread);
						p.Thread = thread;
						thread.Start(p);
					}

					p.Action = action;
					p.Parameter = parameter;
					
					p.Thread.IsBackground = false;
					p.WaitEvent.Set();
				}
				
				/// <summary>
				/// Runs an action in another thread. Uses an existing thread if one is 
				/// available or creates a new one if none are available, so this call will
				/// not hang if there are no available threads.
				/// </summary>
				/// <typeparam name="T">The type of the parameter.</typeparam>
				/// <param name="action">The function to execute.</param>
				/// <param name="parameter">The object passed as parameter to the action.</param>
				public static void Run<T>(Action<T> action, T parameter)
				{
					Run
					(
						delegate(object obj)
						{
							T typedParameter = (T)obj;
							action(typedParameter);
						},
						parameter
					);
				}
			#endregion
			
			#region _RunThread
				private static void _RunThread(object parameters)
				{
					Thread currentThread = Thread.CurrentThread;
					Parameters p = (Parameters)parameters;
					ManualResetEvent waitEvent = p.WaitEvent;
					
					try
					{
						while(true)
						{
							waitEvent.WaitOne();
							
							if (p.Action == null)
							{
								waitEvent.Close();
								return;
							}
							
							p.Action(p.Parameter);

							_lock.Lock
							(
								delegate
								{
									int actualCollectionNumber = GC.CollectionCount(GC.MaxGeneration);
									int keepAliveUntil = p.KeepAliveUntilCollectionOfNumber;
									
									if (keepAliveUntil <= actualCollectionNumber)
										p.KeepAliveUntilCollectionOfNumber = actualCollectionNumber + 1;
									else
									{
										int maxValue = actualCollectionNumber + _collectionsToKeepAlive;
										if (keepAliveUntil < maxValue)
											p.KeepAliveUntilCollectionOfNumber = keepAliveUntil + 1;
									}

									currentThread.IsBackground = true;
									waitEvent.Reset();
									_freeEvents.Add(p);
								}
							);
						}
					}
					finally
					{
						if (currentThread.IsBackground)
						{
							_lock.Lock
							(
								() => _freeEvents.Remove(p)
							);
						}
					}
				}
			#endregion
		#endregion

		#region Parameters - Nested class
			private sealed class Parameters
			{
				internal Thread Thread;
				internal ManualResetEvent WaitEvent;
				internal volatile Action<object> Action;
				internal volatile object Parameter;
				internal volatile int KeepAliveUntilCollectionOfNumber;
			}
		#endregion
	}
}
