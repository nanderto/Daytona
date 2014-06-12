using System;

namespace Pfz.Factories
{
	/// <summary>
	/// The base interface used by AutoRegisterInEditorFactory, AutoRegisterInSearcherFactory and
	/// so on.
	/// </summary>
	public abstract class AutoRegisterInFactoryAttributeBase:
		Attribute
	{
		/// <summary>
		/// Instantiates a new AutoRegisterInFactoryAttributeBase setting its parameters.
		/// </summary>
		public AutoRegisterInFactoryAttributeBase(Type dataType, bool canBeUsedForSubDataTypes)
		{
			DataType = dataType;
			CanBeUsedForSubDataTypes = canBeUsedForSubDataTypes;
		}

		/// <summary>
		/// Gets the base interface type of the factory.
		/// </summary>
		public abstract Type BaseFactoryType { get; }

		/// <summary>
		/// Gets the DataType to which the factored control/editor/searcher will be able to work.
		/// </summary>
		public Type DataType { get; private set; }

		/// <summary>
		/// Gets a value indicating if the editor/searcher/control will be able to handle sub-types
		/// of DataType.
		/// </summary>
		public bool CanBeUsedForSubDataTypes { get; private set; }
	}
}
