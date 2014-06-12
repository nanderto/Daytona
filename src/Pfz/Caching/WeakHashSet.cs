using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// HashSet class which allows items to be collected.
	/// </summary>
	[Serializable]
	public class WeakHashSet<T>:
		ThreadSafeDisposable,
		IEnumerable<T>,
		ISerializable
	where
		T: class
	{
		private object _item;
		
		#region Private classes
			private sealed class _Values
			{
				public _Values(T item, int hashCode)
				{
					Handles = new GCHandle[1];
					Handles[0] = GCHandle.Alloc(item, GCHandleType.Weak);
					HashCode = hashCode;
					Count = 1;
				}
			
				public int HashCode;
				public int Count;
				public GCHandle[] Handles;

				internal void Add(object item)
				{
					if (Count == Handles.Length)
					{
						for (int i=0; i<Count; i++)
						{
							GCHandle handle = Handles[i];
							if (handle.Target == null)
							{
								handle.Target = item;
								return;
							}
						}
						
						Array.Resize(ref Handles, Count * 2);
					}
					
					try
					{
					}
					finally
					{
						Handles[Count] = GCHandle.Alloc(item, GCHandleType.Weak);
						Count++;
					}
				}
			}
		#endregion

		#region Constructor
			/// <summary>
			/// Creates a new instance of the WeakHashSet class.
			/// </summary>
			public WeakHashSet()
			{
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases all the GCHandles used internally.
			/// </summary>
			/// <param name="disposing"></param>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
					GCUtils.Collected -= _Collected;
					
				_Dispose(_item);
			
				base.Dispose(disposing);
			}
			private static void _Dispose(object item)
			{
				if (item == null)
					return;
				
				var pair = item as object[];
				if (pair != null)
				{
					for (int i=0; i<16; i++)
						_Dispose(pair[i]);
					
					return;
				}
				
				_Values values = (_Values)item;
				for(int i=0; i<values.Count; i++)
					values.Handles[i].Free();
			}
		#endregion
		#region _Collected
			private void _Collected()
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
						
						_Collect(ref _item);
					}
				);
			}
			private static bool _Collect(ref object item)
			{
				if (item == null)
					return true;
				
				var pair = item as object[];
				if (pair != null)
				{
					bool allCollected = true;
					for (int i=0; i<16; i++)
						allCollected &= _Collect(ref pair[i]);
					
					if (allCollected)
						item = null;
						
					return allCollected;
				}
				
				_Values values = (_Values)item;
				int count = values.Count;
				int countValid = 0;
				
				for (int i=0; i<count; i++)
					if (values.Handles[i].Target != null)
						countValid++;
						
				if (countValid == 0)
				{
					try
					{
					}
					finally
					{
						for (int i=0; i<count; i++)
							values.Handles[i].Free();
							
						item = null;
					}
					
					return true;
				}
				
				if (countValid < count)
				{
					GCHandle[] newArray = new GCHandle[countValid];
					int newArrayIndex = 0;
					try
					{
					}
					finally
					{
						for (int i=0; i<count; i++)
						{
							GCHandle handle = values.Handles[i];
							
							if (handle.Target == null)
								handle.Free();
							else
							{
								newArray[newArrayIndex] = handle;
								newArrayIndex++;
							}
						}
						
						values.Handles = newArray;
						values.Count = newArrayIndex;
					}
				}
				
				return false;
			}
		#endregion
		
		#region Clear
			/// <summary>
			/// Clears the contents of this weak hashset.
			/// </summary>
			public void Clear()
			{
				lock(DisposeLock)
				{
					CheckUndisposed();

					try
					{
					}
					finally
					{
						_Dispose(_item);
						_item = null;
					}
				}
			}
		#endregion
		#region Add
			/// <summary>
			/// Tries to add an item in this hashset.
			/// Returns true if the item was added, or false if the item was
			/// already present.
			/// </summary>
			public bool Add(T item)
			{
				if (item == null)
					throw new ArgumentNullException("item");
				
				bool result = false;
				
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
					
						int hashCode = item.GetHashCode();
						
						// 32-4: Reason - starting by the end (which changes more)
						// gives a better distribution.
						result = _Add(ref _item, hashCode, 32-4, item);
					}
				);
				
				return result;
			}
		#endregion
		#region _Add
			private static bool _Add(ref object hashItem, int hashCode, int level, T item)
			{
				if (hashItem == null)
				{
					try
					{
					}
					finally
					{
						hashItem = new _Values(item, hashCode);
					}
					
					return true;
				}
				
				int index;
				var pair = hashItem as object[];
				if (pair != null)
				{
					index = (hashCode >> level) & 15;
					return _Add(ref pair[index], hashCode, level-4, item);
				}

				_Values values = (_Values)hashItem;
				if (hashCode == values.HashCode)
				{
					for (int i=0; i<values.Count; i++)
						if (item == values.Handles[i].Target)
							return false;

					values.Add(item);
					return true;
				}
				
				pair = new object[16];
				
				index = (values.HashCode >> level) & 15;
				pair[index] = values;
				hashItem = pair;
				
				index = (hashCode >> level) & 15;
				return _Add(ref pair[index], hashCode, level-4, item);
			}
		#endregion
		#region Contains
			/// <summary>
			/// Returns true if the given item is in this hashset, false otherwise.
			/// </summary>
			public bool Contains(T item)
			{
				if (item == null)
					throw new ArgumentNullException("item");
				
				bool result = false;
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
					
						int hashCode = item.GetHashCode();
						int actualHashCode = hashCode;

						var hashItem = _item;
						while(true)
						{
							if (hashItem == null)
								return;
							
							var pair = hashItem as object[];
							if (pair == null)
								break;
							
							int zeroOrOne = actualHashCode & 15;
							hashItem = pair[zeroOrOne];
							
							actualHashCode = actualHashCode >> 4;
						}
						
						_Values values = (_Values)hashItem;
						if (values.HashCode == hashCode)
						{
							for(int i=0; i<values.Count; i++)
							{
								if (item == values.Handles[i].Target)
								{
									result = true;
									return;
								}
							}
						}
					}
				);
				
				return result;
			}
		#endregion
		#region ToList
			/// <summary>
			/// Gets a list with all the non-collected items present in this
			/// hashset.
			/// </summary>
			/// <returns></returns>
			public List<T> ToList()
			{
				List<T> result = new List<T>();
				
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
						
						_ToList(_item, result);
					}
				);
				
				return result;
			}
		#endregion
		#region _ToList
			private static void _ToList(object hashItem, List<T> result)
			{
				if (hashItem == null)
					return;
				
				var pair = hashItem as object[];
				if (pair != null)
				{
					for (int i=0; i<16; i++)
						_ToList(pair[i], result);

					return;
				}

				_Values values = (_Values)hashItem;
				int count = values.Count;
				for (int i=0; i<count; i++)
				{
					GCHandle handle = values.Handles[i];
					object target = handle.Target;
					if (target != null)
						result.Add((T)target);
				}
			}
		#endregion
		#region Remove
			/// <summary>
			/// Removes the given item from this hashset.
			/// Returns a value indicating if the given item was in the hashset.
			/// </summary>
			public bool Remove(T item)
			{
				if (item == null)
					throw new ArgumentNullException("item");
				
				bool result = false;
				DisposeLock.Lock
				(
					delegate
					{
						CheckUndisposed();
					
						int hashCode = item.GetHashCode();
						int actualHashCode = hashCode;

						var hashItem = _item;
						while(true)
						{
							if (hashItem == null)
								return;
							
							var pair = hashItem as object[];
							if (pair == null)
								break;
							
							int zeroOrOne = actualHashCode & 15;
							hashItem = pair[zeroOrOne];
							
							actualHashCode = actualHashCode >> 4;
						}
						
						_Values values = (_Values)hashItem;
						if (values.HashCode == hashCode)
						{
							for(int i=0; i<values.Count; i++)
							{
								GCHandle handle = values.Handles[i];
								
								if (item == handle.Target)
								{
									handle.Target = null;
									result = true;
									return;
								}
							}
						}
					}
				);
				
				return result;
			}
		#endregion

		#region ISerializable Members
			/// <summary>
			/// Creates the class from serialization. At this level, does not read
			/// anything, as if everything was collected.
			/// </summary>
			protected WeakHashSet(SerializationInfo info, StreamingContext context)
			{
			}
			
			/// <summary>
			/// Does not add any items to the serialization info, as if everything
			/// was collected.
			/// </summary>
			protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				GCUtils.Collected += _Collected;
			}
			
			void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
			{
				GetObjectData(info, context);
			}
		#endregion
		#region IEnumerable<T> Members
			/// <summary>
			/// Gets an enumerator over the non-collected items in this hashset.
			/// </summary>
			public IEnumerator<T> GetEnumerator()
			{
				return ToList().GetEnumerator();
			}
		#endregion
		#region IEnumerable Members
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		#endregion
	}
}
