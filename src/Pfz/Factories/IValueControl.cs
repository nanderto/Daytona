using Pfz.DataTypes;

namespace Pfz.Factories
{
	/// <summary>
	/// Interface used by controls that hold some data.
	/// They usually also implement at least one version of the IValueContainer
	/// generic interface.
	/// </summary>
	[FactoryBase]
	public interface IValueControl:
		IValueContainer
	{
		/// <summary>
		/// Clears the value of the control.
		/// </summary>
		void Clear();
		
		/// <summary>
		/// Gets or sets a value telling that the control should be read-only.
		/// </summary>
		bool IsReadOnly { get; set; }
	}
}
