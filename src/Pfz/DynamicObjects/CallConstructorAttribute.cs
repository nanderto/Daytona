using System;

namespace Pfz.DynamicObjects
{
	/// <summary>
	/// Use this attribute in methods present in your interface that you want to call DuckCaster.GetStaticInterface
	/// or StructuralCaster.GetStaticInterface.
	/// This attribute will tell that such method, instead of be a call to a static method, will be a call to a
	/// compatible constructor.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false, Inherited=false)]
	public sealed class CallConstructorAttribute:
		Attribute
	{
	}
}
