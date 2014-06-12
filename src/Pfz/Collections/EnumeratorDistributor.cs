using System;
using System.Collections.Generic;
using Pfz.Caching;
using Pfz.Extensions.MonitorLockExtensions;
using Pfz.Threading;

namespace Pfz.Collections
{
	/// <summary>
	/// This class is responsible for distributing a single enumerator
	/// among many enumerator readers, considering such readers can
	/// "loose" some of the items.
	/// This is useful for senting, for example, web-cam frames. A high-speed
	/// client can receive all frames, while a slow client can receive frame
	/// 1, then frame 6, frame 12... but will still receive the "most recent"
	/// frames.
	/// You can inherit this class the the MultiClientEnumerator themselves
	/// if you must only send the difference between frames.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the item that the original enumerator returns.
	/// </typeparam>
	public class EnumeratorDistributor<T>:
		ThreadSafeExceptionAwareDisposable
	where
		T: class
	{
		internal AutoTrimHashSet<EnumeratorDistributorClient<T>> _clientEnumerators = new AutoTrimHashSet<EnumeratorDistributorClient<T>>();
	
		/// <summary>
		/// Creates a new Distributor over the given real enumerator.
		/// </summary>
		public EnumeratorDistributor(IFastEnumerator<T> baseEnumerator)
		{
			if (baseEnumerator == null)
				throw new ArgumentNullException("baseEnumerator");
			
			BaseEnumerator = baseEnumerator;
			
			UnlimitedThreadPool.Run(_KeepReading);
		}
		
		/// <summary>
		/// Disposes the base enumerator and the clients actually connected.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				var baseEnumerator = BaseEnumerator;
				if (baseEnumerator != null)
				{
					BaseEnumerator = null;
					baseEnumerator.Dispose();
				}

				var clientEnumerators = _clientEnumerators;
				if (clientEnumerators != null)
				{
					_clientEnumerators = null;

					lock(DisposeLock)
						foreach(var client in clientEnumerators)
							client.Dispose();

					clientEnumerators.Dispose();
				}

				_actualValue = null;
				
				var disposedEvent = Disposed;
				if (disposedEvent != null)
					disposedEvent(this, EventArgs.Empty);
			}
			
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// Gets the BaseEnumerator used by this distributor.
		/// </summary>
		public IFastEnumerator<T> BaseEnumerator { get; private set; }
		
		/// <summary>
		/// Called when this object is disposed.
		/// </summary>
		public event EventHandler Disposed;
		
		private volatile T _actualValue;
		
		/// <summary>
		/// Gets the Actual value without waiting.
		/// </summary>
		public T ActualValue
		{
			get
			{
				return _actualValue;
			}
		}
		
		private void _KeepReading()
		{
			try
			{
				var baseEnumerator = BaseEnumerator;
				while(true)
				{
					if (WasDisposed)
						return;
					
					var actualValue = baseEnumerator.GetNext();
					_actualValue = actualValue;
					
					DisposeLock.Lock
					(
						delegate
						{
							foreach(var client in _clientEnumerators)
								client._event.Set();
						}
					);
							
					if (actualValue == null)
					{
						Dispose();
						return;
					}
				}
			}
			catch(Exception exception)
			{
				if (!WasDisposed)
					Dispose(exception);
			}
		}
		
		/// <summary>
		/// Creates a client for this enumerator.
		/// Inheritors can initialize additional information before returning 
		/// the enumerator client to you.
		/// </summary>
		public virtual EnumeratorDistributorClient<T> CreateClient()
		{
			return new EnumeratorDistributorClient<T>(this);
		}
	}
}
