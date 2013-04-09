namespace Daytona.Store
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;
    using Daytona.Store;

    public class EsentStoreResourceManager<T> : IEnlistmentNotification
    {
        //IEsentStore<T> store = default(EsentStore<T>);
        public IEsentStore<T> Store { get; set; }
        public Enlistment Enlistment { get; set; }

        public EsentStoreResourceManager(IEsentStore<T> store)
        {
            if (Transaction.Current != null)
            {
                Debug.Assert(Transaction.Current.TransactionInformation.Status == TransactionStatus.Active);
                this.Enlistment = Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
            }

            this.Store = store;
        }

        public void Commit(Enlistment enlistment)
        {
            Store.Commit();
            Store.Dispose();
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            enlistment.Done();
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            //this is where I shuold do stuff
            preparingEnlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            Store.Rollback();
            Store.Dispose();
            enlistment.Done();
        }
    }
}
