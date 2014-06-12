using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pfz.Caching;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.Collections
{
	/// <summary>
	/// Dictionary that uses a ReaderWriterLock for its accesses (so the call to each method
	/// is thread-safe) and also gets "trimmed" automatically when garbage collections occur.
	/// 
	/// It is also capable of auto-collecting Values that are empty collections and does not 
	/// accept null values (or remove an item set to null).
	/// </summary>
	public class AutoTrimDictionary<TKey, TValue>:
		ReaderWriterThreadSafeDisposable,
		IDictionary<TKey, TValue>
	{
		private volatile Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

		/// <summary>
		/// Creates a new instance of AutoTrimDictionary.
		/// </summary>
		public AutoTrimDictionary()
		{
			GCUtils.Collected += _Collected;
		}

		/// <summary>
		/// Unregisters this dictionary from GCUtils.Collected.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GCUtils.Collected -= _Collected;
				_dictionary = null;
			}

			base.Dispose(disposing);
		}
		private void _Collected()
		{
			try
			{
				ReaderWriterLock.UpgradeableLock
				(
					() =>
					{
						if (WasDisposed)
							return;

						Dictionary<TKey, TValue> newDictionary;
						if (MustRemoveEmptyCollections)
						{
							var oldDictionary = _dictionary;
							newDictionary = new Dictionary<TKey, TValue>(_dictionary.Count);

							foreach(var pair in oldDictionary)
							{
								var key = pair.Key;
								var value = pair.Value;

								IEnumerable<object> enumerable = value as IEnumerable<object>;
								if (enumerable != null)
									using (var enumerator = enumerable.GetEnumerator())
										if (!enumerator.MoveNext())
											continue;

								newDictionary.Add(key, value);
							}
						}
						else
							newDictionary = new Dictionary<TKey, TValue>(_dictionary);

						ReaderWriterLock.WriteLock
						(
							() =>
								_dictionary = newDictionary
						);
					}
				);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Gets or sets a value indicating that values that are empty-collections must be removed
		/// during a collection.
		/// </summary>
		public bool MustRemoveEmptyCollections { get; set; }

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public void Add(TKey key, TValue value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					_dictionary.Add(key, value);
				}
			);
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public bool ContainsKey(TKey key)
		{
			bool result = false;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					result = _dictionary.ContainsKey(key);
				}
			);

			return result;
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
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

						result = _dictionary.Keys.ToList();
					}
				);

				return result;
			}
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public bool Remove(TKey key)
		{
			bool result = false;

			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					result = _dictionary.Remove(key);
				}
			);

			return result;
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public bool TryGetValue(TKey key, out TValue value)
		{
			bool result = false;
			TValue resultValue = default(TValue);

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					result = _dictionary.TryGetValue(key, out resultValue);
				}
			);

			value = resultValue;
			return result;
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public ICollection<TValue> Values
		{
			get
			{
				ICollection<TValue> result = null;

				ReaderWriterLock.ReadLock
				(
					() =>
					{
						CheckUndisposed();

						result = _dictionary.Values.ToList();
					}
				);

				return result;
			}
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public TValue this[TKey key]
		{
			get
			{
				TValue result = default(TValue);

				ReaderWriterLock.ReadLock
				(
					() =>
					{
						CheckUndisposed();

						result = _dictionary[key];
					}
				);

				return result;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				ReaderWriterLock.WriteLock
				(
					() =>
					{
						CheckUndisposed();

						_dictionary[key] = value;
					}
				);
			}
		}


		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			if (item.Value == null)
				throw new ArgumentException("item.Value can't be null.");

			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					_dictionary.Add(item.Key, item.Value);
				}
			);
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public void Clear()
		{
			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					_dictionary.Clear();
				}
			);
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			bool result = false;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();
					
					ICollection<KeyValuePair<TKey, TValue>> dictionary = _dictionary;
					result = dictionary.Contains(item);
				}
			);

			return result;
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					ICollection<KeyValuePair<TKey, TValue>> dictionary = _dictionary;
					dictionary.CopyTo(array, arrayIndex);
				}
			);
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
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

		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
		{
			get
			{
				return false;
			}
		}

		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			bool result = false;

			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					ICollection<KeyValuePair<TKey, TValue>> dictionary = _dictionary;
					result = dictionary.Remove(item);
				}
			);

			return result;
		}

		/// <summary>
		/// Redirects the call to the real dictionary, in a thread-safe manner.
		/// </summary>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			List<KeyValuePair<TKey, TValue>> result = null;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					result = _dictionary.ToList();
				}
			);

			return result.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
