using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Pfz.Caching;
using Pfz.Extensions.MonitorLockExtensions;

namespace Pfz.Threading
{
	#region ActionRunner - for action without parameters
		/// <summary>
		/// Class that creates a thread to run Actions as messages. It only creates 
		/// one thread to process all messages. Use it only when you know you want to
		/// process many messages asynchronously, but don't want (or can't) use ThreadPool 
		/// threads.
		/// </summary>
		public sealed class ActionRunner:
			ThreadSafeDisposable
		{
			private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
			private volatile Queue<Action> _queue = new Queue<Action>();
		
			/// <summary>
			/// Creates a new action runner.
			/// </summary>
			public ActionRunner()
			{
				try
				{
					GCUtils.Collected += _Collected;
					var pair = new KeyValuePair<ManualResetEvent, WeakReference>(_manualResetEvent, new WeakReference(this));
					UnlimitedThreadPool.Run(_Run, pair);
				}
				catch
				{
					Dispose();
					throw;
				}
			}
		
			/// <summary>
			/// Frees all used resources.
			/// </summary>
			[SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_manualResetEvent")]
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_manualResetEvent.Set();
					GCUtils.Collected -= _Collected;
				}
			
				base.Dispose(disposing);
			}
			private void _Collected()
			{
				try
				{
					DisposeLock.Lock
					(
						delegate
						{
							if (WasDisposed)
							{
								GCUtils.Collected -= _Collected;
								return;
							}
						
							_queue = new Queue<Action>(_queue);
						}
					);
				}
				catch
				{
				}
			}
		
			private static void _Run(KeyValuePair<ManualResetEvent, WeakReference> pair)
			{
				var manualResetEvent = pair.Key;
				var weakReference = pair.Value;
				var thread = Thread.CurrentThread;

				while(true)
				{
					thread.IsBackground = true;

					#if DEBUG
						bool signaled = manualResetEvent.WaitOne(6000);
					#else
						bool signaled = manualResetEvent.WaitOne(60000);
					#endif
					thread.IsBackground = false;

					var actionRunner = (ActionRunner)weakReference.Target;
					if (actionRunner == null)
						return;

					if (!signaled)
						continue;

					if (!actionRunner._Run2())
						return;
				}
			}

			private bool _Run2()
			{
				while(true)
				{
					Action action = null;
					
					bool mustBreak = false;
					bool mustReturn = false;
					
					DisposeLock.UnabortableLock
					(
						delegate
						{
							if (WasDisposed)
							{
								_manualResetEvent.Close();
								mustReturn = true;
								return;
							}

							var queue = _queue;
							if (queue.Count == 0)
							{
								_manualResetEvent.Reset();
								mustBreak = true;
								return;
							}
								
							action = queue.Dequeue();
						}
					);
					
					if (mustReturn)
						return false;
					
					if (mustBreak)
						break;
					
					action();
				}

				return true;
			}
		
			/// <summary>
			/// Runs the given action.
			/// </summary>
			/// <param name="action">The action to run.</param>
			public void Run(Action action)
			{
				if (action == null)
					throw new ArgumentNullException("action");
				
				DisposeLock.UnabortableLock
				(
					delegate
					{
						CheckUndisposed();
					
						_queue.Enqueue(action);
					}
				);
			
				_manualResetEvent.Set();
			}
		}
	#endregion
	#region ActionRunner<T>
		/// <summary>
		/// Class that creates a thread to run Actions as messages. It only creates 
		/// one thread to process all messages. Use it only when you know you want to
		/// process many messages asynchronously, but don't want (or can't) use ThreadPool 
		/// threads.
		/// <typeparam name="T">The type of the actions this executor invokes.</typeparam>
		/// </summary>
		public sealed class ActionRunner<T>:
			ThreadSafeDisposable
		{
			private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);
			private volatile Queue<KeyValuePair<Action<T>, T>> _queue = new Queue<KeyValuePair<Action<T>, T>>();
		
			/// <summary>
			/// Creates a new action runner.
			/// </summary>
			public ActionRunner()
			{
				try
				{
					GCUtils.Collected += _Collected;
					var pair = new KeyValuePair<ManualResetEvent, WeakReference>(_manualResetEvent, new WeakReference(this));
					UnlimitedThreadPool.Run(_Run, pair);
				}
				catch
				{
					Dispose();
					throw;
				}
			}
		
			/// <summary>
			/// Frees all used resources.
			/// </summary>
			/// <param name="disposing"></param>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_manualResetEvent.Set();
					GCUtils.Collected -= _Collected;
				}
			
				base.Dispose(disposing);
			}
			private void _Collected()
			{
				try
				{
					DisposeLock.Lock
					(
						delegate
						{
							if (WasDisposed)
							{
								GCUtils.Collected -= _Collected;
								return;
							}
						
							_queue = new Queue<KeyValuePair<Action<T>, T>>(_queue);
						}
					);
				}
				catch
				{
				}
			}
		
			private static void _Run(KeyValuePair<ManualResetEvent, WeakReference> pair)
			{
				var manualResetEvent = pair.Key;
				var weakReference = pair.Value;
				var thread = Thread.CurrentThread;

				while(true)
				{
					thread.IsBackground = true;

					#if DEBUG
						bool signaled = manualResetEvent.WaitOne(6000);
					#else
						bool signaled = manualResetEvent.WaitOne(60000);
					#endif
					thread.IsBackground = false;

					var actionRunner = (ActionRunner<T>)weakReference.Target;
					if (actionRunner == null)
						return;

					if (!signaled)
						continue;

					if (!actionRunner._Run2())
						return;
				}
			}
			private bool _Run2()
			{
				while(true)
				{
					KeyValuePair<Action<T>, T> pair = new KeyValuePair<Action<T>, T>();
					
					bool mustBreak = false;
					bool mustReturn = false;
					
					DisposeLock.UnabortableLock
					(
						delegate
						{
							if (WasDisposed)
							{
								_manualResetEvent.Close();
								mustReturn = true;
								return;
							}

							var queue = _queue;
							if (queue.Count == 0)
							{
								_manualResetEvent.Reset();
								mustBreak = true;
								return;
							}
								
							pair = queue.Dequeue();
						}
					);
					
					if (mustReturn)
						return false;
					
					if (mustBreak)
						break;
				
					pair.Key(pair.Value);	
				}

				return true;
			}
		
			/// <summary>
			/// Runs the given action.
			/// </summary>
			/// <param name="action">The action to run.</param>
			/// <param name="value">The value for the given action.</param>
			public void Run(Action<T> action, T value)
			{
				if (action == null)
					throw new ArgumentNullException("action");
				
				DisposeLock.UnabortableLock
				(
					delegate
					{
						CheckUndisposed();
					
						var pair = new KeyValuePair<Action<T>, T>(action, value);
						_queue.Enqueue(pair);
					}
				);
			
				_manualResetEvent.Set();
			}
		}
	#endregion
}
