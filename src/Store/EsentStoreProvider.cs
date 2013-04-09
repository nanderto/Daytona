//-----------------------------------------------------------------------
// <copyright file="EsentStoreProvider.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//------------------------------------------------------------------
namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Daytona.Store;
    using Microsoft.Isam.Esent.Interop;
    using Newtonsoft.Json;
    using System.Transactions;
    using Phantom.PubSub;

    /// <summary>
    /// Esent Implementation of the IStoreProvider.
    /// </summary>
    /// <typeparam name="T">The type that this component handles</typeparam>
    public class EsentStoreProvider<T> : IStoreProvider<T> 
    {
        private static readonly object SyncLock = new object();
        
        private static volatile bool isStoreConfigured = false;
        
        private string longStoreName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="EsentStoreProvider{T}" /> class.
        /// </summary>
        public EsentStoreProvider()
        {
            this.Name = CleanupName(typeof(T).ToString());
            this.longStoreName = "PhantomPubSub";

            bool esentTempPathInUseExceptionTrue = false;
            int retryCount = 0;
            do
            {
                esentTempPathInUseExceptionTrue = false;
                try
                {
                    if (!isStoreConfigured)
                    {
                        lock (SyncLock)
                        {
                            if (!isStoreConfigured)
                            {
                                isStoreConfigured = this.ConfigureStore(this.Name, StoreTransactionOption.SupportTransactions);
                            }
                        }
                    }
                }
                catch (EsentTempPathInUseException)
                {
                    Trace.WriteLine("Path in use exception" + retryCount.ToString(CultureInfo.CurrentCulture));
                    esentTempPathInUseExceptionTrue = true;
                    ++retryCount;                    
                    if (retryCount > 4)
                    {
                        throw;
                    }

                    Thread.Sleep(retryCount * retryCount * 1000);
                }
            }          
            while (esentTempPathInUseExceptionTrue);           
        }       

        public string Name { get; set; }

        public int GetMessageCount()
        {
            // TODO: Implement this method
            throw new NotImplementedException();
        }

        public bool ConfigureStore(string storeName, StoreTransactionOption storeTransactionOption)
        {
            if (!EsentConfig.DoesDatabaseExist("PhantomPubsub.edb"))
            {
                EsentConfig.CreateDatabaseAndActorStore(storeName);
            }
            else
            {
                if (!EsentConfig.DoesStoreExist(storeName))
                {
                    EsentConfig.CreateMessageStore(storeName);
                }
            }

            return true;
        }

        public bool SubscriberGroupCompletedForMessage(string messageId)
        {
            return true;
        }

       
        public bool CheckItsStillInTheStore(string messageId)
        {
            using (var store = new EsentStore<T>(EsentInstanceService.Service.EsentInstance))
            {
                Repository<T> repository = new Repository<T>((IEsentStore<T>)store);
                var result = repository.PeekForMessage(messageId);
                return result;
            }         
        }
        
        //public void UpdateMessageStore(MessagePacket<T> messagePacket)
        //{
        //    this.UpdateMessage(messagePacket);
        //}

        //public string PutMessage(MessagePacket<T> messagePacket)
        //{
        //    if (messagePacket == null)
        //    {
        //        throw new ArgumentNullException("MessagePacket is null for store name: " + this.Name);
        //    }

        //    if (string.IsNullOrEmpty(this.Name))
        //    {
        //        throw new ArgumentNullException("Store provider name is null for Store name: " + this.Name);
        //    }

        //    string messageId = string.Empty;

        //    if (System.Transactions.Transaction.Current != null && System.Transactions.Transaction.Current.TransactionInformation.Status == TransactionStatus.Active)
        //    {
        //        var store = new EsentStore<T>(true);
        //        var resourceManager = new EsentStoreResourceManager<T>(store);
        //        using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required))
        //        {
        //            Repository<T> repository = new Repository<T>(resourceManager);
        //            messageId = repository.AddMessage(messagePacket).ToString(CultureInfo.CurrentCulture);
        //            scope.Complete();
        //        }
        //    }
        //    else
        //    {
        //        using (var store = new EsentStore<T>(true))
        //        {
        //            Repository<T> repository = new Repository<T>(store);
        //            messageId = repository.AddMessage(messagePacket).ToString(CultureInfo.CurrentCulture);
        //            store.Commit();
        //        }
        //    }
        //    return messageId;
        //}

        //public string UpdateMessage(MessagePacket<T> message)
        //{
        //    if (string.IsNullOrEmpty(this.Name))
        //    {
        //        throw new ArgumentNullException("Queue provider name is null for Queue name: " + this.Name);
        //    } 
            
        //    if (message == null)
        //    {
        //        throw new ArgumentNullException("Message is null for Queue name: " + this.Name);
        //    }
            
        //    string result = string.Empty;
        //    using (var store = new EsentStore<T>(true))
        //    {
        //        Repository<T> repository = new Repository<T>(store);
        //        repository.UpdateMessage(message);
        //        store.Commit();
        //    }

        //    return result;
        //}

        //public List<MessagePacket<T>> GetAllMessages()
        //{
        //    List<MessagePacket<T>> result = null;
        //    using (var store = new EsentStore<T>())
        //    {
        //        Repository<T> repository = new Repository<T>(store);
        //        result = repository.GetAllMessages().ToList();
        //    }

        //    return result;
        //}

        //public void DeleteMessage(string messageId)
        //{
        //    using (var store = new EsentStore<T>())
        //    {
        //        Repository<T> repository = new Repository<T>(store);
        //        repository.Delete(messageId);
        //    }
        //}

        //public int GetMessageCount()
        //{
        //    int result = 0;
        //    using (var store = new EsentStore<T>())
        //    {
        //        Repository<T> repository = new Repository<T>(store);
        //        result = repository.GetRecordCount();
        //    }

        //    return result;
        //}
       
        private static string CleanupName(string dirtyname)
        {
            return dirtyname.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        }    
    }
}
