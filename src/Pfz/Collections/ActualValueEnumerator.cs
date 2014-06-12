using System.Collections.Generic;
using System.Threading;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Collections
{
	/// <summary>
	/// This enumerator returns a "next value" every time the actual value is set, but
	/// if many sets are done before the client is able to process them, the 
	/// intermediate values are lost, so only the "actual" one is got.
	/// </summary>
	/// <typeparam name="T">The type of the values.</typeparam>
	public class ActualValueEnumerator<T>:
		ThreadSafeDisposable,
		IFastEnumerator<T>
	where
		T: class
	{
		private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

		#region Dispose
			/// <summary>
			/// Only sets the event so any waiting thread is free.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_actualValue = null;

					var manualResetEvent = _manualResetEvent;
					if (manualResetEvent != null)
						manualResetEvent.Set();
				}
				
				base.Dispose(disposing);
			}
		#endregion
		
		#region Property ActualValue
			private T _actualValue;
			/// <summary>
			/// Gets or sets the actual value.
			/// </summary>
			public T ActualValue
			{
				get
				{
					T result = null;
					
					DisposeLock.UnabortableLock
					(
						() =>
						{
							if (!WasDisposed)
								result = _actualValue;
						}
					);
					
					return result;
				}
				set
				{
					DisposeLock.UnabortableLock
					(
						() =>
						{
							CheckUndisposed();
							
							_actualValue = value;
							_manualResetEvent.Set();
						}
					);
				}
			}
		#endregion
		#region Methods
			#region GetNext
				/// <summary>
				/// Gets the next value.
				/// </summary>
				public T GetNext()
				{
					if (WasDisposed)
						return null;
					
					_manualResetEvent.WaitOne();
					
					T result = null;
					DisposeLock.UnabortableLock
					(
						() =>
						{
							if (!WasDisposed)
							{
								result = _actualValue;
								_manualResetEvent.Reset();
							}
						}
					);
					return result;
				}
			#endregion
			#region _GetEnumerator
				private IEnumerator<T> _GetEnumerator()
				{
					while(true)
					{
						var value = GetNext();
						
						if (value == null)
							yield break;
						
						yield return value;
					}
				}
			#endregion
		#endregion
	}
}
