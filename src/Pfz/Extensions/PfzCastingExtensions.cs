using Pfz.DynamicObjects;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds the DuckCast method to any object.
	/// </summary>
	public static class PfzCastingExtensions
	{
		/// <summary>
		/// Returns an object that implements the given interface and redirects the compatible calls to the real
		/// object. For non-compatible calls, a NotSupportedException will be thrown.
		/// </summary>
		public static T DuckCast<T>(this object source, object securityToken=null)
		{
			return DuckCaster.Cast<T>(source, securityToken);
		}

		/// <summary>
		/// Tries to cast an object to the given interface, even if it does not actually implement it.
		/// This will only works if the actual object has at least compatible methods, properties and events
		/// needed by the interface, or will throw an exception.
		/// </summary>
		public static T StructuralCast<T>(this object source, object securityToken=null)
		{
			return StructuralCaster.Cast<T>(source, securityToken);
		}
	}
}
