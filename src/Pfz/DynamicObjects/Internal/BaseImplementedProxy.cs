using System;

namespace Pfz.DynamicObjects.Internal
{
	/// <summary>
	/// This is the base class for the generated proxies of InterfaceImplementer and DelegateImplementer.
	/// You may use this if you need to know if the object was created dinamically or to know to which
	/// object the calls are redirected.
	/// </summary>
	public abstract class BaseImplementedProxy
	{
		/// <summary>
		/// Creates a new instance of this object, setting the ProxyObject.
		/// </summary>
		public BaseImplementedProxy(object proxyObject)
		{
			_proxyObject = proxyObject;
		}

		/// <summary>
		/// This field is read by all auto-implemented methods that need to be redirected.
		/// </summary>
		[CLSCompliant(false)]
		internal protected readonly object _proxyObject;
		/// <summary>
		/// Gets the object to which the calls are redirected.
		/// </summary>
		public object ProxyObject
		{
			get
			{
				return _proxyObject;
			}
		}
	}
}
