using Ptixed.Sql.Impl;
using Ptixed.Sql.Meta;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Ptixed.Sql.SqlServer
{
    public class Database : Database<SqlConnection, SqlCommand, SqlParameter>, IDatabase
    {
        public Database(ConnectionConfig config) : base(config)
        {
        }
        
        public void BulkInsert<T>(IEnumerable<T> entities)
        {
            var table = Table.Get(typeof(T));
            var columns = table.PhysicalColumns.Except(new[] { table.AutoIncrementColumn }).ToList<PhysicalColumn>();

            var payload = new DataTable();

            foreach (var column in columns)
                payload.Columns.Add(column.Name);

            foreach (var entity in entities)
            {
                var row = payload.NewRow();
                var values = table.ToQuery(columns, entity);
                foreach (var (name, value) in values)
                    row[name] = value;
                payload.Rows.Add(row);
            }

            using (var bcp = new SqlBulkCopy(Connection.Value))
            {
                foreach (var column in columns)
                    bcp.ColumnMappings.Add(column.Name, column.Name);

                bcp.DestinationTableName = table.Name;
                bcp.WriteToServer(payload);
            }
        }
    }
}
