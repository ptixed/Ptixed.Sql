﻿using System;
using System.Collections.Generic;
using System.Data;

namespace Ptixed.Sql
{
    public interface IDatabase : IDisposable
    {
        MappingConfig MappingConfig { get; }

        IEnumerable<T> Query<T>(Query query, params Type[] types);
        void NonQuery(params Query[] query);

        IDatabaseTransaction OpenTransaction(IsolationLevel isolation);
    }
}
