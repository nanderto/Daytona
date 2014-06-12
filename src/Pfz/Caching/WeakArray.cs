using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Caching
{
	/// <summary>
	/// This is an array of objects, where each object can be collected.
	/// Each time an item is accessed by the indexer, a GCUtils.KeepAlive
	/// is done.
	/// </summary>
	/// <typeparam name="T">The type of the array.</typeparam>
	public sealed class WeakArray<T>:
		ThreadSafeDisposable,
		IEnumerable<T>
	where
		T: class
	{
		#region Empty weak array
			/// <summary>
			/// Gets an empty WeakArray.
			/// </summary>
			[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
			public static readonly WeakArray<T> Empty = new WeakArray<T>(0);
		#endregion
	
		#region Private array
			private UnsafeWeakArray<T> _array;
		#endregion
		
		#region Constructor
			/// <summary>
			/// Creates the array with the given length.
			/// </summary>
			public WeakArray(int length)
			{
				try
				{
				}
				finally
				{
					_array = new UnsafeWeakArray<T>(length);
				}
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases the handles used by the array.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				var array = _array;
				if (array != default(UnsafeWeakArray<T>))
				{
					_array = default(UnsafeWeakArray<T>);
					array.Free();
				}
			
				base.Dispose(disposing);
			}
		#endregion
		
		#region Properties
			#region Length
				/// <summary>
				/// Gets the number of items in this array.
				/// </summary>
				public int Length
				{
					get
					{
						int result = 0;
						
						DisposeLock.UnabortableLock
						(
							delegate
							{
								CheckUndisposed();
								
								result = _array.Length;
							}
						);
						
						return result;
					}
				}
			#endregion
			#region this[]
				/// <summary>
				/// Gets or sets the items in this array.
				/// </summary>
				/// <param name="index">The index of the item to get or set.</param>
				public T this[int index]
				{
					get
					{
						T result = default(T);
						
						DisposeLock.UnabortableLock
						(
							delegate
							{
								CheckUndisposed();
								
								result = _array[index];
							}
						);
						
						return result;
					}
					set
					{
						DisposeLock.UnabortableLock
						(
							delegate
							{
								CheckUndisposed();
								
								_array[index] = value;
							}
						);
					}
				}
			#endregion
		#endregion
		#region Methods
			#region ToArray
				/// <summary>
				/// Converts this WeakArray into a common array.
				/// </summary>
				public T[] ToArray()
				{
					T[] result = null;
					
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							
							int count = _array.Length;
							result = new T[count];
							for(int i=0; i<count; i++)
								result[i] = _array.GetAllowingExpiration(i);
						}
					);
					
					return result;
				}
			#endregion
			#region ToList
				/// <summary>
				/// Converts this WeakArray into a list.
				/// </summary>
				public List<T> ToList()
				{
					List<T> result = null;
				
					DisposeLock.Lock
					(
						delegate
						{
							CheckUndisposed();
							
							int count = _array.Length;
							result = new List<T>(count);
							for(int i=0; i<count; i++)
								result.Add(_array.GetAllowingExpiration(i));
						}
					);
					
					return result;
				}
			#endregion
		
			#region GetEnumerator
				/// <summary>
				/// Gets an enumerator for this array, so foreach can be done.
				/// </summary>
				public IEnumerator<T> GetEnumerator()
				{
					return ToList().GetEnumerator();
				}
			#endregion
		#endregion

		#region IEnumerable Members
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ToArray().GetEnumerator();
			}
		#endregion
	}
}
