using System;
using Pfz.Extensions.MonitorLockExtensions;

namespace Pfz.Threading
{
	/// <summary>
	/// Class that returns the value of DateTime.Now.Ticks when GetValue is called 
	/// but also guarantees that two calls will generate different values, even if
	/// they are done one just after the other.
	/// </summary>
	public static class TicksOrIncrement
	{
		private static readonly object _lock = new object();
		private static long _lastValue = DateTime.Now.Ticks;
		private static readonly Random _random = new Random();
		
		private static int _randomIncrement = 1000;
		/// <summary>
		/// Gets or sets the random value used as the increment factor when
		/// two calls are made in the same microsecond.
		/// </summary>
		public static int RandomIncrement
		{
			get
			{
				return _randomIncrement;
			}
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", "The minimum accepted value is 1.");
				
				_randomIncrement = value;
			}
		}
		
		private static int _totalProcesses = 1;
		/// <summary>
		/// Gets or sets a value that is seen as the total number of processes using
		/// TicksOrIncrement. So, together with ProcessId, values are guaranteed to
		/// be exclusive.
		/// </summary>
		public static int TotalProcesses
		{
			get
			{
				return _totalProcesses;
			}
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", "TotalProcesses must be at least 1.");
				
				_totalProcesses = value;
			}
		}
		
		private static int _processId;
		/// <summary>
		/// Gets or sets the processid used to guarantee that never two process using
		/// TicksOrIncrement will get the same value.
		/// </summary>
		public static int ProcessId
		{
			get
			{
				return _processId;
			}
			set
			{
				if (value < 0 || value >= _totalProcesses)
					throw new ArgumentOutOfRangeException("value", "ProcessId must be >=0 and <=TotalProcesses.");
					
				_processId = value;
			}
		}

		/// <summary>
		/// Gets a new DateTime.Now.Ticks value or a random incremented value if
		/// this is a call done at the same microsecond of the last one.
		/// </summary>
		public static long GetNext()
		{
			long ticks = ((DateTime.Now.Ticks / _totalProcesses) * _totalProcesses ) + _processId;
			
			_lock.UnabortableLock
			(
				delegate
				{
					if (ticks <= _lastValue)
					{
						int addValue = (1 + _random.Next(_randomIncrement)) * _totalProcesses;
						ticks = (((_lastValue + addValue) / _totalProcesses) * _totalProcesses) + _processId;
					}
					
					_lastValue = ticks;
				}
			);
			
			return ticks;
		}
	}
}
