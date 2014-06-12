using System;
using System.Runtime.InteropServices;

namespace Pfz.Caching
{
	internal struct UnsafeWeakArray<T>
	where
		T: class
	{
		public static readonly UnsafeWeakArray<T> Empty = new UnsafeWeakArray<T>(0);
	
		private GCHandle[] _array;
		public UnsafeWeakArray(int length)
		{
			_array = new GCHandle[length];
			int index = 0;
			try
			{
				try
				{
				}
				finally
				{
					// does not stop here by an Abort().
					for (index=0; index<length; index++)
						_array[index] = GCHandle.Alloc(null, GCHandleType.Weak);
				}
			}
			catch
			{
				for(int i=0; i<index; i++)
					_array[i].Free();
			
				throw;
			}
		}
		public void Free()
		{
			foreach(GCHandle handle in _array)
				handle.Free();
		}
		
		public bool IsAlive(int index)
		{
			return _array[index].Target != null;
		}
		
		public int Length
		{
			get
			{
				return _array.Length;
			}
		}
		public T this[int index]
		{
			get
			{
				T result = (T)_array[index].Target;
				GCUtils.KeepAlive(result);
				return result;
			}
			set
			{
				GCHandle handle = _array[index];
				GCUtils.Expire(handle.Target);
				handle.Target = value;
				GCUtils.KeepAlive(value);
			}
		}
		public T GetAllowingExpiration(int index)
		{
			return (T)_array[index].Target;
		}

		public override bool Equals(object obj)
		{
			if (obj is UnsafeWeakArray<T>)
				return this == (UnsafeWeakArray<T>)obj;
				
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return _array.GetHashCode();
		}
		
		public static bool operator == (UnsafeWeakArray<T> a, UnsafeWeakArray<T> b)
		{
			return a._array == b._array;
		}
		public static bool operator != (UnsafeWeakArray<T> a, UnsafeWeakArray<T> b)
		{
			return a._array != b._array;
		}
	}
}
