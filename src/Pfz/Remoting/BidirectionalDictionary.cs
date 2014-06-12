using System;
using System.Collections.Generic;
using System.Threading;
using Pfz.Caching;
using Pfz.Extensions;
using Pfz.Threading;

namespace Pfz.Remoting
{
	internal sealed class BidirectionalDictionary:
		ReaderWriterThreadSafeDisposable
	{
		private long _idGenerator;
		private Dictionary<long, object> _dictionary1 = new Dictionary<long, object>();
		private Dictionary<object, long> _dictionary2 = new Dictionary<object, long>();

		internal BidirectionalDictionary()
		{
			GCUtils.Collected += _Collected;
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
				GCUtils.Collected -= _Collected;

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
					() =>
					{
						_dictionary1 = new Dictionary<long, object>(_dictionary1);
						_dictionary2 = new Dictionary<object, long>(_dictionary2);
					}
				);
			}
			catch
			{
			}
		}

		public object Get(long id)
		{
			object result = null;

			ReaderWriterLock.ReadLock
			(
				() =>
				{
					if (!_dictionary1.TryGetValue(id, out result))
						throw new RemotingException("Can't find Id referenced from remote side... how?");
				}
			);

			return result;
		}
		public ReferenceOrWrapped GetOrWrap(object obj)
		{
			long id = 0;
			ReaderWriterLock.ReadLock
			(
				() => _dictionary2.TryGetValue(obj, out id)
			);

			if (id != 0)
				return new Reference { Id = id };

			ReferenceOrWrapped referenceOrWrapped = null;
			ReaderWriterLock.UpgradeableLock
			(
				() =>
				{
					if (_dictionary2.TryGetValue(obj, out id))
						return;

					id = Interlocked.Increment(ref _idGenerator);
					ReaderWriterLock.WriteLock
					(
						() =>
						{
							_dictionary1.Add(id, obj);
							_dictionary2.Add(obj, id);
						}
					);
					
					var type = obj.GetType();
					if (type.IsSubclassOf(typeof(Delegate)))
					{
						WrappedDelegate wd = new WrappedDelegate();
						wd.DelegateType = type;
						wd.Id = id;
						referenceOrWrapped = wd;
						return;
					}

					Wrapped wrapped = new Wrapped();
					wrapped.Id = id;
					wrapped.InterfaceTypes = type.GetFinalInterfaces();

					IReconnectable reconnectable = obj as IReconnectable;
					if (reconnectable != null)
					{
						wrapped.RecreateAssembly = type.Assembly.FullName;
						wrapped.RecreateTypeName = type.FullName;
						wrapped.RecreateData = reconnectable.GetRecreateData();
					}

					Dictionary<Type, Dictionary<string, object>> cachedValues = null;
					foreach(var interfaceType in type.GetInterfaces())
					{
						bool isInterfaceCached = false;
						var attribute = interfaceType.GetCustomAttribute<CacheRemotePropertyValuesAttribute>();
						if (attribute != null)
							isInterfaceCached = attribute.CacheRemotePropertyValues;

						foreach(var property in interfaceType.GetProperties())
						{
							bool isCached = isInterfaceCached;
							attribute = property.GetCustomAttribute<CacheRemotePropertyValuesAttribute>();
							if (attribute != null)
								isCached = attribute.CacheRemotePropertyValues;

							if (!isCached)
								continue;

							object value = property.GetValue(obj, null);
							if (value == null)
								continue;

							if (cachedValues == null)
								cachedValues = new Dictionary<Type, Dictionary<string, object>>();

							var declaringType = property.DeclaringType;
							Dictionary<string, object> innerDictionary;
							if (!cachedValues.TryGetValue(declaringType, out innerDictionary))
							{
								innerDictionary = new Dictionary<string, object>();
								cachedValues.Add(declaringType, innerDictionary);
							}

							innerDictionary.Add(property.Name, value);
						}
					}
					wrapped.CachedValues = cachedValues;

					referenceOrWrapped = wrapped;
				}
			);

			if (referenceOrWrapped == null)
				return new Reference { Id = id };

			return referenceOrWrapped;
		}

		internal void RemoveIds(long[] ids)
		{
			ReaderWriterLock.WriteLock
			(
				() =>
				{
					foreach(long id in ids)
					{
						object obj;
						
						if (!_dictionary1.TryGetValue(id, out obj))
							throw new RemotingException("For some reason, the other side is trying to remove an Id that does not exist!");

						_dictionary1.Remove(id);

						long otherId;
						if (_dictionary2.TryGetValue(obj, out otherId))
							if (otherId == id)
								_dictionary2.Remove(obj);
					}
				}
			);
		}
		internal void InvalidateIds(long[] ids)
		{
			ReaderWriterLock.WriteLock
			(
				() =>
				{
					foreach(long id in ids)
					{
						object obj;
						
						if (!_dictionary1.TryGetValue(id, out obj))
							continue;

						long otherId;
						if (_dictionary2.TryGetValue(obj, out otherId))
							if (otherId == id)
								_dictionary2.Remove(obj);
					}
				}
			);
		}

		internal void Clear()
		{
			ReaderWriterLock.WriteLock
			(
				() =>
				{
					_dictionary1.Clear();
					_dictionary2.Clear();
				}
			);
		}
	}
}
