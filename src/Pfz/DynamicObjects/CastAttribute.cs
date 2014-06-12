using System;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Use this attribute in properties or methods of interfaces that you will use for Duck or Structural Castings.
	/// The Result of such property or method will also have its value casted, accordingly to this attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple=false, Inherited=false)]
	public sealed class CastAttribute:
		Attribute
	{
		/// <summary>
		/// Creates a new CastResultAttribute.
		/// </summary>
		/// <param name="mustValidateStructure">If set to true (the default), a StructuralCaster.Cast will be used. Otherwise, a DuckCaster.Cast will be used.</param>
		/// <param name="mustUseSecurityToken">If set to true, Casts done will use the actual object securityToken. This will make the new casted result also be secured, but will also allow to recast to the real type (which can be a security problem, but needed sometimes).</param>
		public CastAttribute(bool mustValidateStructure = true, bool mustUseSecurityToken = false)
		{
			MustValidateStructure = mustValidateStructure;
			MustUseSecurityToken = mustUseSecurityToken;
		}

		/// <summary>
		/// Gets a value indicating if the structure will be validated (StructuralCaster.Cast) or not (DuckCaster.Cast).
		/// </summary>
		public bool MustValidateStructure { get; private set; }

		/// <summary>
		/// Gets a value indicating if the security token of the actual object must be used when doing the cast.
		/// </summary>
		public bool MustUseSecurityToken { get; private set; }
	}
}
