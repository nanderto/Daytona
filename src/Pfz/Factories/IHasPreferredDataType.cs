using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Interface that must be implemented by IDataControls that support many
	/// Value types to tell which one to use as the "default" type.
	/// </summary>
	public interface IHasPreferredDataType
	{
		/// <summary>
		/// Gets or sets the type of the data used by default by the
		/// IValueContainer.Value property.
		/// </summary>
		Type PreferredDataType { get; set; }
	}
}
