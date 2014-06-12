using System;
using System.Runtime.Serialization;

namespace Pfz.Caching
{
	/// <summary>
	/// This is a WeakReference class with KeepAlive capability.
	/// Everytime you get the Target from this WeakReference, it calls the GCUtils.KeepAlive.
	/// This is a very simple way to use weak-references only to your objects and also
	/// keep them alive if they are used frequently.
	/// </summary>
	[Serializable]
	public class KeepAliveWeakReference:
		WeakReference
	{
		#region Constructors
			/// <summary>
			/// Constructs a KeepAliveWeakReference pointing to a target.
			/// </summary>
			/// <param name="target">The target of this KeepAliveWeakReference.</param>
			public KeepAliveWeakReference(object target):
				base(target)
			{
				GCUtils.KeepAlive(target);
			}
			
			/// <summary>
			/// Constructs a KeepAliveWeakReference pointing to a target and allowing to trackResurrection.
			/// The Caching framework does not use this constructor, it is only here to keep the functionality
			/// of the second parameter present in the System.WeakReference constructor.
			/// </summary>
			/// <param name="target">The target of this WeakReference.</param>
			/// <param name="trackResurrection">Boolean indicating if this WeakReference must trackResurrection.</param>
			public KeepAliveWeakReference(object target, bool trackResurrection):
				base(target, trackResurrection)
			{
				GCUtils.KeepAlive(target);
			}
			
			/// <summary>
			/// Constructs a KeepAliveWeakReference pointing to a target, allowing to trackResurrection and
			/// allowing you to tell if immediateExpiration is allowed. The other two constructors always
			/// do a KeepAlive, so they don't allow immediateExpiration.
			/// </summary>
			/// <param name="target">The target of this WeakReference.</param>
			/// <param name="trackResurrection">Boolean indicating if this WeakReference must trackResurrection.</param>
			/// <param name="allowImmediateExpiration">
			/// If true, the target can be collected in the next collection.
			/// If false, it will be kept alive at the next collection.
			/// </param>
			public KeepAliveWeakReference(object target, bool trackResurrection, bool allowImmediateExpiration):
				base(target, trackResurrection)
			{
				if (!allowImmediateExpiration)
					GCUtils.KeepAlive(target);
			}
			
			/// <summary>
			/// Simple keeping the serialization constructor present in WeakReference.
			/// </summary>
			/// <param name="info"></param>
			/// <param name="context"></param>
			protected KeepAliveWeakReference(SerializationInfo info, StreamingContext context):
				base(info, context)
			{
			}
		#endregion
		
		#region Target
			/// <summary>
			/// Overrides the WeakReference.Target, so it calls KeepAlive while gets or sets the Target.
			/// </summary>
			public override object Target
			{
				get
				{
					object result = base.Target;
					GCUtils.KeepAlive(result);
					return result;
				}
				set
				{
					GCUtils.Expire(base.Target);
					base.Target = value;
					GCUtils.KeepAlive(value);
				}
			}
		#endregion
		#region TargetAllowingExpiration
			/// <summary>
			/// This is equivalent to a custom WeakReference target, as it's gets or sets the target
			/// without calling KeepAlive. Keep in mind that when an external code uses WeakReference,
			/// you can pass a KeepAliveWeakReference as a parameter, and the conventional Target that
			/// is used by default will have the KeepAlive effect.
			/// </summary>
			public object TargetAllowingExpiration
			{
				get
				{
					return base.Target;
				}
				set
				{
					base.Target = value;
				}
			}
		#endregion
	}
	
	/// <summary>
	/// A typed version of weak-reference.
	/// Note that it simple hides the untyped Target and TargetAllowingExpiration.
	/// If you cast this object as a simple WeakReference you can still set
	/// an invalid typed target to it. This is only a helper class to avoid
	/// manual casts.
	/// </summary>
	/// <typeparam name="T">The type of the objects used by this weak-reference.</typeparam>
	[Serializable]
	public sealed class KeepAliveWeakReference<T>:
		KeepAliveWeakReference
	where
		T: class
	{
		#region Constructors
			/// <summary>
			/// Constructs a KeepAliveWeakReference pointing to a target.
			/// </summary>
			/// <param name="target">The target of this KeepAliveWeakReference.</param>
			public KeepAliveWeakReference(object target):
				base(target)
			{
			}
			
			/// <summary>
			/// Constructs a KeepAliveWeakReference pointing to a target and allowing to trackResurrection.
			/// The Caching framework does not use this constructor, it is only here to keep the functionality
			/// of the second parameter present in the System.WeakReference constructor.
			/// </summary>
			/// <param name="target">The target of this WeakReference.</param>
			/// <param name="trackResurrection">Boolean indicating if this WeakReference must trackResurrection.</param>
			public KeepAliveWeakReference(object target, bool trackResurrection):
				base(target, trackResurrection)
			{
			}
			
			/// <summary>
			/// Constructs a KeepAliveWeakReference pointing to a target, allowing to trackResurrection and
			/// allowing you to tell if immediateExpiration is allowed. The other two constructors always
			/// do a KeepAlive, so they don't allow immediateExpiration.
			/// </summary>
			/// <param name="target">The target of this WeakReference.</param>
			/// <param name="trackResurrection">Boolean indicating if this WeakReference must trackResurrection.</param>
			/// <param name="allowImmediateExpiration">
			/// If true, the target can be collected in the next collection.
			/// If false, it will be kept alive at the next collection.
			/// </param>
			public KeepAliveWeakReference(object target, bool trackResurrection, bool allowImmediateExpiration):
				base(target, trackResurrection, allowImmediateExpiration)
			{
			}
			
			/// <summary>
			/// Simple keeping the serialization constructor present in WeakReference.
			/// </summary>
			/// <param name="info"></param>
			/// <param name="context"></param>
			private KeepAliveWeakReference(SerializationInfo info, StreamingContext context):
				base(info, context)
			{
			}
		#endregion
		
		#region Target
			/// <summary>
			/// Gets or sets the typed target, calling GCUtils.KeepAlive() 
			/// while doing it.
			/// </summary>
			public new T Target
			{
				get
				{
					return base.Target as T;
				}
				set
				{
					base.Target = value;
				}
			}
		#endregion
		#region TargetAllowingExpiration
			/// <summary>
			/// Gets or sets the typed-target, without calling GCUtils.KeepAlive().
			/// </summary>
			public new T TargetAllowingExpiration
			{
				get
				{
					return base.TargetAllowingExpiration as T;
				}
				set
				{
					base.TargetAllowingExpiration = value;
				}
			}
		#endregion
	}
}
