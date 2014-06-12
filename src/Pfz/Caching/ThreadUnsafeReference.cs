using System;
using System.Runtime.InteropServices;
using Pfz.DataTypes;

namespace Pfz.Caching
{
	/// <summary>
	/// This class is a GCHandle wrapper that allows you to reference other objects
	/// using WeakReference, Strong or other reference types.
	/// It has a dispose and a destructor, but it is NOT thread safe, in the sense
	/// that changing the type or disposing from many threads can cause exceptions.
	/// Even not being thread-safe, this class is abort-safe.
	/// </summary>
	public sealed class ThreadUnsafeReference<T>:
		IReference<T>,
		ICloneable<ThreadUnsafeReference<T>>,
		IEquatable<ThreadUnsafeReference<T>>,
		IDisposable
	{
		private GCHandle _handle;

		/// <summary>
		/// Creates a new empty reference.
		/// </summary>
		public ThreadUnsafeReference():
			this(default(T), GCHandleType.Normal)
		{
		}
		
		/// <summary>
		/// Creates a new reference using the given value and handle type.
		/// </summary>
		public ThreadUnsafeReference(T value, GCHandleType handleType)
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
		~ThreadUnsafeReference()
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
			
			GC.SuppressFinalize(this);
		}
		
		/// <summary>
		/// Gets or sets the value pointed by the handle.
		/// </summary>
		public T Value
		{
			get
			{
				return (T)_handle.Target;
			}
			set
			{
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
				return _handleType;
			}
			set
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

		/// <summary>
		/// Gets the data of this Reference.
		/// </summary>
		public ReferenceData<T> GetData()
		{
			return new ReferenceData<T>((T)_handle.Target, _handleType);
		}

		/// <summary>
		/// Sets the data of this reference.
		/// </summary>
		public void Set(GCHandleType handleType, T value)
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

		/// <summary>
		/// Gets the HashCode of the Value.
		/// </summary>
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
				Value = value;
			}
			object IReadOnlyValueContainer.Value
			{
				get
				{
					return _handle.Target;
				}
			}
			void IWriteOnlyValueContainer.SetValue(object value)
			{
				Value = (T)value;
			}
		#endregion
		#region ICloneable<ThreadUnsafeReference<T>> Members
			/// <summary>
			/// Creates a copy of this reference.
			/// </summary>
			public ThreadUnsafeReference<T> Clone()
			{
				return new ThreadUnsafeReference<T>((T)_handle.Target, _handleType);
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
