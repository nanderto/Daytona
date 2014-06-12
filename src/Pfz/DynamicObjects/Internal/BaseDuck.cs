
namespace Pfz.DynamicObjects.Internal
{
	// and also make the initial cast verify the real type. If this is already a duck,
	// maybe it is also possible to do another duck-cast in the REAL object, not in the ducked object.
	/// <summary>
	/// Class used by the framework when generating duck-types.
	/// You don't need to use it.
	/// </summary>
	public abstract class BaseDuck
	{
		/// <summary>
		/// Casts the target of this object, instead of casting this object.
		/// </summary>
		public abstract T DuckCast<T>(object securityToken);

		/// <summary>
		/// Casts the target of this object, instead of casting this object.
		/// </summary>
		public abstract T StructuralCast<T>(object securityToken);
	}
}
