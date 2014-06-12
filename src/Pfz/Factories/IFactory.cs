using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Interface used to access a factory without knowing its details.
	/// </summary>
	public interface IFactory
	{
		/// <summary>
		/// Gets the base type of the factory.
		/// </summary>
		Type FactoryType { get; }

		/// <summary>
		/// Gets the DataType of this factory.
		/// </summary>
		Type DataType { get; }

		/// <summary>
		/// Creates the appropriate editor/searcher for the data-type.
		/// </summary>
		object Create();
	}
}
