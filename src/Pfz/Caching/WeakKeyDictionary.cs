using System;
using System.Collections;
using System.Collections.Generic;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// A dictionary were keys are weakreferences. This is useful if you need
	/// to "extend" existing classes. For example, if you want to add a Tag
	/// property to any object. The way this dictionary works, you can add
	/// items to a given object, which will be kept while the object is alive,
	/// but if the object dies (is collected) they will be allowed to be 
	/// with it.
	/// </summary>
	/// <typeparam name="TKey">The type of the key, which must be a class.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class WeakKeyDictionary<TKey, TValue>:
		ThreadSafeDisposable,
		IDictionary<TKey, TValue>
	where
		TKey: class
	{
		#region Private dictionary
			private volatile Dictionary<int, List<KeyValuePair<KeepAliveGCHandle, TValue>>> _dictionary = new Dictionary<int, List<KeyValuePair<KeepAliveGCHandle, TValue>>>();
		#endregion

		#region Constructor
			/// <summary>
			/// Creates the WeakKeyDictionary.
			/// </summary>
			public WeakKeyDictionary()
			{
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Frees all handles.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
					GCUtils.Collected -= _Collected;
					
				var dictionary = _dictionary;
				if (dictionary != null)
				{
					_dictionary = null;
				
					foreach(var list in dictionary.Values)
						foreach(var pair in list)
							pair.Key.Free();
				}
			
				base.Dispose(disposing);
			}
		#endregion
		#region _Collected
			private void _Collected()
			{
				try
				{
					lock(DisposeLock)
					{
						if (WasDisposed)
						{
							GCUtils.Collected -= _Collected;
							return;
						}
							
						var oldDictionary = _dictionary;
						var newDictionary = new Dictionary<int, List<KeyValuePair<KeepAliveGCHandle, TValue>>>();
						
						foreach(var dictionaryPair in oldDictionary)
						{
							var oldList = dictionaryPair.Value;
							var newList = new List<KeyValuePair<KeepAliveGCHandle,TValue>>(oldList.Count);
							foreach(var pair in oldList)
							{
								var key = pair.Key;
								if (key.IsAlive)
									newList.Add(pair);
								else
									key.Free();
							}
							
							if (newList.Count > 0)
								newDictionary.Add(dictionaryPair.Key, newList);
						}
						
						_dictionary = newDictionary;
					}
				}
				catch
				{
				}
			}
		#endregion
		
		#region Properties
			#region Count
				/// <summary>
				/// Gets the number of the items in the dictionary. This value
				/// is not that useful, as just after getting it the number of 
				/// items can change by a collection.
				/// </summary>
				public int Count
				{
					get
					{
						lock(DisposeLock)
						{
							CheckUndisposed();
							
							return _dictionary.Count;
						}
					}
				}
			#endregion
			#region this[]
				/// <summary>
				/// Gets or sets a value for the given key.
				/// While getting, if the value does not exist an exception is thrown.
				/// This can happen if the value was collected, so avoid using getter,
				/// use TryGetValue instead.
				/// </summary>
				/// <param name="key">The key.</param>
				public TValue this[TKey key]
				{
					get
					{
						if (key == null)
							throw new ArgumentNullException("key");
						
						int hashCode = key.GetHashCode();
						
						lock(DisposeLock)
						{
							CheckUndisposed();
							
							List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
							if (_dictionary.TryGetValue(hashCode, out list))
							{
								foreach(var pair in list)
								{
									KeepAliveGCHandle handle = pair.Key;
									object target = handle.TargetAllowingExpiration;
									if (target == key)
									{
										TValue result = pair.Value;
										return result;
									}
								}
							}
							
							throw new KeyNotFoundException("The given key \"" + key + "\" was not found in dictionary.");
						}
					}
					set
					{
						if (key == null)
							throw new ArgumentNullException("key");

						int hashCode = key.GetHashCode();

						lock(DisposeLock)
						{
							CheckUndisposed();
							
							var dictionary = _dictionary;
							List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
							if (!dictionary.TryGetValue(hashCode, out list))
							{
								list = new List<KeyValuePair<KeepAliveGCHandle, TValue>>(1);
								dictionary.Add(hashCode, list);
							}
							
							int count = list.Count;
							for(int i=0; i<count; i++)
							{
								var pair = list[i];

								KeepAliveGCHandle handle = pair.Key;
								object target = handle.TargetAllowingExpiration;
								if (target == key)
								{
									pair = new KeyValuePair<KeepAliveGCHandle, TValue>(pair.Key, value);
									list[i] = pair;
									return;
								}
							}
							
							KeepAliveGCHandle newHandle = new KeepAliveGCHandle(key);
							try
							{
								var newPair = new KeyValuePair<KeepAliveGCHandle, TValue>(newHandle, value);
								list.Add(newPair);
							}
							catch
							{
								newHandle.Free();
								throw;
							}
						}
					}
				}
			#endregion

			#region Keys
				/// <summary>
				/// Returns all the non-collected keys.
				/// </summary>
				public ICollection<TKey> Keys
				{
					get
					{
						List<TKey> keys = new List<TKey>();
						
						lock(DisposeLock)
						{
							CheckUndisposed();
							
							var dictionary = _dictionary;
							foreach(var list in dictionary.Values)
							{
								foreach(var pair in list)
								{
									var keyHandle = pair.Key;
									object key = keyHandle.TargetAllowingExpiration;
									if (key != null)
										keys.Add((TKey)key);
								}
							}
						}

						return keys;
					}
				}
			#endregion
			#region Values
				/// <summary>
				/// Gets all the values still alive in this dictionary.
				/// </summary>
				public ICollection<TValue> Values
				{
					get
					{
						List<TValue> result = new List<TValue>();
						
						lock(DisposeLock)
						{
							CheckUndisposed();
							
							var dictionary = _dictionary;
							foreach(var list in dictionary.Values)
								foreach(var pair in list)
									result.Add(pair.Value);
						}

						return result;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region Clear
				/// <summary>
				/// Clears all items in the dictionary.
				/// </summary>
				public void Clear()
				{
					lock(DisposeLock)
					{
						CheckUndisposed();

						var dictionary = _dictionary;
						foreach(var list in dictionary.Values)
							foreach(var pair in list)
								pair.Key.Free();
						
						dictionary.Clear();
					}
				}
			#endregion

			#region Add
				/// <summary>
				/// Adds an item to the dictionary, or throws an exception if an item
				/// with the same key already exists.
				/// </summary>
				/// <param name="key">The key of the item to add.</param>
				/// <param name="value">The value of the item to add.</param>
				public void Add(TKey key, TValue value)
				{
					if (key == null)
						throw new ArgumentNullException("key");
					
					int hashCode = key.GetHashCode();
				
					lock(DisposeLock)
					{
						CheckUndisposed();
						
						var dictionary = _dictionary;
						List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
						if (!dictionary.TryGetValue(hashCode, out list))
						{
							list = new List<KeyValuePair<KeepAliveGCHandle, TValue>>(1);
							dictionary.Add(hashCode, list);
						}
						
						foreach(var pair in list)
						{
							KeepAliveGCHandle handle = pair.Key;
							object target = handle.TargetAllowingExpiration;
							if (target == key)
								throw new ArgumentException("An item with the same key \"" + key + "\" already exists in the dictionary.");
						}
						
						KeepAliveGCHandle newHandle = new KeepAliveGCHandle(key);
						try
						{
							var newPair = new KeyValuePair<KeepAliveGCHandle, TValue>(newHandle, value);
							list.Add(newPair);
						}
						catch
						{
							newHandle.Free();
							throw;
						}
					}
				}
			#endregion
			#region Remove
				/// <summary>
				/// Tries to remove an item from the dictionary, and returns a value 
				/// indicating if an item with the specified key existed.
				/// </summary>
				/// <param name="key">The key of the item to remove.</param>
				/// <returns>true if an item with the given key existed, false otherwise.</returns>
				public bool Remove(TKey key)
				{
					if (key == null)
						throw new ArgumentNullException("key");
						
					int hashCode = key.GetHashCode();
					
					lock(DisposeLock)
					{
						CheckUndisposed();

						var dictionary = _dictionary;
						List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
						if (!dictionary.TryGetValue(hashCode, out list))
							return false;
						
						int count = list.Count;
						for(int i=0; i<count; i++)
						{
							var pair = list[i];

							KeepAliveGCHandle handle = pair.Key;
							object target = handle.TargetAllowingExpiration;
							if (target == key)
							{
								// if the item exists, we simple set the handle target
								// to null. We do not remove the item now, as the
								// _Collected does this.
								handle.TargetAllowingExpiration = null;
								GCUtils.Expire(key);
								return true;
							}
						}
					}
					
					return false;
				}
			#endregion

			#region ContainsKey
				/// <summary>
				/// Checks if an item with the given key exists in this dictionary.
				/// </summary>
				public bool ContainsKey(TKey key)
				{
					if (key == null)
						throw new ArgumentNullException("key");
						
					int hashCode = key.GetHashCode();
					
					lock(DisposeLock)
					{
						CheckUndisposed();
						
						var dictionary = _dictionary;
						List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
						if (!dictionary.TryGetValue(hashCode, out list))
							return false;
						
						foreach(var pair in list)
							if (pair.Key.TargetAllowingExpiration == key)
								return true;
					}
					
					return false;
				}
			#endregion
			#region TryGetValue
				/// <summary>
				/// Tries to get a value with a given key.
				/// </summary>
				/// <param name="key">The key of the item to try to get.</param>
				/// <param name="value">
				/// The variable that will receive the found value, or the default value 
				/// if an item with the given key does not exist.
				/// </param>
				/// <returns>
				/// true if an item with the given key was found and stored in value
				/// parameter, false otherwise.
				/// </returns>
				public bool TryGetValue(TKey key, out TValue value)
				{
					if (key == null)
						throw new ArgumentNullException("key");
						
					int hashCode = key.GetHashCode();
					
					lock(DisposeLock)
					{
						CheckUndisposed();
						
						List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
						
						var dictionary = _dictionary;
						if (dictionary.TryGetValue(hashCode, out list))
						{
							foreach(var pair in list)
							{
								if (pair.Key.TargetAllowingExpiration == key)
								{
									value = pair.Value;
									return true;
								}
							}
						}
					}
					
					value = default(TValue);
					return false;
				}
			#endregion

			#region ToList
				/// <summary>
				/// Creates a list with all non-collected keys and values.
				/// </summary>
				public List<KeyValuePair<TKey, TValue>> ToList()
				{
					var result = new List<KeyValuePair<TKey, TValue>>();

					lock(DisposeLock)
					{
						CheckUndisposed();
						
						var dictionary = _dictionary;
						foreach(var list in dictionary.Values)
						{
							foreach(var pair in list)
							{
								TKey key = (TKey)pair.Key.TargetAllowingExpiration;
								if (key == null)
									continue;
								
								var resultItem = new KeyValuePair<TKey, TValue>(key, pair.Value);
								result.Add(resultItem);
							}
						}
					}

					return result;
				}
			#endregion
			#region GetEnumerator
				/// <summary>
				/// Gets an enumerator of all non-collected keys and values.
				/// </summary>
				public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
				{
					return ToList().GetEnumerator();
				}
			#endregion
		#endregion
		
		#region ICollection<KeyValuePair<TKey,TValue>> Members
			void ICollection<KeyValuePair<TKey,TValue>>.Add(KeyValuePair<TKey, TValue> item)
			{
				Add(item.Key, item.Value);
			}
			bool ICollection<KeyValuePair<TKey,TValue>>.Contains(KeyValuePair<TKey, TValue> item)
			{
				TValue value;
				if (TryGetValue(item.Key, out value))
					return object.Equals(value, item.Value);
				
				return false;
			}
			void ICollection<KeyValuePair<TKey,TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
			{
				ToList().CopyTo(array, arrayIndex);
			}
			bool ICollection<KeyValuePair<TKey,TValue>>.IsReadOnly
			{
				get
				{
					return false;
				}
			}
			bool ICollection<KeyValuePair<TKey,TValue>>.Remove(KeyValuePair<TKey, TValue> item)
			{
				var key = item.Key;
				if (key == null)
					throw new ArgumentException("item.Key can't be null.", "item");
					
				int hashCode = key.GetHashCode();
				lock(DisposeLock)
				{
					CheckUndisposed();

					var dictionary = _dictionary;
					List<KeyValuePair<KeepAliveGCHandle, TValue>> list;
					if (!dictionary.TryGetValue(hashCode, out list))
						return false;
					
					int count = list.Count;
					for(int i=0; i<count; i++)
					{
						var pair = list[i];

						KeepAliveGCHandle handle = pair.Key;
						object target = handle.TargetAllowingExpiration;
						if (target == key)
						{
							if (object.Equals(pair.Value, item.Value))
							{
								// if the item exists, we simple set the handle target
								// to null. We do not remove the item now, as the
								// _Collected does this.
								handle.TargetAllowingExpiration = null;
								GCUtils.Expire(key);
								return true;
							}
							
							// we already found the key, but the value is not the
							// one we expected, so we can already return false.
							return false;
						}
					}
				}
				
				return false;
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
