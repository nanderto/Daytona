using System;
using System.Runtime.InteropServices;

namespace Pfz.Caching
{
	/// <summary>
	/// This struct works like a KeepAliveWeakReference, but it is internal
	/// as it's uses the GCHandle directly, and can leak memory if used
	/// unproperly.
	/// </summary>
	internal struct KeepAliveGCHandle
	{
		#region Private handle
			private GCHandle _handle;
		#endregion
		
		#region Constructor
			public KeepAliveGCHandle(object target)
			{
				if (target == null)
					throw new ArgumentNullException("target");
					
				GCUtils.KeepAlive(target);
				
				try
				{
				}
				finally
				{
					_handle = GCHandle.Alloc(target, GCHandleType.Weak);
				}
			}
		#endregion
		#region Free
			public void Free()
			{
				if (_handle.IsAllocated)
				{
					GCUtils.Expire(_handle.Target);
					_handle.Free();
				}
			}
		#endregion
		
		#region IsAlive
			public bool IsAlive
			{
				get
				{
					return _handle.Target != null;
				}
			}
		#endregion
		#region Target
			public object Target
			{
				get
				{
					object result = _handle.Target;
					GCUtils.KeepAlive(result);
					return result;
				}
				set
				{
					GCUtils.Expire(_handle.Target);
					GCUtils.KeepAlive(value);
					_handle.Target = value;
				}
			}
		#endregion
		#region TargetAllowingExpiration
			public object TargetAllowingExpiration
			{
				get
				{
					return _handle.Target;
				}
				set
				{
					_handle.Target = value;
				}
			}
		#endregion
	}
}
