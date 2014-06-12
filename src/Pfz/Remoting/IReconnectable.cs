
namespace Pfz.Remoting
{
	/// <summary>
	/// Interface used by objects capable of be recreated if the connection is lost.
	/// They must also have a static method called Recreate, receiving an object as parameter (that
	/// object will be the result of GetRecreateData).
	/// </summary>
	public interface IReconnectable:
		IRemotable
	{
		/// <summary>
		/// Gets the data needed to recreate this object.
		/// </summary>
		object GetRecreateData();
	}
}
