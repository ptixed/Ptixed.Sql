﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Ptixed.Sql.Collections;
using Ptixed.Sql.Implementation.Accessors;
using Ptixed.Sql.Metadata;

namespace Ptixed.Sql.Implementation
{
    internal class QueryResult<T> : IDisposable
    {
        private readonly MappingConfig _config;
        private readonly SqlDataReader _reader;
        private readonly ITracker _tracker;
        private readonly Type[] _types;

        public QueryResult(MappingConfig config, SqlDataReader reader, ITracker tracker, Type[] types)
        {
            _config = config;
            _reader = reader;
            _tracker = tracker;
            _types = types;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            if (_types.Length > 1)
                return new ModelGraphEnumerator(this);

            var type = _types[0];
            if (type == typeof(IDictionary<string, object>) || type == typeof(Dictionary<string, object>))
                return new DictionaryEnumerator(this);

            var tuple = TupleAccessor.Get(type);
            if (tuple != null)
                return new TupleEnumerator(this, tuple);

            return new ModelEnumerator(this);
        }

        private abstract class EnumeratorBase : IEnumerator<T>
        {
            protected readonly QueryResult<T> Result;

            public T Current { get; set; }
            object IEnumerator.Current => Current;

            public EnumeratorBase(QueryResult<T> result) => Result = result;

            public void Dispose() => Result.Dispose();
            public void Reset() => throw new NotSupportedException();

            public abstract bool MoveNext();
        }

        private class ModelGraphEnumerator : EnumeratorBase
        {
            private readonly Table[] _tables;

            private ColumnValueSet _columns;
            private bool _consumed = true;

            public ModelGraphEnumerator(QueryResult<T> result)
                : base(result)
            {
                _tables = result._types.Select(x => Table.Get(x)).ToArray();
            }

            public override bool MoveNext()
            {
                if (!ReadRow())
                    return false;

                var current = ModelMapper.Map(Result._config, Result._tracker, Result._types, _columns);
                _consumed = true;

                if (_tables[0].PrimaryKey == null)
                {
                    Current = (T)current.Single();
                    return true;
                }

                var currentpk = _tables[0][current[0], _tables[0].PrimaryKey];
                var objs = new List<object[]> { current };

                while (ReadRow())
                {
                    var next = ModelMapper.Map(Result._config, Result._tracker, Result._types, _columns);
                    if (currentpk?.Equals(_tables[0][next[0], _tables[0].PrimaryKey]) != true)
                        break;
                    objs.Add(next);
                    _consumed = true;
                }

                Current = (T)ModelMapper.ConsructObjectGraph(_tables, objs).SingleOrDefault();
                return true;
            }

            private bool ReadRow()
            {
                if (!_consumed)
                    return true;

                if (!Result._reader.Read())
                    return false;

                _columns = new ColumnValueSet(Result._reader);
                _consumed = false;
                return true;
            }
        }

        private class ModelEnumerator : EnumeratorBase
        {
            public ModelEnumerator(QueryResult<T> result)
                : base (result)
            {

            }

            public override bool MoveNext()
            {
                if (!Result._reader.Read())
                    return false;

                Current = (T)ModelMapper.Map(Result._config, Result._tracker, Result._types[0], new ColumnValueSet(Result._reader));

                return true;
            }
        }

        private class DictionaryEnumerator : EnumeratorBase
        {
            public DictionaryEnumerator(QueryResult<T> result)
                : base(result)
            {
            }

            public override bool MoveNext()
            {
                if (!Result._reader.Read())
                    return false;

                Current = (T)(object)new ColumnValueSet(Result._reader).ToDictionary();

                return true;
            }
        }

        private class TupleEnumerator : EnumeratorBase
        {
            private TupleAccessor _tuple;

            public TupleEnumerator(QueryResult<T> result, TupleAccessor tuple)
                : base(result)
            {
                _tuple = tuple;
            }

            public override bool MoveNext()
            {
                if (!Result._reader.Read())
                    return false;

                var objs = ModelMapper.Map(Result._config, Result._tracker, _tuple.Types, new ColumnValueSet(Result._reader));
                Current = (T)_tuple.CreateNew(objs);

                return true;
            }
        }
    }
}
