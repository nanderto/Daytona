using System;
using System.Threading;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Collections
{
	/// <summary>
	/// This class connects to a MultiClientEnumeratorDistributor and is able
	/// to use GetNext to get a next frame when one is available, while it is 
	/// also able to "loose" values if the real enumerator is running faster 
	/// than this client. This is useful when getting frames from a web-cam, 
	/// for example.
	/// </summary>
	public class EnumeratorDistributorClient<T>:
		ThreadSafeDisposable,
		IFastEnumerator<T>
	where
		T: class
	{
		internal AutoResetEvent _event = new AutoResetEvent(false);
		
		#region Constructor
			/// <summary>
			/// Creates a new multi-client enumerator connected to the given distributor.
			/// </summary>
			public EnumeratorDistributorClient(EnumeratorDistributor<T> distributor)
			{
				if (distributor == null)
					throw new ArgumentNullException("distributor");
				
				Distributor = distributor;
				distributor.DisposeLock.UnabortableLock
				(
					delegate
					{
						distributor.CheckUndisposed();
						distributor._clientEnumerators.Add(this);
					}
				);
			}
		#endregion
		#region Dispose
			/// <summary>
			/// Releases the resources used by this enumerator and removes it from
			/// the distributor list.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					var distributor = Distributor;
					if (distributor != null)
					{
						Distributor = null;
						
						distributor.DisposeLock.UnabortableLock
						(
							delegate
							{
								var clientEnumerators = distributor._clientEnumerators;
								if (clientEnumerators != null)
									clientEnumerators.Remove(this);
							}
						);
					}
					
					var are = _event;
					if (are != null)
						are.Set();
				}
			
				base.Dispose(disposing);
			}
		#endregion
		
		#region Property - Distributor
			/// <summary>
			/// Gets the Distributor used by this enumerator.
			/// </summary>
			public EnumeratorDistributor<T> Distributor { get; private set; }
		#endregion
		#region Method - GetNext
			/// <summary>
			/// Gets the actual value of the distributor or waits until a new
			/// value is available.
			/// </summary>
			public virtual T GetNext()
			{
				if (WasDisposed)
					return null;
				
				_event.WaitOne();
				
				var distributor = Distributor;
				if (distributor == null)
					return null;
					
				T value = null;

				distributor.UnabortableLock
				(
					delegate
					{
						if (!distributor.WasDisposed)
							value = distributor.ActualValue;
					}
				);
				
				return value;
			}
		#endregion
	}
}
