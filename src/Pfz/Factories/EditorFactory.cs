using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Wraps a Factory&lt;IEditor&gt; so you can create right typed editors.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class EditorFactory<T>:
		IFactory
	{
		private Factory<IEditor> _factory;
		internal EditorFactory(Factory<IEditor> factory)
		{
			_factory = factory;
		}

		/// <summary>
		/// Creates and wraps and IEditor.
		/// </summary>
		public Editor<T> Create()
		{
			var result = _factory.Create();
			return new Editor<T>(result);
		}

		#region IFactory Members
			Type IFactory.FactoryType
			{
				get
				{
					return typeof(IEditor);
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
