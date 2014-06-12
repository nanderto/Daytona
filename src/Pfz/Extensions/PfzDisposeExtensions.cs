using System;

namespace Pfz.Extensions
{
	/// <summary>
	/// Adds methods to the IDispose interface.
	/// </summary>
	public static class PfzDisposeExtensions
	{
		/// <summary>
		/// Disposes a disposable object if it is not null.
		/// </summary>
		public static void CheckedDispose(this IDisposable disposable)
		{
			if (disposable != null)
				disposable.Dispose();
		}
	}
}
