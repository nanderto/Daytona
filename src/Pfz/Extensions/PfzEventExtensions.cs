using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pfz.Caching;
using Pfz.DynamicObjects;

namespace Pfz.Extensions
{
	/// <summary>
	/// This class adds two methods to the EventInfo class, making it possible
	/// to register into events in a "weak" manner. This means that the registered
	/// object can still be collected, also deregistering itself from the event.
	/// </summary>
	public static class PfzEventExtensions
	{
		private sealed class WeakProxy:
			WeakReference,
			IProxyDelegate
		{
			public WeakProxy(object target):
				base(target)
			{
			}

			internal Delegate _delegate;
			internal MethodInfo _methodInfo;
			public object Invoke(object[] parameters)
			{
				var target = Target;
				if (target != null)
					return _methodInfo.Invoke(target, parameters);

				return null;
			}
		}

		private static readonly object _registeredEventsLock = new object();
		private static Dictionary<EventInfo, Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>>> _registeredEvents;

		[SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
		static PfzEventExtensions()
		{
			GCUtils.Collected += _Collected;
		}
		private static void _Collected()
		{
			lock(_registeredEventsLock)
			{
				var oldRegisteredEvents = _registeredEvents;
				if (oldRegisteredEvents == null)
					return;

				var newRegisteredEvents = new Dictionary<EventInfo, Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>>>();
				foreach(var pair1 in oldRegisteredEvents)
				{
					var eventInfo = pair1.Key;
					var oldMethodDictionary = pair1.Value;
					var newMethodDictionary = new Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>>();
					foreach(var pair2 in oldMethodDictionary)
					{
						var methodInfo = pair2.Key;
						var oldOwnerDictionary = pair2.Value;
						var newOwnerDictionary = new Dictionary<object, HashSet<WeakProxy>>();

						foreach(var pair3 in oldOwnerDictionary)
						{
							var owner = pair3.Key;
							var oldHashset = pair3.Value;
							var newHashset = new HashSet<WeakProxy>();

							foreach(var weakProxy in oldHashset)
								if (weakProxy.IsAlive)
									newHashset.Add(weakProxy);

							if (newHashset.Count > 0)
								newOwnerDictionary.Add(owner, newHashset);
						}

						if (newOwnerDictionary.Count > 0)
							newMethodDictionary.Add(methodInfo, newOwnerDictionary);
					}

					if (newMethodDictionary.Count > 0)
						newRegisteredEvents.Add(eventInfo, newMethodDictionary);
				}

				if (newRegisteredEvents.Count > 0)
					_registeredEvents = newRegisteredEvents;
				else
					_registeredEvents = null;
			}
		}

		/// <summary>
		/// Adds an event-handler to an event, but still allows the target object to
		/// be collected.
		/// </summary>
		public static void AddWeakEventHandler(this EventInfo eventInfo, object eventOwner, Delegate handler)
		{
			if (eventInfo == null)
				throw new ArgumentNullException("eventInfo");
			
			if (handler == null)
				throw new ArgumentNullException("handler");
				
			var target = handler.Target;
			if (target == null)
				throw new ArgumentException("The handler does not have a target. Do not use this method for static event-handlers.");
				
			if (eventInfo.GetAddMethod().IsStatic != (eventOwner == null))
				throw new ArgumentException("Invalid eventOwner.");
				
			Type handlerType = eventInfo.EventHandlerType;
			
			if (handler.GetType() != handlerType)
				throw new ArgumentException("Invalid handler for this eventInfo.");

			WeakProxy weakProxy = new WeakProxy(target);
			weakProxy._methodInfo = handler.Method;

			var newHandler = DelegateProxier.Proxy(weakProxy, handler.GetType());
			weakProxy._delegate = newHandler;
			eventInfo.AddEventHandler(eventOwner, newHandler);

			lock(_registeredEventsLock)
			{
				var registeredEvents = _registeredEvents;
				if (registeredEvents == null)
				{
					registeredEvents = new Dictionary<EventInfo, Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>>>();
					_registeredEvents = registeredEvents;
				}

				Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>> methodDictionary;
				if (!registeredEvents.TryGetValue(eventInfo, out methodDictionary))
				{
					methodDictionary = new Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>>();
					registeredEvents.Add(eventInfo, methodDictionary);
				}

				MethodInfo methodInfo = handler.Method;
				Dictionary<object, HashSet<WeakProxy>> ownerDictionary;
				if (!methodDictionary.TryGetValue(methodInfo, out ownerDictionary))
				{
					ownerDictionary = new Dictionary<object, HashSet<WeakProxy>>();
					methodDictionary.Add(methodInfo, ownerDictionary);
				}

				var fakeOwner = eventOwner;
				if (eventOwner == null)
					fakeOwner = _registeredEventsLock;

				HashSet<WeakProxy> hashset;
				if (!ownerDictionary.TryGetValue(fakeOwner, out hashset))
				{
					hashset = new HashSet<WeakProxy>();
					ownerDictionary.Add(fakeOwner, hashset);
				}

				hashset.Add(weakProxy);
			}
		}
		
		/// <summary>
		/// Removes an event previously registered as weak.
		/// </summary>
		public static void RemoveWeakEventHandler(this EventInfo eventInfo, object eventOwner, Delegate handler)
		{
			if (eventInfo == null)
				throw new ArgumentNullException("eventInfo");
			
			if (handler == null)
				throw new ArgumentNullException("handler");

			var target = handler.Target;
			if (target == null)
				throw new ArgumentException("handler.Target is null.", "handler");
			
			lock(_registeredEventsLock)
			{
				var registeredEvents = _registeredEvents;
				if (registeredEvents == null)
					return;

				Dictionary<MethodInfo, Dictionary<object, HashSet<WeakProxy>>> methodDictionary;
				if (!registeredEvents.TryGetValue(eventInfo, out methodDictionary))
					return;

				Dictionary<object, HashSet<WeakProxy>> ownerDictionary;
				if (!methodDictionary.TryGetValue(handler.Method, out ownerDictionary))
					return;
				
				var fakeOwner = eventOwner;
				if (eventOwner == null)
					fakeOwner = _registeredEventsLock;

				HashSet<WeakProxy> hashset;
				if (!ownerDictionary.TryGetValue(fakeOwner, out hashset))
					return;

				foreach(var weakProxy in hashset)
				{
					if (weakProxy.Target == target)
					{
						eventInfo.RemoveEventHandler(target, weakProxy._delegate);
						hashset.Remove(weakProxy);
						break;
					}
				}
			}
		}
	}
}
