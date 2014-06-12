using System;

namespace Pfz.Factories
{
	/// <summary>
	/// General attribute to automatically register a Type into a factory, allowing you
	/// to specify the base interface-factory-type to use.
	/// Try to use the more specific AutoRegisterIn*Factory if possible.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=false)]
	public sealed class AutoRegisterInFactoryAttribute:
		AutoRegisterInFactoryAttributeBase
	{
		/// <summary>
		/// Initializes a new instance of AutoRegisterInFactoryAttribute setting its parameters.
		/// </summary>
		public AutoRegisterInFactoryAttribute(Type baseFactoryType, Type dataType, bool canBeUsedForSubDataTypes=false):
			base(dataType, canBeUsedForSubDataTypes)
		{
			_baseFactoryType = baseFactoryType;
		}

		private Type _baseFactoryType;
		/// <summary>
		/// Gets the interface Type that's the base for the factory.
		/// By this framework, it could be: IEditor, ISearcher or IControl.
		/// </summary>
		public override Type BaseFactoryType
		{
			get
			{
				return _baseFactoryType;
			}
		}
	}
}
