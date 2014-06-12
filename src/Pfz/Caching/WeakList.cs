using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// A list that only keeps weak-references to it's items.
	/// Ideal if at some point you only need to do a for-each over all the
	/// non-collected items. Use WeakHashSet if you need to remove the items
	/// or calls Contains frequently.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class WeakList<T>:
		ThreadSafeDisposable,
		ISerializable,
		IList<T>
	where
		T: class
	{
		#region Private fields
			private GCHandle[] _handles;
		#endregion

		#region Constructors
			/// <summary>
			/// Creates an empty weak-list.
			/// </summary>
			public WeakList():
				this(32)
			{
			}
			
			/// <summary>
			/// Creates an empty weak-list using the given minCapacity to it.
			/// </summary>
			/// <param name="initialCapacity">The initialCapacity of the list. The default value is 32.</param>
			public WeakList(int initialCapacity)
			{
				if (initialCapacity < 1)
					throw new ArgumentOutOfRangeException("initialCapacity", "The initial accepted capacity value is 1.");
					
				_handles = new GCHandle[initialCapacity];
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases all handles.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				// if the dispose was called manullay, we must unregister ourselves from the GCUtils.Collected method.
				// we don't need to do this if this object is being collected, as the Collected event is also we and, so,
				// it was already removed from there.
				if (disposing)
					GCUtils.Collected -= _Collected;

				// here we will free all allocated handles. After all, even if the objects can be collected, the
				// handles must be freed.
				var handles = _handles;
				if (handles != null)
				{
					int count = handles.Length;
					for (int i=0; i<count; i++)
					{
						var handle = handles[i];
						if (handle.IsAllocated)
							handle.Free();
					}

					_handles = null;
				}

				base.Dispose(disposing);
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
						
						int allocated = 0;
						
						int count = Count;
						for(int i=0; i<count; i++)
						{
							var handle = _handles[i];
							
							if (handle.IsAllocated && handle.Target != null)
								allocated ++;
						}
						
						int minCapacity = count / 2;
						if (minCapacity < 32)
							minCapacity = 32;
							
						if (allocated < minCapacity)
							allocated = minCapacity;
						
						if (allocated != _handles.Length)
						{
							var newHandles = new GCHandle[allocated];
							try
							{
							}
							finally
							{
								int newIndex = 0;
								for(int i=0; i<count; i++)
								{
									var handle = _handles[i];
									
									if (!handle.IsAllocated)
										continue;
									
									var target = handle.Target;
									if (target == null)
										handle.Free();
									else
									{
										newHandles[newIndex] = handle;
										newIndex++;
									}
								}
								for(int i=count; i<_handles.Length; i++)
								{
									var handle = _handles[i];
									if (handle.IsAllocated)
										handle.Free();
								}
								
								Count = newIndex;
								_handles = newHandles;
							}
						}
					}
				);
			}
		#endregion
		
		#region Properties
			#region Count
				/// <summary>
				/// Gets an approximate count of the items added.
				/// </summary>
				public int Count { get; private set; }
			#endregion
			#region this[]
				/// <summary>
				/// Gets or sets an item at a given index.
				/// </summary>
				[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
				public T this[int index]
				{
					get
					{
						T result = default(T);
						
						DisposeLock.Lock
						(
							delegate
							{
								CheckUndisposed();
								
								if (index < 0 || index >= Count)
									throw new ArgumentOutOfRangeException("index");
								
								result = (T)_handles[index].Target;
							}
						);
						
						return result;
					}
					set
					{
						DisposeLock.Lock
						(
							delegate
							{
								CheckUndisposed();
								
								if (index < 0 || index >= Count)
									throw new ArgumentOutOfRangeException("index");

								var handle = _handles[index];
								if (!handle.IsAllocated)
								{
									if (value != null)
									{
										try
										{
										}
										finally
										{
											_handles[index] = GCHandle.Alloc(value, GCHandleType.Weak);
										}
									}

									return;
								}

								handle.Target = value;
							}
						);
					}
				}
			#endregion
		#endregion
		#region Methods
			#region Clear
				/// <summary>
				/// Clears all the items in the list.
				/// </summary>
				public void Clear()
				{
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							
							Count = 0;
						}
					);
				}
			#endregion
			#region Add
				/// <summary>
				/// Adds an item to the list.
				/// </summary>
				public void Add(T item)
				{
					if (item == null)
						throw new ArgumentNullException("item");
					
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							
							int count = Count;
							if (count == _handles.Length)
								Array.Resize(ref _handles, count * 2);
							
							var handle = _handles[count];
							if (handle.IsAllocated)
								handle.Target = item;
							else
							{
								try
								{
								}
								finally
								{
									_handles[count] = GCHandle.Alloc(item, GCHandleType.Weak);
								}
							}

							Count++;
						}
					);
				}
			#endregion
			#region ToList
				/// <summary>
				/// Gets a strong-list with all the non-collected items present
				/// in this list.
				/// </summary>
				public List<T> ToList()
				{
					List<T> result = new List<T>();
					
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							
							int count = Count;
							for (int i=0; i<count; i++)
							{
								object target = _handles[i].Target;
								
								if (target != null)
									result.Add((T)target);
							}
						}
					);
						
					return result;
				}
			#endregion
			
			#region Contains
				/// <summary>
				/// Returns true if an item exists in the collection, false otherwise.
				/// </summary>
				public bool Contains(T item)
				{
					return IndexOf(item) != -1;
				}
			#endregion
			#region IndexOf
				/// <summary>
				/// Gets the IndexOf an item in this list, or -1 if it does not exist.
				/// Note that just after checking for the index of an item, it could
				/// be collected, so this is not really useful.
				/// </summary>
				public int IndexOf(T item)
				{
					if (item == null)
						throw new ArgumentNullException("item");
				
					int result = -1;
					
					DisposeLock.Lock
					(
						() =>
						{
							CheckUndisposed();
							
							int count = Count;
							for (int i=0; i<count; i++)
							{
								GCHandle handle = _handles[i];
								if (handle.Target == item)
								{
									result = i;
									return;
								}
							}
						}
					);
					
					return result;
				}
			#endregion
			
			#region RemoveAt
				/// <summary>
				/// Removes an item at a given index.
				/// Note that, different from normal lists, the following items do
				/// not move and the count is not immediatelly updated.
				/// </summary>
				[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
				public void RemoveAt(int index)
				{
					DisposeLock.Lock
					(
						() =>
						{
							CheckUndisposed();
							
							if (index < 0 || index >= Count)
								throw new ArgumentOutOfRangeException("index");
							
							var handle = _handles[index];
							if (handle.IsAllocated)
								handle.Target = null;
						}
					);
				}
			#endregion
			#region Remove
				/// <summary>
				/// Tries to remove an item from this list and returns if it was
				/// found and removed. Note that the Count and the following items 
				/// are not automatically updated, as this will only happen in the next
				/// garbage collection.
				/// </summary>
				public bool Remove(T item)
				{
					bool result = false;
					
					DisposeLock.Lock
					(
						() =>
						{
							int index = IndexOf(item);
							if (index != -1)
							{
								_handles[index].Target = null;
								result = true;
							}
						}
					);
					
					return result;
				}
			#endregion
			
			#region GetEnumerator
				/// <summary>
				/// Gets an enumerator over the non-collected items of this
				/// list.
				/// </summary>
				public IEnumerator<T> GetEnumerator()
				{
					return ToList().GetEnumerator();
				}
			#endregion
		#endregion
		
		#region Interfaces
			#region ISerializable Members
				/// <summary>
				/// Creates the WeakList from the serialization info.
				/// </summary>
				protected WeakList(SerializationInfo info, StreamingContext context):
					this(32)
				{
				}
				
				/// <summary>
				/// Creates serialization info.
				/// </summary>
				protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
				{
				}

				void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
				{
					GetObjectData(info, context);
				}
			#endregion
			#region IList<T> Members
				void IList<T>.Insert(int index, T item)
				{
					throw new NotImplementedException();
				}
			#endregion
			#region ICollection<T> Members
				void ICollection<T>.CopyTo(T[] array, int arrayIndex)
				{
					ToList().CopyTo(array, arrayIndex);
				}
				bool ICollection<T>.IsReadOnly
				{
					get
					{
						return false;
					}
				}
			#endregion
			#region IEnumerable Members
				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			#endregion
		#endregion
	}
}
