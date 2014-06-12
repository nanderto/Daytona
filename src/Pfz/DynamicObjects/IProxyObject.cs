using System;
using System.Reflection;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Interface that must be implemented by objects that want to be the proxies to calls done to
	/// any interface implemented by InterfaceImplementer class.
	/// </summary>
	public interface IProxyObject
	{
		/// <summary>
		/// Method invoked when a call to a method on the interface is done.
		/// </summary>
		/// <param name="methodInfo">The methodInfo being invoked.</param>
		/// <param name="genericArguments">If the method is generic, this parameter contains the generic arguments to build it.</param>
		/// <param name="parameters">The parameters to the method call.</param>
		/// <returns>You must return a value, if the methodInfo expects a result. Otherwise, return null.</returns>
		object InvokeMethod(MethodInfo methodInfo, Type[] genericArguments, object[] parameters);

		/// <summary>
		/// Method invoked when trying to get a property value.
		/// </summary>
		/// <param name="propertyInfo">The propertyInfo of the property being read.</param>
		/// <param name="indexes">The indexes, if this is an indexed property.</param>
		/// <returns>You must return a valid value for the property.</returns>
		object InvokePropertyGet(PropertyInfo propertyInfo, object[] indexes);

		/// <summary>
		/// Method invoked when trying to set a property value.
		/// </summary>
		/// <param name="propertyInfo">The propertyInfo of the property being set.</param>
		/// <param name="indexes">If this is an indexed property, the indexes of the value being set.</param>
		/// <param name="value">The value to set.</param>
		void InvokePropertySet(PropertyInfo propertyInfo, object[] indexes, object value);

		/// <summary>
		/// Method invoked when trying to register into an event of an interface.
		/// </summary>
		/// <param name="eventInfo">The eventInfo describing the event.</param>
		/// <param name="handler">The handler to add.</param>
		void InvokeEventAdd(EventInfo eventInfo, Delegate handler);

		/// <summary>
		/// Method invoked when trying to unregister from an event of an interface.
		/// </summary>
		/// <param name="eventInfo">The eventInfo describing the event.</param>
		/// <param name="handler">The handler to be removed.</param>
		void InvokeEventRemove(EventInfo eventInfo, Delegate handler);
	}
}
