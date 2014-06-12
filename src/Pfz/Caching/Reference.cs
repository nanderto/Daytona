using System;
using System.Runtime.InteropServices;
using Pfz.DataTypes;
using System.Diagnostics.CodeAnalysis;

namespace Pfz.Caching
{
	/// <summary>
	/// This class is a GCHandle wrapper that allows you to reference other objects
	/// using WeakReference, Strong or other reference types.
	/// This class is thread and abort-safe, but uses lock(this) in its methods, 
	/// so never hold a lock over a reference, as this can cause dead-locks.
	/// </summary>
	public sealed class Reference<T>:
		IReference<T>,
		ICloneable<Reference<T>>,
		IDisposable
	{
		private GCHandle _handle;

		/// <summary>
		/// Creates a new empty reference.
		/// </summary>
		public Reference():
			this(default(T), GCHandleType.Normal)
		{
		}
		
		/// <summary>
		/// Creates a new reference using the given value and handle type.
		/// </summary>
		public Reference(T value, GCHandleType handleType)
		{
			try
			{
			}
			finally
			{
				_handle = GCHandle.Alloc(value, handleType);
			}
			_handleType = handleType;
		}
		
		/// <summary>
		/// Frees the handle.
		/// </summary>
		~Reference()
		{
			var handle = _handle;
			if (handle.IsAllocated)
				handle.Free();
		}
		
		/// <summary>
		/// Frees the handle immediatelly.
		/// </summary>
		public void Dispose()
		{
			lock(this)
			{
				var handle = _handle;
				if (handle.IsAllocated)
				{
					try
					{
					}
					finally
					{
						_handle = new GCHandle();
						handle.Free();
					}
				}
			}
			
			GC.SuppressFinalize(this);
		}
		
		/// <summary>
		/// Gets or sets the value pointed by the handle.
		/// </summary>
		public T Value
		{
			get
			{
				lock(this)
					return (T)_handle.Target;
			}
			set
			{
				lock(this)
					_handle.Target = value;
			}
		}
		
		private GCHandleType _handleType;
		
		/// <summary>
		/// Gets or sets the handle type.
		/// </summary>
		public GCHandleType HandleType
		{
			get
			{
				lock(this)
					return _handleType;
			}
			set
			{
				lock(this)
				{
					if (value == _handleType)
						return;
					
					GCHandle oldHandle = _handle;
					try
					{
					}
					finally
					{
						// this block must be executed completelly or must not be executed.
						_handle = GCHandle.Alloc(_handle.Target, value);
						_handleType = value;
						oldHandle.Free();
					}
				}
			}
		}

		/// <summary>
		/// Sets the data of this reference.
		/// </summary>
		public void Set(GCHandleType handleType, T value)
		{
			lock(this)
			{
				if (handleType == _handleType)
					_handle.Target = value;
				else
				{
					GCHandle oldHandle = _handle;
					try
					{
					}
					finally
					{
						// this block must be executed completelly or must not be executed.
						_handle = GCHandle.Alloc(value, handleType);
						_handleType = handleType;
						oldHandle.Free();
					}
				}
			}
		}

		/// <summary>
		/// Gets the data of this reference.
		/// </summary>
		public ReferenceData<T> GetData()
		{
			lock(this)
				return new ReferenceData<T>((T)_handle.Target, _handleType);
		}

		/// <summary>
		/// Gets the HashCode of the Value.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			var value = Value;
			if (value == null)
				return 0;

			return value.GetHashCode();
		}

		/// <summary>
		/// Compares the Value of this reference with the value of another reference.
		/// </summary>
		public override bool Equals(object obj)
		{
			var other = obj as ThreadUnsafeReference<T>;
			if (other != null)
				return Equals(other);

			return false;
		}

		/// <summary>
		/// Compares the Value of this reference with the value of another reference.
		/// </summary>
		public bool Equals(ThreadUnsafeReference<T> other)
		{
			if (other == null)
				return false;

			return object.Equals(Value, other.Value);
		}

		#region IValueContainer Members
			void IWriteOnlyValueContainer<T>.SetValue(T value)
			{
				lock(this)
					Value = value;
			}
			object IReadOnlyValueContainer.Value
			{
				get
				{
					lock(this)
						return _handle.Target;
				}
			}

			void IWriteOnlyValueContainer.SetValue(object value)
			{
				lock(this)
					Value = (T)value;
			}
		#endregion
		#region ICloneable<Reference<T>> Members
			/// <summary>
			/// Creates a copy of this reference.
			/// </summary>
			[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
			public Reference<T> Clone()
			{
				lock(this)
					return new Reference<T>((T)_handle.Target, _handleType);
			}
		#endregion
		#region ICloneable Members
			object ICloneable.Clone()
			{
				return Clone();
			}
		#endregion

		#region IReference Members
			void IReference.Set(GCHandleType handleType, object value)
			{
				Set(handleType, (T)value);
			}
			IReferenceData IReference.GetData()
			{
				return GetData();
			}
		#endregion
	}
}
