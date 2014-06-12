using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Attribute to be used in Controls types that must be registered in ControlFactory.
	/// This will make it register automatically if the assembly is referenced directly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
	public sealed class AutoRegisterInControlFactoryAttribute:
		AutoRegisterInFactoryAttributeBase
	{
		/// <summary>
		/// Instantiates this Attribute, telling if it can be used for sub-types or not.
		/// </summary>
		public AutoRegisterInControlFactoryAttribute(Type dataType, bool canCreateForSubDataTypes=false):
			base(dataType, canCreateForSubDataTypes)
		{
		}

		/// <summary>
		/// Returns typeof(IValueControl).
		/// </summary>
		public override Type BaseFactoryType
		{
			get
			{
				return typeof(IValueControl);
			}
		}
	}
}
