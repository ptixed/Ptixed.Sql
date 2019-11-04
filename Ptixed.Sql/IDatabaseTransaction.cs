using System;

namespace Ptixed.Sql
{
    public interface IDatabaseTransaction : IDisposable
    {
        void Commit();
    }
}
