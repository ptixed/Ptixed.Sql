using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Ptixed.Sql.Implementation.Trackers;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Implementation.Transactions
{
    internal class UntrackedTransactionContext : TransactionContextBase, ITransactionContext
    {
        private readonly IDatabaseCore _db;

        public UntrackedTransactionContext(IDatabaseCore db, SqlTransaction transaction) : base(transaction)
        {
            _db = db;
        }

        private IQueryExecutor Executor => new QueryExecutor(_db, new DefaultTracker());

        public List<T> Query<T>(Query query, params Type[] types) => Executor.Query<T>(query, types);
        public IEnumerator<T> LazyQuery<T>(Query query, params Type[] types) => Executor.LazyQuery<T>(query, types);
        public int NonQuery(IEnumerable<Query> queries) => Executor.NonQuery(queries);

        public List<T> GetById<T>(IEnumerable<object> ids) => Executor.GetById<T>(ids);
        public void Insert<T>(IEnumerable<T> entities) => Executor.Insert(entities);
        public void Update(IEnumerable<object> entities) => Executor.Update(entities);
        public void Delete(IEnumerable<(Table table, object id)> deletes) => Executor.Delete(deletes);
    }
}
