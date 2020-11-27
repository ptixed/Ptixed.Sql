using System;
using System.Collections;
using System.Collections.Generic;

namespace Ptixed.Sql.Collections
{
    internal class Range<T> : IEnumerable<T>
    {
        public readonly int Length;

        private readonly int _offset;
        private readonly IList<T> _array;

        public T this[int index]
        {
            get
            {
                if (index >= Length || index < 0)
                    throw new ArgumentOutOfRangeException();
                return _array[_offset + index];
            }
            set
            {
                if (index >= Length || index < 0)
                    throw new ArgumentOutOfRangeException();
                _array[_offset + index] = value;
            }
        }

        public Range(IList<T> array)
        {
            _array = array;
            _offset = 0;
            Length = array.Count;
        }

        public Range(IList<T> array, int offset, int length)
        {
            _array = array;
            _offset = offset;
            Length = length;
        }

        public Range<T> GetRange(int offset, int length)
        {
            if (offset + length > Length)
                throw new ArgumentOutOfRangeException();
            return new Range<T>(_array, _offset + offset, length);
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        private class Enumerator : IEnumerator<T>
        {
            private readonly Range<T> _range;
            private int _index = -1;

            public Enumerator(Range<T> range) => _range = range;
            
            public void Dispose() { }
            public bool MoveNext() => ++_index < _range.Length;
            public void Reset() => _index = -1;

            public T Current => _range._array[_range._offset + _index];
            object IEnumerator.Current => Current;
        }
    }
}
