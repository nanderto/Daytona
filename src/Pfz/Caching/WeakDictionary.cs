using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// This is a dictionary that allow values to be collected.
	/// </summary>
	[Serializable]
	public sealed class WeakDictionary<TKey, TValue>:
		ReaderWriterThreadSafeDisposable,
		IDictionary<TKey, TValue>,
		ISerializable
	where
		TValue: class
	{
		#region Static Area
			private static ConstructorInfo _valueConstructor = typeof(TValue).GetConstructor(Type.EmptyTypes);
		#endregion

		#region Private dictionary of KeepAliveGCHandle
			private Dictionary<TKey, KeepAliveGCHandle> _dictionary = new Dictionary<TKey, KeepAliveGCHandle>();
		#endregion
		
		#region Constructor
			/// <summary>
			/// Creates the dictionary.
			/// </summary>
			public WeakDictionary()
			{
				GCUtils.Collected += _Collected;
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Frees all handles used to know if an item was collected or not.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
					GCUtils.Collected -= _Collected;
				
				var dictionary = _dictionary;
				if (dictionary != null)
				{
					_dictionary = null;
					
					foreach(KeepAliveGCHandle wr in dictionary.Values)
						wr.Free();
				}
				
				base.Dispose(disposing);
			}
		#endregion
		#region _Collected
			private void _Collected()
			{
				try
				{
					var readerWriterLock = ReaderWriterLock;
					readerWriterLock.UpgradeableLock
					(
						() =>
						{
							if (WasDisposed)
							{
								GCUtils.Collected -= _Collected;
								return;
							}
							
							var oldDictionary = _dictionary;
							var newDictionary = new Dictionary<TKey, KeepAliveGCHandle>(oldDictionary.Count);
							foreach(var pair in oldDictionary)
							{
								var wr = pair.Value;
							
								if (wr.IsAlive)
									newDictionary.Add(pair.Key, pair.Value);
								else
									wr.Free();
							}
						
							readerWriterLock.WriteLock
							(
								() => _dictionary = newDictionary
							);
						}
					);
				}
				catch
				{
				}
			}
		#endregion
		
		#region Properties
			#region Count
				/// <summary>
				/// Gets the number of items in this dictionary.
				/// </summary>
				public int Count
				{
					get
					{
						int result = 0;
						
						ReaderWriterLock.ReadLock
						(
							() =>
							{
								CheckUndisposed();
							
								result = _dictionary.Count;
							}
						);
						
						return result;
					}
				}
			#endregion
			#region this[]
				/// <summary>
				/// Gets or sets a value for the specified key.
				/// Returns null if the item does not exist. The indexer, when
				/// used as an IDictionary throws an exception when the item does
				/// not exist.
				/// </summary>
				public TValue this[TKey key]
				{
					get
					{
						if (key == null)
							throw new ArgumentNullException("key");
					
						TValue result = default(TValue);
						ReaderWriterLock.ReadLock
						(
							() =>
							{
								CheckUndisposed();
							
								KeepAliveGCHandle wr;
								if (_dictionary.TryGetValue(key, out wr))
									result = (TValue)wr.Target;
							}
						);
						return result;
					}
					set
					{
						if (key == null)
							throw new ArgumentNullException("key");
					
						ReaderWriterLock.WriteLock
						(
							() =>
							{
								CheckUndisposed();
							
								if (value == null)
									_Remove(key);
								else
								{
									KeepAliveGCHandle wr;
								
									var dictionary = _dictionary;
									if (dictionary.TryGetValue(key, out wr))
										wr.Target = value;
									else
										_Add(key, value, dictionary);
								}
							}
						);
					}
				}
			#endregion
			
			#region Keys
				/// <summary>
				/// Gets the Keys that exist in this dictionary.
				/// </summary>
				public ICollection<TKey> Keys
				{
					get
					{
						ICollection<TKey> result = null;
						ReaderWriterLock.ReadLock
						(
							() =>
							{
								CheckUndisposed();
							
								result = _dictionary.Keys.ToArray();
							}
						);
						return result;
					}
				}
			#endregion
			#region Values
				/// <summary>
				/// Gets the values that exist in this dictionary.
				/// </summary>
				public ICollection<TValue> Values
				{
					get
					{
						ICollection<TValue> baseResult = null;
						
						ReaderWriterLock.ReadLock
						(
							() =>
							{
								CheckUndisposed();
							
								var dictionary = _dictionary;

								List<TValue> result = new List<TValue>();
								foreach(KeepAliveGCHandle wr in dictionary.Values)
								{
									TValue item = (TValue)wr.TargetAllowingExpiration;
									if (item != null)
										result.Add(item);
								}
							
								baseResult = result;
							}
						);
						
						return baseResult;
					}
				}
			#endregion
		#endregion
		#region Methods
			#region _CreateValue
				private static TValue _CreateValue()
				{
					if (_valueConstructor == null)
						throw new NotSupportedException("Type " + typeof(TValue).FullName + " does not has a public default constructor.");

					return (TValue)_valueConstructor.Invoke(null);
				}
			#endregion

			#region Clear
				/// <summary>
				/// Clears all items in this dictionary.
				/// </summary>
				public void Clear()
				{
					ReaderWriterLock.WriteLock
					(
						() =>
						{
							CheckUndisposed();
						
							var dictionary = _dictionary;
							try
							{
							}
							finally
							{
								foreach(KeepAliveGCHandle wr in dictionary.Values)
									wr.Free();
							
								dictionary.Clear();
							}
						}
					);
				}
			#endregion
			#region GetOrCreateValue
				/// <summary>
				/// Gets the value for the given key or, if it does not exist, creates it, adds it and
				/// returns it.
				/// </summary>
				public TValue GetOrCreateValue(TKey key)
				{
					if (key == null)
						throw new ArgumentNullException("key");
					
					object result = null;
					ReaderWriterLock.ReadLock
					(
						() =>
						{
							CheckUndisposed();
						
							var dictionary = _dictionary;
							KeepAliveGCHandle wr;
							if (dictionary.TryGetValue(key, out wr))
								result = wr.Target;
						}
					);

					if (result != null)
						return (TValue)result;

					TValue typedResult = default(TValue);
					ReaderWriterLock.UpgradeableLock
					(
						() =>
						{
							CheckUndisposed();

							var dictionary = _dictionary;
							KeepAliveGCHandle wr;
							if (dictionary.TryGetValue(key, out wr))
							{
								result = wr.Target;
								if (result != null)
								{
									typedResult = (TValue)result;
									return;
								}
							
								typedResult = _CreateValue();
								ReaderWriterLock.WriteLock
								(
									() => wr.Target = typedResult
								);
							}
							else
							{
								typedResult = _CreateValue();
								ReaderWriterLock.WriteLock
								(
									() => _Add(key, typedResult, dictionary)
								);
							}
						}
					);

					return typedResult;
				}
			#endregion
			#region Add
				/// <summary>
				/// Adds an item to this dictionary. Throws an exception if an item
				/// with the same key already exists.
				/// </summary>
				public void Add(TKey key, TValue value)
				{
					if (key == null)
						throw new ArgumentNullException("key");
						
					if (value == null)
						throw new ArgumentNullException("value");
						
					ReaderWriterLock.WriteLock
					(
						() =>
						{
							CheckUndisposed();
						
							var dictionary = _dictionary;
							KeepAliveGCHandle wr;
							if (dictionary.TryGetValue(key, out wr))
							{
								if (wr.IsAlive)
									throw new ArgumentException("An element with the same key \"" + key + "\" already exists.");
							
								wr.Target = value;
							}
							else
								_Add(key, value, dictionary);
						}
					);
				}
			#endregion
			#region Remove
				/// <summary>
				/// Removes an item with the given key from the dictionary.
				/// </summary>
				public bool Remove(TKey key)
				{
					if (key == null)
						throw new ArgumentNullException("key");
					
					bool result = false;
					
					ReaderWriterLock.WriteLock
					(
						() =>
						{
							CheckUndisposed();

							result = _Remove(key);
						}
					);
					
					return result;
				}
			#endregion
			
			#region ContainsKey
				/// <summary>
				/// Gets a value indicating if an item with the specified key exists.
				/// </summary>
				public bool ContainsKey(TKey key)
				{
					return _dictionary.ContainsKey(key);
				}
			#endregion
			
			#region GetEnumerator
				/// <summary>
				/// Gets an enumerator with all key/value pairs that exist in
				/// this dictionary.
				/// </summary>
				public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
				{
					return ToList().GetEnumerator();
				}
			#endregion
			#region ToList
				/// <summary>
				/// Gets a list with all non-collected key/value pairs.
				/// </summary>
				public List<KeyValuePair<TKey, TValue>> ToList()
				{
					List<KeyValuePair<TKey, TValue>> result = new List<KeyValuePair<TKey, TValue>>();
					
					ReaderWriterLock.ReadLock
					(
						() =>
						{
							CheckUndisposed();
						
							var dictionary = _dictionary;
							foreach(var pair in dictionary)
							{
								TValue target = (TValue)pair.Value.TargetAllowingExpiration;
								if (target != null)
									result.Add(new KeyValuePair<TKey, TValue>(pair.Key, target));
							}
						}
					);
					
					return result;
				}
			#endregion
			
			#region _Add
				[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
				private static void _Add(TKey key, TValue value, Dictionary<TKey, KeepAliveGCHandle> dictionary)
				{
					try
					{
					}
					finally
					{
						KeepAliveGCHandle wr = new KeepAliveGCHandle(value);
						try
						{
							dictionary.Add(key, wr);
						}
						catch
						{
							wr.Free();
							throw;
						}
					}
				}
			#endregion
			#region _Remove
				private bool _Remove(TKey key)
				{
					var dictionary = _dictionary;
					KeepAliveGCHandle wr;
					if (!dictionary.TryGetValue(key, out wr))
						return false;

					wr.Free();
					return dictionary.Remove(key);
				}
			#endregion
		#endregion
		#region Interfaces
			#region IDictionary<TKey,TValue> Members
				bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
				{
					value = this[key];
					return value != null;
				}
				TValue IDictionary<TKey, TValue>.this[TKey key]
				{
					get
					{
						TValue result = this[key];
						if (result == null)
							throw new KeyNotFoundException("The given key \"" + key + "\" was not found in the dictionary.");
						
						return result;
					}
					set
					{
						this[key] = value;
					}
				}
			#endregion
			#region ICollection<KeyValuePair<TKey,TValue>> Members
				void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
				{
					Add(item.Key, item.Value);
				}
				bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
				{
					if (item.Value == null)
						return false;
				
					KeepAliveGCHandle wr;
					if (!_dictionary.TryGetValue(item.Key, out wr))
						return false;
					
					return object.Equals(wr.TargetAllowingExpiration, item.Value);
				}
				void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
				{
					ToList().CopyTo(array, arrayIndex);
				}
				bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
				{
					get
					{
						return false;
					}
				}
				bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
				{
					if (item.Value == null)
						return false;
				
					bool result = false;
					ReaderWriterLock.UpgradeableLock
					(
						() =>
						{
							CheckUndisposed();
						
							KeepAliveGCHandle wr;
							var dictionary = _dictionary;
							if (!dictionary.TryGetValue(item.Key, out wr))
								return;
						
							if (!object.Equals(wr.TargetAllowingExpiration, item.Value))
								return;

							ReaderWriterLock.WriteLock
							(
								() => result = _Remove(item.Key)
							);
						}
					);
					return result;
				}
			#endregion
			#region IEnumerable Members
				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			#endregion

			#region ISerializable Members
				/// <summary>
				/// Creates the dictionary from serialization info.
				/// Actually, it does not load anything, as if everything was
				/// collected.
				/// </summary>
				internal WeakDictionary(SerializationInfo info, StreamingContext context):
					this()
				{
				}
				void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
				{
				}
			#endregion
		#endregion
	}
}
