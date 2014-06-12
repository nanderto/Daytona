using System;
using System.Collections;
using System.Collections.Generic;

namespace Pfz.Collections
{
	internal sealed class CollectionWrapper<T>:
		ICollection
	{
		private ICollection<T> _collection;
		public CollectionWrapper(ICollection<T> collection)
		{
			_collection = collection;
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get
			{
				return _collection.Count;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerator GetEnumerator()
		{
			return _collection.GetEnumerator();
		}
	}
}
