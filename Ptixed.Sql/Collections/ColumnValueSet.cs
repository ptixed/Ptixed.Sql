﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;

namespace Ptixed.Sql.Collections
{
    public class ColumnValueSet : IEnumerable<ColumnValue>
    {
        private readonly Range<ColumnValue> _values;
        private readonly Lazy<Dictionary<string, object>> _dict;

        public object this[string key]
        {
            get
            {
                if (_dict.Value.TryGetValue(key, out object value))
                    return value;
                throw PtixedException.ColumnNotFound(key);
            }
        }
        public object this[int key] => _values[key];
        public int Count => _values.Length;

        internal ColumnValueSet(Range<ColumnValue> values)
        {
            _values = values;
            _dict = new Lazy<Dictionary<string, object>>(() => _values.ToDictionary(x => x.Name, x => x.Value), LazyThreadSafetyMode.None);
        }

        internal ColumnValueSet(SqlDataReader reader)
        {
            var values = new ColumnValue[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; ++i)
                values[i] = new ColumnValue(reader.GetName(i), reader.GetValue(i));

            _values = new Range<ColumnValue>(values);
            _dict = new Lazy<Dictionary<string, object>>(() => _values.ToDictionary(x => x.Name, x => x.Value), LazyThreadSafetyMode.None);
        }

        public ColumnValueSet GetRange(int index, int count)
        {
            return new ColumnValueSet(_values.GetRange(index, count));
        }

        public IEnumerator<ColumnValue> GetEnumerator() => _values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Dictionary<string, object> ToDictionary() => _dict.Value;
    }
}
