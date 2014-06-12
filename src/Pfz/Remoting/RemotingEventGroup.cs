using System;
using Pfz.DynamicObjects;

namespace Pfz.Remoting
{
	/// <summary>
	/// This class contains the BeforeInvoke, AfterInvoke, BeforeRedirect and AfterRedirect events,
	/// which can be invoked by the remoting framework when invoking locak (BeforeInvoke, AfterInvoke)
	/// or when redirecting a call to a remove object (BeforeRedirect, AfterRedirect).
	/// </summary>
	public sealed class RemotingEventGroup<T>:
		ICloneable<RemotingEventGroup<T>>
	where
		T: Invoke_EventArgs
	{
		internal EventHandler<T> _beforeInvoke;
		/// <summary>
		/// Event invoked when a remote call finished in the invocation of a local object.
		/// </summary>
		public event EventHandler<T> BeforeInvoke
		{
			add
			{
				_beforeInvoke += value;
			}
			remove
			{
				_beforeInvoke -= value;
			}
		}

		internal EventHandler<T> _afterInvoke;
		/// <summary>
		/// Event invoked when a remote call finished in the invocation of a local object.
		/// </summary>
		public event EventHandler<T> AfterInvoke
		{
			add
			{
				_afterInvoke += value;
			}
			remove
			{
				_afterInvoke -= value;
			}
		}

		internal EventHandler<T> _beforeRedirect;
		/// <summary>
		/// Event invoked when a local call is being redirected to a remote object.
		/// </summary>
		public event EventHandler<T> BeforeRedirect
		{
			add
			{
				_beforeRedirect += value;
			}
			remove
			{
				_beforeRedirect -= value;
			}
		}

		internal EventHandler<T> _afterRedirect;
		/// <summary>
		/// Event invoked when a local call was redirected to a remote object.
		/// </summary>
		public event EventHandler<T> AfterRedirect
		{
			add
			{
				_afterRedirect += value;
			}
			remove
			{
				_afterRedirect -= value;
			}
		}

		#region ICloneable<RemotingEventGroup<T>> Members
			/// <summary>
			/// Clones the actual RemotingEventGroup object.
			/// </summary>
			public RemotingEventGroup<T> Clone()
			{
				var result = new RemotingEventGroup<T>();
				result._afterInvoke = _afterInvoke;
				result._afterRedirect = _afterRedirect;
				result._beforeInvoke = _beforeInvoke;
				result._beforeRedirect = _beforeRedirect;
				return result;
			}
		#endregion
		#region ICloneable Members
			object ICloneable.Clone()
			{
				return Clone();
			}
		#endregion
	}
}
