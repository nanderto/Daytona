
namespace Pfz.Factories
{
	/// <summary>
	/// Wraps IEditor so you can access values casted correctly.
	/// </summary>
	public sealed class Editor<T>:
		IEditor
	{
		internal Editor(IEditor editor)
		{
			InternalEditor = editor;
		}

		/// <summary>
		/// Gets the real editor, in case you need it.
		/// </summary>
		public IEditor InternalEditor { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating if the form must close and return
		/// the actual record on save.
		/// </summary>
		public bool MustWaitForResult
		{
			get
			{
				return InternalEditor.MustWaitForResult;
			}
			set
			{
				InternalEditor.MustWaitForResult = value;
			}
		}

		/// <summary>
		/// Gets the result.
		/// </summary>
		public T Result
		{
			get
			{
				return (T)InternalEditor.Result;
			}
		}

		/// <summary>
		/// Edits the given item.
		/// </summary>
		public bool Edit(T item)
		{
			return InternalEditor.Do(item);
		}

		#region IEditor Members
			object IEditor.Result
			{
				get
				{
					return InternalEditor.Result;
				}
			}

			bool IEditorOrSearcher.Do(object item)
			{
				return InternalEditor.Do(item);
			}
		#endregion
	}
}
