
using System.Collections.Generic;
namespace Pfz.Factories
{
	/// <summary>
	/// Interface that must be implemented by "Searchers" or "Lookups".
	/// </summary>
	[FactoryBase]
	public interface ISearcher:
		IEditorOrSearcher
	{
		/// <summary>
		/// Gets or sets a value indicating if this searcher allows multi-select.
		/// </summary>
		bool MustAllowMultiSelect { get; set; }

		/// <summary>
		/// Gets a collection with all selected items.
		/// </summary>
		ICollection<object> Result { get; }
	}
}
