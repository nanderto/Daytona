//-----------------------------------------------------------------------
// <copyright file="EsentStore.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-------------------------------------------------------------------
namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Isam.Esent.Interop;
    using Newtonsoft.Json;

    /// <summary>
    /// EsentStore encapsulates all access to the ESENT instance
    /// </summary>
    /// <typeparam name="T">The type that this component handles</typeparam>
    public class EsentStore<T> : IUnitOfWork, IDisposable, IEsentStore<T>
    {
       // private const string SubscriberMetadataTableName = "subscribermetadata"; 
        
        //private const string DatabaseName = "PhantomPubSub.edb";

        private Instance instance = null;
        
        private Session currentSession;
        
        private JET_DBID dbid;
        
        private string longStoreName;
        
        private Transaction transaction;
        
        private bool defaultForWrite = false;

        public EsentStore(Instance esentInstance)
        {
            this.longStoreName = CleanupName(typeof(T).ToString());
            this.instance = esentInstance;
            this.currentSession = this.OpenSession();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="EsentStore{T}" /> class.
        /// </summary>
        //public EsentStore()
        //{
        //    this.longStoreName = CleanupName(typeof(T).ToString());
        //    if (this.instance == null)
        //    {
        //        this.instance = EsentInstanceService.Service.EsentInstance;
        //    }

        //    this.currentSession = this.OpenSession();
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="EsentStore{T}" /> class.
        /// </summary>
        /// <param name="forWrite">if set to <c>true</c> [for write].</param>
        //public EsentStore(bool forWrite)
        //{
        //    this.longStoreName = CleanupName(typeof(T).ToString());
        //    if (this.instance == null)
        //    {
        //        this.instance = EsentInstanceService.Service.EsentInstance;
        //    }

        //    this.defaultForWrite = forWrite;
        //    this.currentSession = this.OpenSession();
        //}

        /// <summary>
        /// Finalizes an instance of the <see cref="EsentStore{T}" /> class.
        /// </summary>
        ~EsentStore()
        {
            this.Dispose(false);
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
        /// Creates the session. opens the database with the session and if DefaultForWrite has been set to true will also create a transaction 
        /// </summary>
        /// <returns>An opened Session</returns>
        public Session OpenSession()
        {
            this.currentSession = new Session(this.instance);
            Api.JetOpenDatabase(this.currentSession, EsentConfig.DatabaseName, null, out this.dbid, OpenDatabaseGrbit.None);
            if (this.defaultForWrite)
            {
                this.transaction = new Transaction(this.currentSession);
            }

            return this.currentSession;
        }

        /// <summary>
        /// Opens the transaction, using the current session.
        /// </summary>
        /// <returns>A new transaction</returns>
        public Transaction OpenTransaction()
        {
            this.transaction = new Transaction(this.currentSession);
            return this.transaction;
        }

        /// <summary>
        /// Closes the session. If a transaction is present it will comit it
        /// closes the current session
        /// </summary>
        public void CloseSession()
        {
            if (this.transaction != null)
            {
                if (this.transaction.IsInTransaction)
                {
                    this.transaction.Commit(CommitTransactionGrbit.LazyFlush);
                }

                this.transaction.Dispose();
            }

            if (this.currentSession != null)
            {
                this.currentSession.Dispose();
            }
        }

        /// <summary>
        /// Commits the transaction.
        /// </summary>
        public void Commit()
        {
            if (this.transaction != null)
            {
                if (this.transaction.IsInTransaction)
                {
                    this.transaction.Commit(CommitTransactionGrbit.LazyFlush);
                }
            }
        }

        /// <summary>
        /// Rollbacks this transaction.
        /// </summary>
        public void Rollback()
        {
            if (this.transaction != null)
            {
                if (this.transaction.IsInTransaction)
                {
                    this.transaction.Rollback();
                }
            }
        }

        
        public int? SavePayload(byte[] body, string tableName)
        {
            tableName = tableName + "payload";
            int? autoinc = null;
            using (var table = new Table(this.currentSession, this.dbid, tableName, OpenTableGrbit.None))
            {
                IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);
                JET_COLUMNID autoincColumn = columnids["id"];
                JET_COLUMNID columnidPayload  = columnids["payload"];

                using (var internalTransaction = new Transaction(this.currentSession))
                {
                    using (var update = new Update(this.currentSession, table, JET_prep.Insert))
                    {
                        Api.SetColumn( this.currentSession, table, columnidPayload, body, SetColumnGrbit.None);

                        autoinc = Api.RetrieveColumnAsInt32(
                            this.currentSession,
                            table,
                            autoincColumn,
                            RetrieveColumnGrbit.RetrieveCopy);
                        update.Save();
                    }

                    internalTransaction.Commit(CommitTransactionGrbit.LazyFlush);
                }
            }

            return autoinc;
        }

        ///// <summary>
        ///// Adds the metadata.
        ///// </summary>
        ///// <param name="messageId">The message id.</param>
        ///// <param name="metadatas">The metadatas.</param>
        ///// <exception cref="System.ArgumentNullException">Argument Null Exception</exception>
        //public void AddMetadata(int? messageId, List<ISubscriberMetadata> metadatas)
        //{
        //    if (messageId == null)
        //    {
        //        throw new ArgumentNullException("messageId");
        //    }

        //    if (metadatas == null)
        //    {
        //        throw new ArgumentNullException("metadatas");
        //    }

        //    int? autoinc = null;
        //    using (var table = new Table(this.currentSession, this.dbid, SubscriberMetadataTableName, OpenTableGrbit.None))
        //    {
        //        IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);
        //        JET_COLUMNID autoincColumn = columnids["id"];
        //        JET_COLUMNID columnidName = columnids["name"];
        //        JET_COLUMNID columnidMessageid = columnids["messageid"];
        //        JET_COLUMNID columnidTimetoexpire = columnids["timetoexpire"];
        //        JET_COLUMNID columnidCompleted = columnids["completed"];
        //        JET_COLUMNID columnidFailedortimedout = columnids["failedortimedout"];
        //        JET_COLUMNID columnidStarttime = columnids["starttime"];
        //        JET_COLUMNID columnidFailedortimeouttime = columnids["failedortimeouttime"];
        //        JET_COLUMNID columnidRetrycount = columnids["retrycount"];

        //        using (var internalTransaction = new Transaction(this.currentSession))
        //        {
        //            foreach (var item in metadatas)
        //            {
        //                using (var update = new Update(this.currentSession, table, JET_prep.Insert))
        //                {
        //                    Api.SetColumn(this.currentSession, table, columnidName, item.Name, Encoding.Unicode);
        //                    Api.SetColumn(this.currentSession, table, columnidMessageid, (int)messageId);
        //                    Api.SetColumn(this.currentSession, table, columnidTimetoexpire, item.TimeToExpire.TotalMilliseconds);
        //                    Api.SetColumn(this.currentSession, table, columnidCompleted, item.Completed);
        //                    Api.SetColumn(this.currentSession, table, columnidFailedortimedout, item.FailedOrTimedOut);
        //                    Api.SetColumn(this.currentSession, table, columnidStarttime, item.StartTime);
        //                    Api.SetColumn(this.currentSession, table, columnidFailedortimeouttime, item.FailedOrTimedOutTime);
        //                    Api.SetColumn(this.currentSession, table, columnidRetrycount, (short)item.RetryCount);

        //                    autoinc = Api.RetrieveColumnAsInt32(
        //                        this.currentSession,
        //                        table,
        //                        autoincColumn,
        //                        RetrieveColumnGrbit.RetrieveCopy);
        //                    update.SaveAndGotoBookmark();
        //                }
        //            }
                    
        //            internalTransaction.Commit(CommitTransactionGrbit.LazyFlush);
        //        }
        //    }
        //}

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="metadataId">The metadata id.</param>
        /// <returns>Subscriber Metadata</returns>
        //public ISubscriberMetadata GetMetadata(int messageId, int metadataId)
        //{
        //    using (var table = new Table(this.currentSession, this.dbid, SubscriberMetadataTableName, OpenTableGrbit.None))
        //    {
        //        IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);
                
        //        JET_COLUMNID columnidId = columnids["id"];
        //        JET_COLUMNID columnidName = columnids["name"];
        //        JET_COLUMNID columnidMessageid = columnids["messageid"];
        //        JET_COLUMNID columnidTimetoexpire = columnids["timetoexpire"];
        //        JET_COLUMNID columnidCompleted = columnids["completed"];
        //        JET_COLUMNID columnidFailedortimedout = columnids["failedortimedout"];
        //        JET_COLUMNID columnidStarttime = columnids["starttime"];
        //        JET_COLUMNID columnidFailedortimeouttime = columnids["failedortimeouttime"];
        //        JET_COLUMNID columnidRetrycount = columnids["retrycount"];

        //        SeekToId(this.currentSession, table, Convert.ToInt32(metadataId));
        //        return new SubscriberMetadata()
        //        {
        //            Id = Api.RetrieveColumnAsInt32(this.currentSession, table, columnidId).ToString(),
        //            Name = Api.RetrieveColumnAsString(this.currentSession, table, columnidName),
        //            MessageId = (int)Api.RetrieveColumnAsInt32(this.currentSession, table, columnidMessageid),
        //            Completed = (bool)Api.RetrieveColumnAsBoolean(this.currentSession, table, columnidCompleted),
        //            FailedOrTimedOut = (bool)Api.RetrieveColumnAsBoolean(this.currentSession, table, columnidFailedortimedout),
        //            FailedOrTimedOutTime = (DateTime)Api.RetrieveColumnAsDateTime(this.currentSession, table, columnidFailedortimeouttime),
        //            RetryCount = (int)Api.RetrieveColumnAsInt16(this.currentSession, table, columnidRetrycount),
        //            StartTime = (DateTime)Api.RetrieveColumnAsDateTime(this.currentSession, table, columnidStarttime),
        //            TimeToExpire = new TimeSpan(0, 0, 0, 0, (int)Api.RetrieveColumnAsInt32(this.currentSession, table, columnidTimetoexpire))
        //        };
        //    }
        //}

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <returns>list of ISubscriberMetadata</returns>
        //public List<ISubscriberMetadata> GetMetadata(int messageId)
        //{
        //    List<ISubscriberMetadata> metadatas = new List<ISubscriberMetadata>();
        //    using (var table = new Table(this.currentSession, this.dbid, SubscriberMetadataTableName, OpenTableGrbit.None))
        //    {
        //        metadatas = this.GetMetadata(messageId, table);
        //    }

        //    return metadatas;
        //}

        /// <summary>
        /// Updates the metadata.
        /// </summary>
        /// <param name="subscriberMetadata">The subscriber metadata.</param>
        /// <exception cref="System.ArgumentNullException">Argument Null Exception</exception>
        //public void UpdateMetadata(ISubscriberMetadata subscriberMetadata)
        //{
        //    if (subscriberMetadata == null)
        //    {
        //        throw new ArgumentNullException("subscriberMetadata");
        //    }

        //    using (var table = new Table(this.currentSession, this.dbid, SubscriberMetadataTableName, OpenTableGrbit.None))
        //    {
        //        IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);

        //        JET_COLUMNID columnidName = columnids["name"];
        //        JET_COLUMNID columnidMessageid = columnids["messageid"];
        //        JET_COLUMNID columnidTimetoexpire = columnids["timetoexpire"];
        //        JET_COLUMNID columnidCompleted = columnids["completed"];
        //        JET_COLUMNID columnidFailedortimedout = columnids["failedortimedout"];
        //        JET_COLUMNID columnidStarttime = columnids["starttime"];
        //        JET_COLUMNID columnidFailedortimeouttime = columnids["failedortimeouttime"];
        //        JET_COLUMNID columnidRetrycount = columnids["retrycount"];

        //        SeekToId(this.currentSession, table, Convert.ToInt32(subscriberMetadata.Id, CultureInfo.CurrentCulture));
                
        //        using (var update = new Update(this.currentSession, table, JET_prep.Replace))
        //        {
        //            Api.SetColumn(this.currentSession, table, columnidName, subscriberMetadata.Name, Encoding.Unicode);
        //            Api.SetColumn(this.currentSession, table, columnidMessageid, (int)subscriberMetadata.MessageId);
        //            Api.SetColumn(this.currentSession, table, columnidTimetoexpire, subscriberMetadata.TimeToExpire.TotalMilliseconds);
        //            Api.SetColumn(this.currentSession, table, columnidCompleted, subscriberMetadata.Completed);
        //            Api.SetColumn(this.currentSession, table, columnidFailedortimedout, subscriberMetadata.FailedOrTimedOut);
        //            Api.SetColumn(this.currentSession, table, columnidStarttime, subscriberMetadata.StartTime);
        //            Api.SetColumn(this.currentSession, table, columnidFailedortimeouttime, subscriberMetadata.FailedOrTimedOutTime);
        //            Api.SetColumn(this.currentSession, table, columnidRetrycount, (short)subscriberMetadata.RetryCount);
        //            update.Save();
        //        }
        //    }
        //}

        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        public void DeleteMessage(string messageId)
        {
            using (var table = new Table(this.currentSession, this.dbid, "messages", OpenTableGrbit.None))
            {
                SeekToId(this.currentSession, table, Convert.ToInt32(messageId, CultureInfo.CurrentCulture));
                Api.JetDelete(this.currentSession, table);
            }
        }

        /// <summary>
        /// Deletes the metadata.
        /// </summary>
        /// <param name="metadatas">The metadatas.</param>
        /// <exception cref="System.ArgumentNullException">Argument Null Exception</exception>
        //public void DeleteMetadata(IEnumerable<ISubscriberMetadata> metadatas)
        //{
        //    if (metadatas == null)
        //    {
        //        throw new ArgumentNullException("metadatas");
        //    }

        //    using (var table = new Table(this.currentSession, this.dbid, SubscriberMetadataTableName, OpenTableGrbit.None))
        //    {
        //        foreach (var item in metadatas)
        //        {
        //            SeekToId(this.currentSession, table, Convert.ToInt32(item.Id, CultureInfo.CurrentCulture));
        //            Api.JetDelete(this.currentSession, table);
        //        }          
        //    }
        //}

        /// <summary>
        /// Gets all messages.
        /// </summary>
        /// <returns>List od MessagePackets</returns>
        //public IEnumerable<MessagePacket<T>> GetAllMessages()
        //{
        //    using (var table = new Table(this.currentSession, this.dbid, "messages", OpenTableGrbit.None))
        //    {
        //        IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);
        //        Api.JetSetCurrentIndex(this.currentSession, table, null);
        //        return this.GetAllRecords(this.currentSession, table, columnids);
        //    }
        //}

        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        ///// <returns>Message Packet</returns>
        //public MessagePacket<T> GetMessage(int messageId)
        //{
        //    using (var table = new Table(this.currentSession, this.dbid, "messages", OpenTableGrbit.None))
        //    {
        //        if (SeekToId(this.currentSession, table, messageId))
        //        {
        //            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);
        //            var result = GetOneRow(this.currentSession, table, columnids);
        //            result.ReplaceMetadatas(this.GetMetadata(messageId));
        //            return result;
        //        }

        //        return null;
        //    }
        //}

        /// <summary>
        /// Gets the record count.
        /// </summary>
        /// <returns>the count</returns>
        public int GetRecordCount()
        {
            using (var table = new Table(this.currentSession, this.dbid, "messages", OpenTableGrbit.None))
            {
                Api.JetSetCurrentIndex(this.currentSession, table, null);
                return GetRecordCount(this.currentSession, table);
            }
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Peeks for message.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <returns>TRue if it exists, false if it is not still available</returns>
        public bool PeekForMessage(string messageId)
        {
            bool result = false;
            using (var table = new Table(this.currentSession, this.dbid, "messages", OpenTableGrbit.None))
            {
                if (SeekToId(this.currentSession, table, Convert.ToInt32(messageId, CultureInfo.CurrentCulture)))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.currentSession != null)
                {
                    this.currentSession.Dispose();
                }

                
            }
        }

        /// <summary>
        /// Seeks to id. Moves database cursor to correct requested position
        /// </summary>
        /// <param name="sesid">The sesid.</param>
        /// <param name="tableid">The tableid.</param>
        /// <param name="id">The id.</param>
        /// <param name="indexName">Name of the index.</param>
        /// <returns>True if it moved cursor to correct position, false if it failed</returns>
        private static bool SeekToId(JET_SESID sesid, JET_TABLEID tableid, int id, string indexName = "Default")
        {
            if (indexName == "Default")
            {
                Api.JetSetCurrentIndex(sesid, tableid, null);
            }
            else
            {
                Api.JetSetCurrentIndex(sesid, tableid, indexName);
            }

            Api.MakeKey(sesid, tableid, id, MakeKeyGrbit.NewKey);

            return Api.TrySeek(sesid, tableid, SeekGrbit.SeekEQ);
        }

        /// <summary>
        /// Gets the record count.
        /// </summary>
        /// <param name="sesid">The sesid.</param>
        /// <param name="tableid">The tableid.</param>
        /// <returns>The count</returns>
        private static int GetRecordCount(JET_SESID sesid, JET_TABLEID tableid)
        {
            int count = 0;
            if (Api.TryMoveFirst(sesid, tableid))
            {
                do
                {
                    count++;
                }
                while (Api.TryMoveNext(sesid, tableid));
            }

            return count;
        }

        /// <summary>
        /// Gets the one row. 
        /// </summary>
        /// <param name="sesid">The sesid.</param>
        /// <param name="tableid">The tableid.</param>
        /// <param name="columnids">The columnids.</param>
        /// <returns>database values from this row </returns>
        //private static MessagePacket<T> GetOneRow(JET_SESID sesid, JET_TABLEID tableid, IDictionary<string, JET_COLUMNID> columnids)
        //{
        //    JET_COLUMNID columnidId = columnids["id"];
        //    JET_COLUMNID columnidMessage = columnids["message"];
        //    JET_COLUMNID columnidMetaData = columnids["metadata"];
        //    string serializedBody = string.Empty;
        //    string serializedMetadata = string.Empty;
        //    int? messageId;

        //    messageId = Api.RetrieveColumnAsInt32(sesid, tableid, columnidId);
        //    serializedBody = Api.RetrieveColumnAsString(sesid, tableid, columnidMessage);
        //    serializedMetadata = Api.RetrieveColumnAsString(sesid, tableid, columnidMetaData);

        //    var metadata = Serializer.DeserializeMessagePacket<T>(serializedBody, serializedMetadata);
        //    metadata.MessageId = messageId;
        //    return metadata;
        //}

        //private static object Deserialize(string input)
        //{
        //    return Serializer.Deserializer<T>(input);
        //}

        /// <summary>
        /// Cleanups the name.
        /// </summary>
        /// <param name="dirtyname">The dirtyname.</param>
        /// <returns>Clean name</returns>
        private static string CleanupName(string dirtyname)
        {
            return dirtyname.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("_", string.Empty).Replace(".", string.Empty);
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="messageId">The message id.</param>
        /// <param name="table">The table.</param>
        /// <returns>List of subscriber metadatas</returns>
        //private List<ISubscriberMetadata> GetMetadata(int messageId, Table table)
        //{
        //    IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(this.currentSession, table);

        //    JET_COLUMNID columnidId = columnids["id"];
        //    JET_COLUMNID columnidName = columnids["name"];
        //    JET_COLUMNID columnidMessageid = columnids["messageid"];
        //    JET_COLUMNID columnidTimetoexpire = columnids["timetoexpire"];
        //    JET_COLUMNID columnidCompleted = columnids["completed"];
        //    JET_COLUMNID columnidFailedortimedout = columnids["failedortimedout"];
        //    JET_COLUMNID columnidStarttime = columnids["starttime"];
        //    JET_COLUMNID columnidFailedortimeouttime = columnids["failedortimeouttime"];
        //    JET_COLUMNID columnidRetrycount = columnids["retrycount"];

        //    List<ISubscriberMetadata> metadatas = new List<ISubscriberMetadata>();

        //    if (SeekToId(this.currentSession, table, messageId, "messageid"))
        //    {
        //        do
        //        {
        //            metadatas.Add(new SubscriberMetadata()

        //            {
        //                Id = Api.RetrieveColumnAsInt32(this.currentSession, table, columnidId).ToString(),
        //                Name = Api.RetrieveColumnAsString(this.currentSession, table, columnidName),
        //                Completed = (bool)Api.RetrieveColumnAsBoolean(this.currentSession, table, columnidCompleted),
        //                FailedOrTimedOut = (bool)Api.RetrieveColumnAsBoolean(this.currentSession, table, columnidFailedortimedout),
        //                FailedOrTimedOutTime = (DateTime)Api.RetrieveColumnAsDateTime(this.currentSession, table, columnidFailedortimeouttime),
        //                RetryCount = (int)Api.RetrieveColumnAsInt16(this.currentSession, table, columnidRetrycount),
        //                StartTime = (DateTime)Api.RetrieveColumnAsDateTime(this.currentSession, table, columnidStarttime),
        //                TimeToExpire = new TimeSpan(0, 0, 0, 0, (int)Api.RetrieveColumnAsInt32(this.currentSession, table, columnidTimetoexpire))
        //            });
        //        }                
        //        while (this.RelatedRecord(table, columnidMessageid, messageId));
        //    }

        //    return metadatas;
        //}

        /// <summary>
        /// Moves to next record and determies if they are related. (MessageId is the same)
        /// </summary>
        /// <param name="tableid">The tableid.</param>
        /// <param name="messageColumnId">The message column id.</param>
        /// <param name="messageId">The message id.</param>
        /// <returns>true if next record is related to previous, false if they are not related (by MessageId)</returns>
        private bool RelatedRecord(JET_TABLEID tableid, JET_COLUMNID messageColumnId, int messageId)
        {
            if (!Api.TryMoveNext(this.currentSession, tableid))
            {
                return false;
            }

            return Api.RetrieveColumnAsInt32(this.currentSession, tableid, messageColumnId) == messageId;
        }

        /// <summary>
        /// Gets all records.
        /// </summary>
        /// <param name="sesid">The sesid.</param>
        /// <param name="tableid">The tableid.</param>
        /// <param name="columnids">The columnids.</param>
        /// <returns>INumerable of Message packets all records in database</returns>
        //private IEnumerable<MessagePacket<T>> GetAllRecords(JET_SESID sesid, JET_TABLEID tableid, IDictionary<string, JET_COLUMNID> columnids)
        //{
        //    List<MessagePacket<T>> results = new List<MessagePacket<T>>();
        //    if (Api.TryMoveFirst(sesid, tableid))
        //    {
        //        using (var table = new Table(this.currentSession, this.dbid, SubscriberMetadataTableName, OpenTableGrbit.None))
        //        {
        //            do
        //            {
        //                var row = GetOneRow(sesid, tableid, columnids);
        //                if (row != null)
        //                {
        //                    row.ReplaceMetadatas(this.GetMetadata((int)row.MessageId, table));
        //                    results.Add(row);
        //                }
        //            }
        //            while (Api.TryMoveNext(sesid, tableid));
        //        }
        //    }

        //    return results;
        //}
    }
}
