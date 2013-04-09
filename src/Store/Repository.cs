//-----------------------------------------------------------------------
// <copyright file="Repository.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//---------------------------------------------------------------
namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Isam.Esent.Interop;

    /// <summary>
    /// Repository for accessing ESENT
    /// </summary>
    /// <typeparam name="T">The type that this component handles</typeparam>
    public class Repository<T>
    {
        private IEsentStore<T> store;
        //private EsentStoreResourceManager<T> rm;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository{T}" /> class.
        /// </summary>
        /// <param name="store">The store.</param>
        public Repository(IEsentStore<T> store)
        {
            this.store = store;         
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository{T}" /> class.
        /// </summary>
        /// <param name="resourceManager">The resource manager. Used for enlisting in external transaction. (not for Esent transaction)</param>
        /// <exception cref="System.ArgumentNullException">resourceManager was Null</exception>
        public Repository(EsentStoreResourceManager<T> resourceManager)
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException("resourceManager");
            }

            this.store = resourceManager.Store;
        }

        //public int AddMessage(MessagePacket<T> messagePacket)
        //{
        //    if (messagePacket == null)
        //    {
        //        throw new ArgumentNullException("messagePacket");
        //    }

        //    var messageId = this.store.AddMessageInTransaction(Serializer.GetSerializedBody(messagePacket), Serializer.GetSerializedMetadata(messagePacket.SubscriberMetadataList));
        //    this.store.AddMetadata(messageId, messagePacket.SubscriberMetadataList);
        //    return (int)messageId;
        //}

        //public void UpdateMessage(MessagePacket<T> messagePacket)
        //{
        //    if (messagePacket == null)
        //    {
        //        throw new ArgumentNullException("messagePacket");
        //    }

        //    var metaDataName = messagePacket.SubscriberMetadataList[0].Name;
        //    bool failedOrTimedOut = messagePacket.SubscriberMetadataList[0].FailedOrTimedOut;

        //    List<ISubscriberMetadata> metadatas = this.store.GetMetadata((int)messagePacket.MessageId);
        //    var metadata = metadatas.Single(md => md.Name == metaDataName);
        //    if (!metadata.Completed)
        //    {
        //        metadata.Completed = failedOrTimedOut == false;
        //    }

        //    var incompleteMetadatas = metadatas.Where<ISubscriberMetadata>(md => md.Completed != true);
        //    if (incompleteMetadatas.Count() == 0)
        //    {
        //        this.store.DeleteMessage(messagePacket.MessageId.ToString());
        //        this.store.DeleteMetadata(metadatas);
        //    }
        //    else
        //    {
        //        metadata.TimeToExpire = messagePacket.SubscriberMetadataList[0].TimeToExpire;
        //        metadata.StartTime = messagePacket.SubscriberMetadataList[0].StartTime;
        //        metadata.Name = messagePacket.SubscriberMetadataList[0].Name;
        //        metadata.RetryCount = messagePacket.SubscriberMetadataList[0].RetryCount;
        //        metadata.FailedOrTimedOutTime = messagePacket.SubscriberMetadataList[0].FailedOrTimedOutTime;
        //        metadata.FailedOrTimedOut = messagePacket.SubscriberMetadataList[0].FailedOrTimedOut;
        //        metadata.MessageId = (int)messagePacket.MessageId;
        //        if (messagePacket.MessageId == null || messagePacket.MessageId == 0)
        //        {
        //            throw new ArgumentException("MessagePacket.MessageID");
        //        }

        //        this.store.UpdateMetadata(metadata);
        //    }
        //}

        //public IEnumerable<MessagePacket<T>> GetAllMessages()
        //{
        //    return this.store.GetAllMessages();
        //}

        public int GetRecordCount()
        {
            return this.store.GetRecordCount();
        }

        public void Delete(string messageId)
        {
            this.store.DeleteMessage(messageId);
        }

        public bool PeekForMessage(string messageId)
        {
            return this.store.PeekForMessage(messageId);
        }

        public int? SavePayload(byte[] messageAsBytes, string tableName)
        {
            return this.store.SavePayload(messageAsBytes, tableName);
        }
    }
}
