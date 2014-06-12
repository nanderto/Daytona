using System.Collections.Generic;
using System.Linq;

namespace Pfz.Factories
{
	/// <summary>
	/// Wraps an ISearcher, so the casts are done for you.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class Searcher<T>:
		ISearcher
	{
		internal Searcher(ISearcher searcher)
		{
			InternalSearcher = searcher;
		}

		/// <summary>
		/// Gets the internal searcher, in case you need to cast it.
		/// </summary>
		public ISearcher InternalSearcher { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating if the searcher must allow multi-selection.
		/// </summary>
		public bool MustAllowMultiSelect
		{
			get
			{
				return InternalSearcher.MustAllowMultiSelect;
			}
			set
			{
				InternalSearcher.MustAllowMultiSelect = value;
			}
		}

		/// <summary>
		/// Gets the Result already casts to the right type.
		/// </summary>
		public IEnumerable<T> Result
		{
			get
			{
				var result = InternalSearcher.Result;

				if (result == null)
					return null;

				return result.OfType<T>();
			}
		}

		/// <summary>
		/// Does the search.
		/// The parameter can be null, or can be supplied to fill some parameters of the search.
		/// </summary>
		public bool Search(T parametersToFill=default(T))
		{
			return InternalSearcher.Do(parametersToFill);
		}

		#region ISearcher Members
			ICollection<object> ISearcher.Result
			{
				get
				{
					return InternalSearcher.Result;
				}
			}
			bool IEditorOrSearcher.Do(object item)
			{
				return InternalSearcher.Do(item);
			}
		#endregion
	}
}
