//-----------------------------------------------------------------------
// <copyright file="EsentConfig.cs" company="The Phantom Coder">
//     Copyright The Phantom Coder. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Isam.Esent.Interop;
    /// <summary>
    /// Creates the Esent Database if none exists
    /// </summary>
    public static class EsentConfig
    {
        public static string DatabaseName
        {
            get
            {
                return "Daytona.Store.edb";
            }
        }

        /// <summary>
        /// Doeses the database exist.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <returns>True if database exists</returns>
        public static bool DoesDatabaseExist(string databaseName)
        {
            bool result = File.Exists(databaseName);

            return result;
        }

        /// <summary>
        /// Creates the Actor Database.
        /// </summary>
        /// <param name="database">The database.</param>
        public static void CreateDatabase()
        {
            using (var instance = new Instance("createdatabase"))
            {
                instance.Parameters.CircularLog = true;
                instance.Init();
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetCreateDatabase(session, DatabaseName, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                }
            }
        }

        /// <summary>
        /// Creates the actor store.
        /// </summary>
        /// <param name="actorTypeName"></param>
        public static void CreateDatabaseAndActorStore(string actorTypeName)
        {
            using (var instance = new Instance("createdatabase"))
            {
                instance.Parameters.CircularLog = true;
                instance.Init();
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetCreateDatabase(session, DatabaseName, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                    CreateActorTable(actorTypeName, session, dbid);
                }
            }
        }

        /// <summary>
        /// Creates the database and a single table that stores all actors.
        /// </summary>
        /// <param name="actorTypeName"></param>
        public static void CreateDatabaseAndSingleActorStore()
        {
            using (var instance = new Instance("createdatabase"))
            {
                instance.Parameters.CircularLog = true;
                instance.Init();
                using (var session = new Session(instance))
                {
                    JET_DBID dbid;
                    Api.JetCreateDatabase(session, DatabaseName, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                    CreateActorTable("actortable", session, dbid);
                }
            }
        }

        public static void CreateActorStore(string actorTypeName, Instance instance)
        {
            using (var session = new Session(instance))
            {
                JET_DBID dbid;
                Api.JetOpenDatabase(session, DatabaseName, null, out dbid, OpenDatabaseGrbit.None);
                CreateActorTable(actorTypeName, session, dbid);
            }
        }

        //private static void CreateSubscriberMetadataTable(string tableName, Session session, JET_DBID dbid)
        //{
        //    tableName = tableName + "subscribermetadata";

        //    using (var transaction = new Transaction(session))
        //    {
        //        JET_TABLEID tableid;
        //        Api.JetCreateTable(session, dbid, tableName, 16, 100, out tableid);
        //        CreateColumnsAndIndexesForSubscriberMetadata(session, tableid);
        //        Api.JetCloseTable(session, tableid);
        //        transaction.Commit(CommitTransactionGrbit.LazyFlush);
        //    }
        //}

        private static void CreateActorTable(string tableName, Session session, JET_DBID dbid)
        {
            using (var transaction = new Transaction(session))
            {
                JET_TABLEID tableid;
                Api.JetCreateTable(session, dbid, tableName, 16, 100, out tableid);
                CreateColumnsAndIndexes(session, tableid);
                Api.JetCloseTable(session, tableid);
                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }

        /// <summary>
        /// Create the required columns and indexes for the message store. 
        /// Creates 3 columns, an ID column, a Message colum for the data and a metadata colum that stores information on the message and the subscribers
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">
        /// The table to add the columns/indexes to. This table must be opened exclusively.
        /// </param>
        private static void CreateColumnsAndIndexes(JET_SESID sesid, JET_TABLEID tableid)
        {
            using (var transaction = new Transaction(sesid))
            {
                JET_COLUMNID columnid;

                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongBinary
                };

                Api.JetAddColumn(sesid, tableid, "payload", columndef, null, 0, out columnid);

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongText,
                    cp = JET_CP.Unicode
                };

                Api.JetAddColumn(sesid, tableid, "textpayload", columndef, null, 0, out columnid);

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement
                };

                Api.JetAddColumn(sesid, tableid, "id", columndef, null, 0, out columnid);

                const string IndexDef = "+id\0\0";
                Api.JetCreateIndex(sesid, tableid, "primary", CreateIndexGrbit.IndexPrimary, IndexDef, IndexDef.Length, 100);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }

        /// <summary>
        /// Creates the columns and indexes to store the subscriber metadata.
        /// </summary>
        /// <param name="sesid">The sesid.</param>
        /// <param name="tableid">The tableid.</param>
        private static void CreateColumnsAndIndexesForSubscriberMetadata(JET_SESID sesid, JET_TABLEID tableid)
        {
            using (var transaction = new Transaction(sesid))
            {
                JET_COLUMNID columnid;

                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement
                };

                Api.JetAddColumn(sesid, tableid, "id", columndef, null, 0, out columnid);

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long
                };
                
                Api.JetAddColumn(sesid, tableid, "messageid", columndef, null, 0, out columnid);

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongText,
                    cp = JET_CP.Unicode
                };

                Api.JetAddColumn(sesid, tableid, "name", columndef, null, 0, out columnid);
                Api.JetAddColumn(sesid, tableid, "timetoexpire", columndef, null, 0, out columnid); ////in miliseconds

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Bit
                };

                Api.JetAddColumn(sesid, tableid, "completed", columndef, null, 0, out columnid);
                Api.JetAddColumn(sesid, tableid, "failedortimedout", columndef, null, 0, out columnid);

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.DateTime
                };

                Api.JetAddColumn(sesid, tableid, "starttime", columndef, null, 0, out columnid);
                Api.JetAddColumn(sesid, tableid, "failedortimeouttime", columndef, null, 0, out columnid);

                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Short
                };

                Api.JetAddColumn(sesid, tableid, "retrycount", columndef, null, 0, out columnid);

                string indexDef = "+id\0\0";
                Api.JetCreateIndex(sesid, tableid, "primary", CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);

                indexDef = "+messageid\0\0";
                Api.JetCreateIndex(sesid, tableid, "messageid", CreateIndexGrbit.None, indexDef, indexDef.Length, 100);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }

        public static bool DoesStoreExist(string storeName)
        {
            JET_DBID dbid;

            using(var instance =  EsentInstanceService.Service.EsentInstance)
            {
                using(var session = new Session(instance))
                {
                    Api.JetOpenDatabase(session, DatabaseName, null, out dbid, OpenDatabaseGrbit.None);
                    JET_TABLEID tableid;
                    
                    if (Api.TryOpenTable(session, dbid, storeName, OpenTableGrbit.None, out tableid))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        internal static bool DoesStoreExist(string storeName, Instance instance)
        {
            JET_DBID dbid;

            using (var session = new Session(instance))
            {
                Api.JetOpenDatabase(session, DatabaseName, null, out dbid, OpenDatabaseGrbit.None);
                JET_TABLEID tableid;
                if (Api.TryOpenTable(session, dbid, storeName, OpenTableGrbit.None, out tableid))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }            
    }
}
