using System;
using Pfz.DataTypes;

namespace Pfz.Factories
{
	/// <summary>
	/// Base interface for IEditor and ISearcher.
	/// Both share the same method and events.
	/// </summary>
	public interface IEditorOrSearcher
	{
		/// <summary>
		/// Do (Edit or Search).
		/// The item can be the record to edit, or the initial parameters of the search.
		/// </summary>
		bool Do(object item);
	}
}
