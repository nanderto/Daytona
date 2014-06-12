
namespace Pfz.Factories
{
	/// <summary>
	/// Interface that must be implemented by data editors.
	/// </summary>
	[FactoryBase]
	public interface IEditor:
		IEditorOrSearcher
	{
		/// <summary>
		/// Gets or sets a value indicating if you are interested in the result of
		/// this editor, so it must return immediatelly on Save.
		/// </summary>
		bool MustWaitForResult { get; set; }

		/// <summary>
		/// Gets the Result of this editor. Only works if MustReturnOnSave is true.
		/// </summary>
		object Result { get; }
	}
}
