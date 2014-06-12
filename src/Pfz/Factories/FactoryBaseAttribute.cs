using System;

namespace Pfz.Factories
{
	/// <summary>
	/// Attribute that must be used in interfaces that are meant to be the "bases" for common objects
	/// created by a factory.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface, AllowMultiple=false, Inherited=false)]
	public sealed class FactoryBaseAttribute:
		Attribute
	{
	}
}
