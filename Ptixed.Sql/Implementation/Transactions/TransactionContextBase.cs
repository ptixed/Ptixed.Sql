using System.Data.SqlClient;

namespace Ptixed.Sql.Implementation.Transactions
{
    internal class TransactionContextBase
    {
        public SqlTransaction SqlTransaction { get; }

        public bool? IsCommited { get; private set; }
        public bool IsDisposed { get; private set; }

        public TransactionContextBase(SqlTransaction transaction)
        {
            SqlTransaction = transaction;
        }

        public virtual void Commit()
        {
            if (IsCommited.HasValue)
                throw PtixedException.InvalidTransacionState(IsCommited.Value ? "committed" : "rolledback");

            SqlTransaction.Commit();
            IsCommited = true;
        }

        public virtual void Dispose()
        {
            if (!IsCommited.HasValue)
            {
                SqlTransaction.Rollback();
                IsCommited = false;
            }
            if (!IsDisposed)
            {
                IsDisposed = true;
                SqlTransaction.Dispose();
            }
        }
    }
}
