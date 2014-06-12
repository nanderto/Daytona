using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// Some methods and events to interact with garbage collection. You can 
	/// keep an object alive during the next collection or register to know 
	/// when a collection has just happened. This is useful if you don't use
	/// WeakReferences, but know how to free memory if needed. For example, 
	/// you can do a TrimExcess to your lists to free some memory.
	/// 
	/// Caution: GC.KeepAlive keeps the object alive until that line of code,
	/// while GCUtils.KeepAlive keeps the object alive until the next 
	/// generation.
	/// </summary>
	public static class GCUtils
    {
		#region Constructor
			private static bool _finished;
			private static ManualResetEvent _collectedEvent = new ManualResetEvent(false);
			[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
			static GCUtils()
			{
				AppDomain current = AppDomain.CurrentDomain;
				current.DomainUnload += new EventHandler(_DomainUnload);
				current.ProcessExit += new EventHandler(_ProcessExit);
				var runner = new Runner();
				runner.DoNothing();
				
				Thread collectorThread = new Thread(_ExecuteCollected);
				collectorThread.Name = "Pfz.Caching.GCUtils.Collected executor thread.";
				collectorThread.Priority = ThreadPriority.AboveNormal;
				collectorThread.Start();
			}
		#endregion
		#region Finalization event handles
			private static void _ProcessExit(object sender, EventArgs e)
			{
				_finished = true;
				_collectedEvent.Set();
			}
			private static void _DomainUnload(object sender, EventArgs e)
			{
				_finished = true;
				_collectedEvent.Set();
			}
		#endregion
		
		#region ProcessMemory
			private static long _processMemory;
			
			/// <summary>
			/// Gets or sets a value that indicates how much memory the process
			/// can use without freeing it's caches.
			/// Note that such value does not affect how often GC occurs and is
			/// not the size of the cache, it only says: If my process is not using
			/// more than X memory, caches don't need to be erased.
			/// The default value is 0 mb.
			/// </summary>
			public static long ProcessMemory
			{
				get
				{
					return _processMemory;
				}
				set
				{
					_processMemory = value;
				}
			}
		#endregion
    
		#region KeepAlive
			private static HashSet<object> _keepedObjects = new HashSet<object>(ReferenceComparer.Instance);
			
			/// <summary>
			/// Keeps an object alive at the next collection. This is useful for WeakReferences as an way
			/// to guarantee that recently used objects will not be immediatelly collected. At the next
			/// generation, you can call KeepAlive again, making the object alive for another generation.
			/// </summary>
			/// <param name="item"></param>
			public static void KeepAlive(object item)
			{
				if (item == null)
					return;
			
				var keepedObjects = _keepedObjects;
				
				lock(keepedObjects)
					AbortSafe.Run(() => keepedObjects.Add(item));
			}
			
			/// <summary>
			/// Expires an object. Is the opposite of KeepAlive.
			/// </summary>
			/// <param name="item"></param>
			/// <returns>true if the object was in the KeepAlive list, false otherwise.</returns>
			public static bool Expire(object item)
			{
				if (item == null)
					return false;
			
				var keepedObjects = _keepedObjects;

				bool result = false;
				lock(keepedObjects)
					AbortSafe.Run(() => result = keepedObjects.Remove(item));

				return result;
			}
		#endregion
		#region _ExecuteCollected
			private static void _ExecuteCollected()
			{
				var thread = Thread.CurrentThread;
				while(true)
				{
					// we are background while waiting.
					thread.IsBackground = true;
					
					_collectedEvent.WaitOne();
					
					if (_finished)
						return;
						
					_collectedEvent.Reset();
					
					// but we are not background while running.
					thread.IsBackground = false;
					
					_ExecuteCollectedNow();
				}
			}
		#endregion
		#region _ExecuteCollectedNow
			private static void _ExecuteCollectedNow()
			{
				// no lock is needed for the keeped objects,
				// as we simple put a new object and don't
				// even try to read the old object.
				_keepedObjects = new HashSet<object>(ReferenceComparer.Instance);

				List<InternalWeakDelegate> listToRun = null;
				try
				{
					List<List<InternalWeakDelegate>> availLater = new List<List<InternalWeakDelegate>>();
					lock(_collectedLock)
					{
						var oldCollected = _collected;
						listToRun = new List<InternalWeakDelegate>(oldCollected.Count);
						var newCollected = new Dictionary<int, List<InternalWeakDelegate>>(oldCollected.Count);
						foreach(var pair in oldCollected)
						{
							var oldList = pair.Value;
							lock(oldList)
							{
								if (oldList.Count > 0)
								{
									newCollected.Add(pair.Key, oldList);
									availLater.Add(oldList);
								}
							}
						}
						
						_collected = newCollected;
					}
					
					foreach(var list in availLater)
					{
						lock(list)
						{
							AbortSafe.Run
							(
								delegate
								{
									int count = list.Count;
									for(int i=count-1; i>=0; i--)
									{
										InternalWeakDelegate weakDelegate = list[i];
										
										if (weakDelegate.Target == null && !weakDelegate.Method.IsStatic)
											list.RemoveAt(i);
										else
											listToRun.Add(weakDelegate);
									}
									
									list.TrimExcess();
								}
							);
						}
					}
				}
				catch
				{
				}
				
				// we use the listToRun instead of a foreach in each list as an way to avoid creating many objects,
				// which could (in very rare circunstances) throw an exception.
				// If an exception was thrown, no problem. If we have any item in
				// list, will try to run such item.
				if (listToRun != null)
				{
					int countListToRun = listToRun.Count;
					for (int i=0; i<countListToRun; i++)
					{
						InternalWeakDelegate action = listToRun[i];
						action.Invoke(null);
					}
				}
			}
		#endregion
		
        #region Collected
			private static volatile Dictionary<int, List<InternalWeakDelegate>> _collected = new Dictionary<int, List<InternalWeakDelegate>>();
			private static object _collectedLock = new object();
			
			/// <summary>
			/// This event is called after a GarbageCollection has just finished,
			/// in another thread. As this happens after the collection has finished,
			/// all other threads are running too, so you must guarantee that
			/// your event is thread safe.
			/// </summary>
			public static event Action Collected
			{
				add
				{
					if (value == null)
						throw new ArgumentNullException("value");

					int hashCode = value.GetHashCode();
				
					bool mustReturn = false;
					List<InternalWeakDelegate> list = null;
					lock(_collectedLock)
					{
						AbortSafe.Run
						(
							delegate
							{
								var collected = _collected;
								
								// if there is no item with the same hashCode, we
								// can insert a new one directly.
								if (!collected.TryGetValue(hashCode, out list))
								{
									InternalWeakDelegate weakDelegate = new InternalWeakDelegate(value);
									list = new List<InternalWeakDelegate>(1);
									list.Add(weakDelegate);
									collected.Add(hashCode, list);
									mustReturn = true;
								}
							}
						);
					}
					
					if (mustReturn)
						return;
					
					// ok, an item with the same hashCode exists, so
					// first we check if this item is already in the list.
					// if there is, we simple return.
					lock(list)
					{
						AbortSafe.Run
						(
							delegate
							{
								foreach(InternalWeakDelegate action in list)
									if (action.Target == value.Target && action.Method == value.Method)
										return;
								
								InternalWeakDelegate weakDelegate = new InternalWeakDelegate(value);
								list.Add(weakDelegate);
							}
						);
					}
				}
				remove
				{
					if (value == null)
						throw new ArgumentNullException("value");

					int hashCode = value.GetHashCode();
				
					List<InternalWeakDelegate> list = null;
					
					lock(_collectedLock)
						_collected.TryGetValue(hashCode, out list);
					
					if (list == null)
						return;
					
					lock(list)
					{
						int count = list.Count;
						for(int i=0; i<count; i++)
						{
							InternalWeakDelegate weakDelegate = list[i];
							if (weakDelegate.Method == value.Method && weakDelegate.Target == value.Target)
							{
								AbortSafe.Run(() => list.RemoveAt(i));
								return;
							}
						}
					}
				}
			}
        #endregion
        
        #region Runner - nested class
			private sealed class Runner
			{
				[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
				public void DoNothing()
				{
				}

				~Runner()
				{
					// If we don't test, we will keep re-registering forever
					// when the application is finishing.
					if (_finished)
						return;
						
					GC.ReRegisterForFinalize(this);
					
					if (GC.GetTotalMemory(false) <= _processMemory)
						return;
					
					_collectedEvent.Set();
				}
			}
        #endregion
    }
}
