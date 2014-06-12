using System.Collections;
using System.Collections.Generic;
using Pfz.Caching;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.Collections
{
	/// <summary>
	/// A very simple hashset collection that's thread-safe and also calls TrimExcess automatically.
	/// </summary>
	public class AutoTrimHashSet<T>:
		ReaderWriterThreadSafeDisposable,
		ICollection<T>
	{
		private HashSet<T> _hashSet;

		/// <summary>
		/// Creates a new hashset.
		/// </summary>
		public AutoTrimHashSet()
		{
			_hashSet = new HashSet<T>();
			GCUtils.Collected += _Collected;
		}

		/// <summary>
		/// Creates a new hashset, using the given comparer.
		/// </summary>
		public AutoTrimHashSet(IEqualityComparer<T> comparer)
		{
			_hashSet = new HashSet<T>(comparer);
			GCUtils.Collected += _Collected;
		}
			
		/// <summary>
		/// Returns the comparer used by this hashset.
		/// </summary>
		public IEqualityComparer<T> Comparer
		{
			get
			{
				var hashSet = _hashSet;
				CheckUndisposed();
				return hashSet.Comparer;
			}
		}

		/// <summary>
		/// Unregisters this from GCUtils.Collected.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GCUtils.Collected -= _Collected;
				_hashSet = null;
			}

			base.Dispose(disposing);
		}
		private void _Collected()
		{
			if (WasDisposed)
				return;

			try
			{
				ReaderWriterLock.WriteLock
				(
					() => _hashSet.TrimExcess()
				);
			}
			catch
			{
			}
		}

		/// <summary>
		/// Clears this hashset.
		/// </summary>
		public void Clear()
		{
			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					_hashSet.Clear();
				}
			);
		}

		/// <summary>
		/// Tries to add an item to the hashset. Returns true if the item was added.
		/// </summary>
		public bool Add(T item)
		{
			bool result = false;

			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					result = _hashSet.Add(item);
				}
			);

			return result;
		}

		/// <summary>
		/// Removes an item. Returns true if the item was found and removed.
		/// </summary>
		public bool Remove(T item)
		{
			bool result = false;

			ReaderWriterLock.WriteLock
			(
				() =>
				{
					CheckUndisposed();

					result = _hashSet.Remove(item);
				}
			);

			return result;
		}

		/// <summary>
		/// Verifies if an item exists.
		/// </summary>
		public bool Contains(T item)
		{
			bool result = false;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					result = _hashSet.Contains(item);
				}
			);

			return result;
		}

		/// <summary>
		/// Copies the values from this hashset to an array.
		/// </summary>
		public void CopyTo(T[] array, int arrayIndex)
		{
			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					_hashSet.CopyTo(array, arrayIndex);
				}
			);
		}

		/// <summary>
		/// Gets the number of items in this hashset.
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

						result = _hashSet.Count;
					}
				);

				return result;
			}
		}

		/// <summary>
		/// Gets a list that is a copy of this hashset.
		/// </summary>
		public List<T> ToList()
		{
			List<T> result = null;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					int count = _hashSet.Count;
					result = new List<T>(count);

					foreach(T value in _hashSet)
						result.Add(value);
				}
			);

			return result;
		}

		/// <summary>
		/// Gets an enumerator over a copy of this hashset.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return ToList().GetEnumerator();
		}

		/// <summary>
		/// Returns an array that's a copy of this hashset.
		/// </summary>
		public T[] ToArray()
		{
			T[] result = null;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					CheckUndisposed();

					int count = _hashSet.Count;
					result = new T[count];

					int index = -1;
					foreach(T value in _hashSet)
					{
						index++;
						result[index] = value;
					}
				}
			);

			return result;
		}

		#region ICollection<T> Members
			void ICollection<T>.Add(T item)
			{
				Add(item);
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
	}
}
