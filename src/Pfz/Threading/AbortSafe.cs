using System;
using System.IO;
using System.Threading;
using Pfz.Extensions;

namespace Pfz.Threading
{
	/// <summary>
	/// Class with methods safe from "Thread.Abort()".
	/// </summary>
	public static class AbortSafe
	{
		#region WasAbortRequested
			/// <summary>
			/// Returns a value indicating if an abort was requested for this thread.
			/// </summary>
			public static bool WasAbortRequested
			{
				get
				{
					return (Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) == ThreadState.AbortRequested;
				}
			}
		#endregion
	
		#region AllowAbort
			/// <summary>
			/// This method will make a thread that is running in non-abortable manner 
			/// (ie, inside a finally block) to skip the rest of the actual finally block
			/// if an abort was requested.
			/// </summary>
			public static void AllowAbort()
			{
				if (WasAbortRequested)
					Thread.CurrentThread.Abort();
			}
		#endregion
		#region New
			/// <summary>
			/// Allocates a new object using it's default constructor and sets
			/// it's result value to the variable passed as out parameter.
			/// This will work completelly or fail completelly in case of an
			/// Abort call, so there is no risk of stopping in-the-middle of the work.
			/// </summary>
			public static void New<T>(out T variable)
			where
				T: new()
			{
				try
				{
				}
				finally
				{
					variable = new T();
				}
			}
		#endregion
		#region Run
			/// <summary>
			/// Runs a code block in an AbortSafe manner.
			/// Be careful when using this, as you must not avoid Aborts of long running
			/// code.
			/// </summary>
			public static void Run(Action code)
			{
				if (code == null)
					throw new ArgumentNullException("code");

				try
				{
				}
				finally
				{
					code();
				}
			}
		
			/// <summary>
			/// Runs a block of code, guaranting that:
			/// The allocation block will not be aborted.
			/// The finally block will be called, independent if the allocation block was
			/// run.
			/// The code block is the only one that could be aborted.
			/// </summary>
			public static void Run(Action allocationBlock, Action codeBlock, Action finallyBlock)
			{
				if (allocationBlock == null)
					throw new ArgumentNullException("allocationBlock");
			
				if (codeBlock == null)
					throw new ArgumentNullException("codeBlock");
			
				if (finallyBlock == null)
					throw new ArgumentNullException("finallyBlock");
		
				try
				{
					try
					{
					}
					finally
					{
						allocationBlock();
					}
				
					codeBlock();
				}
				finally
				{
					finallyBlock();
				}
			}
		#endregion
		#region ReadAllBytes
			/// <summary>
			/// Reads all bytes from a file, avoiding errors caused from Thread.Abort().
			/// </summary>
			public static byte[] ReadAllBytes(string path)
			{
				FileStream stream = null;
				try
				{
					Run(() => stream = File.OpenRead(path));
				
					int length = (int)stream.Length;
				
					byte[] bytes = new byte[length];
					stream.FullRead(bytes);
					return bytes;
				}
				finally
				{
					stream.CheckedDispose();
				}
			}
		#endregion
		#region WriteAllBytes
			/// <summary>
			/// Writes all bytes to a file, avoiding errors caused from Aborts.
			/// If an abort happens, the stream is closed and the file is deleted.
			/// </summary>
			public static void WriteAllBytes(string path, byte[] bytes)
			{
				FileStream stream = null;
				try
				{
					Run(() => stream = File.Create(path));
					stream.Write(bytes);
				}
				catch
				{
					if (stream != null)
					{
						stream.Dispose();
						stream = null;
					
						try
						{
							File.Delete(path);
						}
						catch
						{
						}
					}
				
					throw;
				}
				finally
				{
					stream.CheckedDispose();
				}
			}
		#endregion
		#region Using
			/// <summary>
			/// Executes the given blocks as if they where a using clause.
			/// The first block must return a disposable object. The second one will be
			/// the "body" executed inside the using clause.
			/// 
			/// It simulates:
			/// using(...allocationBlock...)
			/// {
			///		...codeBlock...
			/// }
			/// </summary>
			public static void Using<T>(Func<T> allocationBlock, Action<T> codeBlock)
			where
				T: IDisposable
			{
				if (allocationBlock == null)
					throw new ArgumentNullException("allocationBlock");
			
				if (codeBlock == null)
					throw new ArgumentNullException("codeBlock");
		
				T value = default(T);
				try
				{
					try
					{
					}
					finally
					{
						value = allocationBlock();
					}
				
					codeBlock(value);
				}
				finally
				{
					value.CheckedDispose();
				}
			}
		#endregion
	}
}
