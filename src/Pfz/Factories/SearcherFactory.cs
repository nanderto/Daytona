using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Wraps a Factory&lt;ISearcher&gt; so you can creates ISearch objects that casts
	/// the value automatically.
	/// </summary>
	public sealed class SearcherFactory<T>:
		IFactory
	{
		private Factory<ISearcher> _factory;
		internal SearcherFactory(Factory<ISearcher> factory)
		{
			_factory = factory;
		}

		/// <summary>
		/// Creates and wraps a searcher.
		/// </summary>
		public Searcher<T> Create()
		{
			var result = _factory.Create();
			return new Searcher<T>(result);
		}

		#region IFactory Members
			Type IFactory.FactoryType
			{
				get
				{
					return typeof(ISearcher);
				}
			}

			Type IFactory.DataType
			{
				get
				{
					return typeof(T);
				}
			}

			object IFactory.Create()
			{
				return _factory.Create();
			}
		#endregion
	}
}
