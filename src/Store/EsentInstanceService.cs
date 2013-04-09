//-----------------------------------------------------------------------
// <copyright file="EsentInstanceService.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//--------------------------------------------------------------------
namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Isam.Esent.Interop;
    using Phantom.PubSub;

    /// <summary>
    /// EsentInstanceService provides access to the ESENT instance. only one instance is allowed in an application and this service which is 
    /// implemented as a singleton provides access to it
    /// </summary>
    /// <typeparam name="T">The type that this component handles</typeparam>
    public sealed class EsentInstanceService : IDisposable
    {
        /// <summary>
        /// The esent instance service
        /// </summary>
        private static volatile EsentInstanceService esentInstanceService;

        private static object syncRoot = new object();
        
        private static object syncRefCount = new object();
        
        private static int refCount = 0;
        
        private static Timer timer;
        
        private Instance esentInstance;
        
        private string databaseName = string.Empty;
        
        private string typeName = string.Empty;

        /// <summary>
        /// Prevents a default instance of the <see cref="EsentInstanceService{T}" /> class from being created.
        /// </summary>
        private EsentInstanceService() 
        {
            this.databaseName = EsentConfig.DatabaseName;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="EsentInstanceService{T}" /> class.
        /// </summary>
        ~EsentInstanceService()
        {
            this.Dispose(false);
        }

        public static EsentInstanceService Service
        {
            get
            {
                if (esentInstanceService == null)
                {
                    lock (syncRoot)
                    {
                        if (esentInstanceService == null)
                        {
                            esentInstanceService = new EsentInstanceService();
                        }
                    }
                }

                return esentInstanceService;
            }
        }

        public Instance EsentInstance
        {
            get
            {
                lock (syncRefCount)
                {
                    if (this.esentInstance == null)
                    {
                        refCount = ++refCount;
                        Trace.WriteLineIf(Utils.EsentSwitch.TraceInfo, "Creating new instance");
                        Trace.WriteLineIf(Utils.EsentSwitch.TraceVerbose, "RefCount: " + refCount.ToString(CultureInfo.CurrentCulture));
                        this.esentInstance = new Instance("Instance");
                        this.esentInstance.Parameters.CircularLog = true;
                        this.esentInstance.Init();
                        using (var session = new Session(this.esentInstance))
                        {
                            Api.JetAttachDatabase(session, this.databaseName, AttachDatabaseGrbit.None);
                        }
                    }
                    else
                    {
                        refCount = ++refCount;                        
                    }

                    Trace.WriteLineIf(Utils.EsentSwitch.TraceVerbose, "RefCount: " + refCount.ToString(CultureInfo.CurrentCulture));
                    return this.esentInstance;
                }
            }
        }

        /// <summary>
        /// Schedules the disposing of esent instance.Creating the Instance is expensive by having a delay in the disposing of the instance we can give enough time for 
        /// another object tine to request the instance. Delay is set for 10 seconds and it gets canceled each time a new request 
        /// for the instance is made
        /// </summary>
        public void ScheduleDisposingOfEsent()
        {
            if (timer == null)
            {
                timer = new Timer(this.OnTimerEvent, null, 10000, 10000);
            }
            else
            {
                timer.Change(10000, 100000);
            }
        }

        /// <summary>
        /// Disposes the of esent instance immediatly.
        /// </summary>
        public void DisposeOfEsentInstanceImmediatly()
        {
            if (this.esentInstance != null)
            {
                this.esentInstance.Dispose();
                this.esentInstance = null;
            }

            Trace.WriteLineIf(Utils.EsentSwitch.TraceInfo, "I just disposed of the Instance Immediatly");
        }

        /// <summary>
        /// Disposes the of esent instance. Decrements the counter of references to the Instance and if the counter equals 0
        /// it will dispose of it
        /// </summary>
        public void DisposeOfEsentInstance()
        {
            lock (syncRefCount)
            {
                DecrementRefCount();
                if (refCount == 0)
                {
                    this.ScheduleDisposingOfEsent();
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleanups the name.
        /// </summary>
        /// <param name="dirtyname">The dirtyname.</param>
        /// <returns>Name minus { } _ . characters</returns>
        private static string CleanupName(string dirtyname)
        {
            return dirtyname.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        }

        /// <summary>
        /// Decrements the ref count.
        /// </summary>
        private static void DecrementRefCount()
        {
            refCount = --refCount;
            Trace.WriteLineIf(Utils.EsentSwitch.TraceVerbose, "RefCount: " + refCount.ToString(CultureInfo.CurrentCulture));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (syncRefCount)
                {
                    DecrementRefCount();
                    if (refCount == 0)
                    {
                        this.ScheduleDisposingOfEsent();
                    }
                }
            }
        }

        /// <summary>
        /// Called when [timer event]. Sets counter to off and if the reference count has hit 0 it will dispose of the Esent instance
        /// </summary>
        /// <param name="state">The state.</param>
        private void OnTimerEvent(object state)
        {
            timer.Change(Timeout.Infinite, 100000);
            lock (syncRefCount)
            {
                if (refCount == 0)
                {
                    this.esentInstance.Dispose();
                    this.esentInstance = null;
                    Trace.WriteLineIf(Utils.EsentSwitch.TraceInfo, "I just disposed of the Instance");
                }
            }
        }
    }
}
