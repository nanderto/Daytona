using System;
using System.Collections.Generic;

namespace Pfz.Remoting
{
	[Serializable]
	internal abstract class ReferenceOrWrapped
	{
		public long Id;

		public abstract ReferenceOrWrappedType Type { get; }
	}

	internal enum ReferenceOrWrappedType
	{
		Reference,
		Wrapped,
		WrappedDelegate,
		BackObjectReference,
		BackDelegateReference
	}

	[Serializable]
	internal sealed class Reference:
		ReferenceOrWrapped
	{
		public override ReferenceOrWrappedType Type
		{
			get
			{
				return ReferenceOrWrappedType.Reference;
			}
		}
	}

	[Serializable]
	internal sealed class Wrapped:
		ReferenceOrWrapped
	{
		public Type[] InterfaceTypes;

		public string RecreateAssembly;
		public string RecreateTypeName;
		public object RecreateData;
		public Dictionary<Type, Dictionary<string, object>> CachedValues;

		public override ReferenceOrWrappedType Type
		{
			get
			{
				return ReferenceOrWrappedType.Wrapped;
			}
		}
	}

	[Serializable]
	internal sealed class WrappedDelegate:
		ReferenceOrWrapped
	{
		public Type DelegateType;

		public override ReferenceOrWrappedType Type
		{
			get
			{
				return ReferenceOrWrappedType.WrappedDelegate;
			}
		}
	}

	[Serializable]
	internal sealed class BackObjectReference:
		ReferenceOrWrapped
	{
		public override ReferenceOrWrappedType Type
		{
			get
			{
				return ReferenceOrWrappedType.BackObjectReference;
			}
		}
	}

	[Serializable]
	internal sealed class BackDelegateReference:
		ReferenceOrWrapped
	{
		public override ReferenceOrWrappedType Type
		{
			get
			{
				return ReferenceOrWrappedType.BackDelegateReference;
			}
		}
	}
}
