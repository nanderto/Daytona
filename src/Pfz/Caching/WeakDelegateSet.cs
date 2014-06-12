using System;
using System.Collections.Generic;
using System.Linq;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// This class acts as a hashset for delegates, but allows the
	/// Targets to be collected, working as if the Delegates where 
	/// WeakDelegates.
	/// The original idea was to make this a generic class, but it
	/// is not possible to use Delegate as a constraint (where T: Delegate).
	/// 
	/// To use, implement your event like:
	/// private WeakDelegateSet fMyEvent = new WeakDelegateSet();
	/// public event EventHandler MyEvent
	/// {
	///		add
	///		{
	///			fMyEvent.Add(value);
	///		}
	///		remove
	///		{
	///			fMyEvent.Remove(value);
	///		}
	///		
	/// And when you want to invoke MyEvent, you call:
	///		fMyEvent.Invoke(this, EventArgs.Empty);
	/// }
	/// </summary>
	public class WeakDelegateSet:
		ThreadSafeDisposable
	{
		#region Private dictionary
			private volatile Dictionary<int, List<InternalWeakDelegate>> _dictionary = new Dictionary<int, List<InternalWeakDelegate>>();
		#endregion
	
		#region Constructor
			/// <summary>
			/// Creates a new WeakDelegateSet.
			/// </summary>
			public WeakDelegateSet()
			{
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Unregisters the WeakDelegateSet from GCUtils.Collected.
			/// </summary>
			/// <param name="disposing"></param>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
					GCUtils.Collected -= _Collected;
					
				base.Dispose(disposing);
			}
		#endregion
		#region _Collected
			private void _Collected()
			{
				try
				{
					DisposeLock.Lock
					(
						delegate
						{
							if (WasDisposed)
							{
								GCUtils.Collected -= _Collected;
								return;
							}
							
							var oldDictionary = _dictionary;
							var newDictionary = new Dictionary<int, List<InternalWeakDelegate>>();
							
							foreach(var pair in oldDictionary)
							{
								List<InternalWeakDelegate> oldList = pair.Value;
								List<InternalWeakDelegate> newList = new List<InternalWeakDelegate>(oldList.Count);
								foreach(InternalWeakDelegate handler in oldList)
									if (handler.Method.IsStatic || handler.Target != null)
										newList.Add(handler);
								
								if (newList.Count > 0)
									newDictionary.Add(pair.Key, newList);
							}
							
							_dictionary = newDictionary;
						}
					);
				}
				catch
				{
				}
			}
		#endregion
		
		#region Clear
			/// <summary>
			/// Clears the delegate set.
			/// </summary>
			public void Clear()
			{
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
						
						_dictionary.Clear();
					}
				);
			}
		#endregion
		#region Add
			/// <summary>
			/// Adds a new handler to the delegate set.
			/// </summary>
			/// <param name="handler">The handler to add.</param>
			/// <returns>true if the delegate was new to the set, false otherwise.</returns>
			public bool Add(Delegate handler)
			{
				if (handler == null)
					throw new ArgumentNullException("handler");
			
				int hashCode = handler.GetHashCode();
			
				bool result = false;
				
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
						
						var dictionary = _dictionary;
						
						List<InternalWeakDelegate> list;
						if (dictionary.TryGetValue(hashCode, out list))
						{
							foreach(InternalWeakDelegate action in list)
								if (action.Target == handler.Target && action.Method == handler.Method)
									return;
						}
						else
						{
							list = new List<InternalWeakDelegate>(1);
							dictionary.Add(hashCode, list);
						}
						
						InternalWeakDelegate weakDelegate = new InternalWeakDelegate(handler);
						list.Add(weakDelegate);
						result = true;
					}
				);
				
				return result;
			}
		#endregion
		#region Remove
			/// <summary>
			/// Removes a handler from the delegate set.
			/// </summary>
			/// <param name="handler">The handler to remove.</param>
			/// <returns>true if the item was in the set, false otherwise.</returns>
			public bool Remove(Delegate handler)
			{
				if (handler == null)
					throw new ArgumentNullException("handler");
			
				int hashCode = handler.GetHashCode();

				bool result = false;
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();

						var dictionary = _dictionary;
						
						List<InternalWeakDelegate> list;
						if (!dictionary.TryGetValue(hashCode, out list))
							return;
						
						int count = list.Count;
						for(int i=0; i<count; i++)
						{
							InternalWeakDelegate weakDelegate = list[i];
							if (weakDelegate.Method == handler.Method && weakDelegate.Target == handler.Target)
							{
								list.RemoveAt(i);
								result = true;
								return;
							}
						}
					}
				);
				
				return result;
			}
		#endregion
		
		#region Invoke
			/// <summary>
			/// Invokes all the handlers in the set with the given parameters.
			/// </summary>
			/// <param name="parameters">The parameters to be used in the invoke of each handler.</param>
			public void Invoke(params object[] parameters)
			{
				List<List<InternalWeakDelegate>> copy = null;
				
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
						
						copy = new List<List<InternalWeakDelegate>>(_dictionary.Values.ToList());
					}
				);
				
				foreach(var list in copy)
				{
					foreach(var handler in list)
					{
						object target = handler.Target;
						if (target != null || handler.Method.IsStatic)
							handler.Method.Invoke(target, parameters);
					}
				}
			}
		#endregion
	}
}
