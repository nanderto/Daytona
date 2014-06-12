using System.Collections.Generic;
using System.Threading;
using Pfz.Caching;
using Pfz.Extensions;
using Pfz.Serialization;
using Pfz.Threading;
using Pfz.Collections;

namespace Pfz.Remoting
{
	internal sealed class WeakBidirectionalDictionary:
		ThreadSafeDisposable
	{
		private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
		private WeakDictionary<long, RemotingProxy> _dictionary = new WeakDictionary<long, RemotingProxy>();
		private AutoTrimHashSet<long> _ids = new AutoTrimHashSet<long>();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				var lockObj = _lock;
				if (lockObj != null)
				{
					_lock = null;
					lockObj.Dispose();
				}

				var ids = _ids;
				if (ids != null)
				{
					_ids = null;
					ids.Dispose();
				}

				var dictionary = _dictionary;
				if (dictionary != null)
				{
					_dictionary = null;
					dictionary.Dispose();
				}

				var weakHashSet = _weakHashSet;
				if (weakHashSet != null)
				{
					_weakHashSet = null;
					weakHashSet.Dispose();
				}
			}

			base.Dispose(disposing);
		}

		public void Add(long id, RemotingProxy remotingProxy)
		{
			_lock.WriteLock
			(
				() =>
				{
					_dictionary.Add(id, remotingProxy);
					_ids.Add(id);
				}
			);
		}
		public object Get(RemotingSerializer serializer, long id)
		{
			object result = null;
			_lock.ReadLock
			(
				() => result = _dictionary[id]
			);

			if (result == null)
			{
				serializer._notFoundReferences.Add(id);
				return null;
			}

			return result;
		}

		public long[] Collect()
		{
			List<long> collectedIds = new List<long>();
			_lock.ReadLock
			(
				() =>
				{
					foreach(long id in _ids)
						if (!_dictionary.ContainsKey(id))
							collectedIds.Add(id);
				}
			);

			if (collectedIds.Count == 0)
				return null;

			foreach(long id in collectedIds)
				_ids.Remove(id);

			return collectedIds.ToArray();
		}

		public void ClearIds()
		{
			_lock.WriteLock
			(
				() =>
				{
					foreach(RemotingProxy remotingProxy in _dictionary.Values)
					{
						remotingProxy.Id = -1;
						remotingProxy._newWrapper = null;
					}

					foreach(RemotingProxy remotingProxy in _weakHashSet)
					{
						remotingProxy.Id = -1;
						remotingProxy._newWrapper = null;
					}

					_dictionary.Clear();
					_ids.Clear();
					_weakHashSet.Clear();
				}
			);
		}

		private WeakHashSet<RemotingProxy> _weakHashSet = new WeakHashSet<RemotingProxy>();
		internal object TryAdd(long id, RemotingProxy remotingProxy)
		{
			object result = null;

			_lock.WriteLock
			(
				() =>
				{
					result = _dictionary[id];

					if (result == null)
					{
						_dictionary.Add(id, remotingProxy);
						_ids.Add(id);
					}
					else
						_weakHashSet.Add(remotingProxy);
				}
			);

			return result;
		}
	}
}
