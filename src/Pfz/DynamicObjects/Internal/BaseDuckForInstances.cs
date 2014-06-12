using System;
using System.Security;

namespace Pfz.DynamicObjects.Internal
{
	/// <summary>
	/// This class is used when emitting code for StructuralCaster.Cast.
	/// </summary>
	public abstract class BaseDuckForInstances:
		BaseDuck
	{
		/// <summary>
		/// Used by emitted code.
		/// </summary>
		protected BaseDuckForInstances(object target, object securityToken)
		{
			_target = target;
			_securityToken = securityToken;
		}

		/// <summary>
		/// Used by emitted code.
		/// </summary>
		[CLSCompliant(false)]
		internal protected readonly object _target;

		/// <summary>
		/// Gets the Target object of interface calls.
		/// </summary>
		public object GetTarget(object securityToken = null)
		{
			if (securityToken != _securityToken)
				throw new SecurityException("Invalid securityToken.");

			return _target;
		}

		/// <summary>
		/// Internal use.
		/// </summary>
		[CLSCompliant(false)]
		internal protected readonly object _securityToken;

		/// <summary>
		/// Gets a value indicating if a SecurityToken was used when creating the object.
		/// </summary>
		public bool HasSecurityToken
		{
			get
			{
				return _securityToken != null;
			}
		}

		/// <summary>
		/// Recasts the source object if the securityToken is OK.
		/// </summary>
		public override T DuckCast<T>(object securityToken)
		{
			if (securityToken != _securityToken)
			{
				if (typeof(T).IsInterface)
					return DuckCaster._Cast<T>(this, securityToken);

				throw new SecurityException("Invalid securityToken.");
			}

			return DuckCaster.Cast<T>(_target, securityToken);
		}

		/// <summary>
		/// Recasts the source object if the securityToken is OK.
		/// </summary>
		public override T StructuralCast<T>(object securityToken)
		{
			if (securityToken != _securityToken)
			{
				if (typeof(T).IsInterface)
					return StructuralCaster._Cast<T>(this, securityToken);

				throw new SecurityException("Invalid securityToken.");
			}

			return StructuralCaster.Cast<T>(_target, securityToken);
		}
	}
}
