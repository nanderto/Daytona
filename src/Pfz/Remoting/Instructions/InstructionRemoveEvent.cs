using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Pfz.Caching;
using Pfz.DynamicObjects;

namespace Pfz.Remoting.Instructions
{
	[Serializable]
	internal sealed class InstructionRemoveEvent:
		InstructionEvent
	{
		[SuppressMessage("Microsoft.Usage", "CA2219:DoNotRaiseExceptionsInExceptionClauses")]
		public override void Run(RemotingClient client, ThreadData threadData)
		{
			threadData._Action
			(
				false,
				() =>
				{
					var eventInfo = EventInfo;

					var obj = client._objectsUsedByTheOtherSide.Get(ObjectId);
					var handler = Handler;

					InvokeEvent_EventArgs args = null;
					EventHandler<InvokeEvent_EventArgs> before = null;
					EventHandler<InvokeEvent_EventArgs> after = null;
					var events = client._parameters._invokeEventRemoveEvents;

					if (events != null)
					{
						before = events._beforeInvoke;
						after = events._afterInvoke;

						if (before != null || after != null)
						{
							args = new InvokeEvent_EventArgs();
							args.EventInfo = eventInfo;
							args.Handler = handler;
							args.Target = handler.Target;
						}
					}

					try
					{
						if (before != null)
						{
							before(this, args);
							if (!args.CanInvoke)
								return null;
						}

						eventInfo.RemoveEventHandler(obj, handler);

						lock(client._registeredEventsLock)
						{
							var registeredEvents = client._registeredEvents;
							if (registeredEvents != null)
							{
								Dictionary<Delegate, WeakList<object>> delegateDictionary;
								if (registeredEvents.TryGetValue(eventInfo, out delegateDictionary))
								{
									WeakList<object> weakList;
									if (delegateDictionary.TryGetValue(handler, out weakList))
										weakList.Remove(obj);
								}
							}
						}
					}
					catch(Exception exception)
					{
						if (after == null || !args.CanInvoke)
							throw;

						args.Exception = exception;
					}
					finally
					{
						if (args != null)
						{
							if (after != null && args.CanInvoke)
								after(this, args);

							var ex = args.Exception;
							if (ex != null)
								throw ex;
						}
					}

					return null;
				}
			);
		}
	}
}
