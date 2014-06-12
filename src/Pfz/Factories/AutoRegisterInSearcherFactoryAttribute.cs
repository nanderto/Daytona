using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Attribute to be used in Controls types that must be registered in SearcherFactory.
	/// This will make it register automatically if the assembly is referenced directly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
	public sealed class AutoRegisterInSearcherFactoryAttribute:
		AutoRegisterInFactoryAttributeBase
	{
		/// <summary>
		/// Instantiates this Attribute, telling if it can be used for sub-types or not.
		/// </summary>
		public AutoRegisterInSearcherFactoryAttribute(Type dataType, bool canEditSubTypes=false):
			base(dataType, canEditSubTypes)
		{
		}

		/// <summary>
		/// Returns typeof(ISearcher).
		/// </summary>
		public override Type BaseFactoryType
		{
			get
			{
				return typeof(ISearcher);
			}
		}
	}
}
