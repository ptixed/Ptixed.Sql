using System;
using System.Collections;
using System.Collections.Generic;

namespace Ptixed.Sql.Collections
{
    internal class Range2<T> : IEnumerable<Range<T>>
    {
        public readonly int Length1;
        public readonly int Length2;

        private readonly int _offset1;
        private readonly int _offset2;
        private readonly T[][] _array;

        public T this[int index1, int index2]
        {
            get
            {
                if (index1 >= Length1 || index1 < 0)
                    throw new ArgumentOutOfRangeException();
                if (index2 >= Length2 || index2 < 0)
                    throw new ArgumentOutOfRangeException();
                return _array[_offset1 + index1][_offset2 + index2];
            }
            set
            {
                if (index1 >= Length1 || index1 < 0)
                    throw new ArgumentOutOfRangeException();
                if (index2 >= Length2 || index2 < 0)
                    throw new ArgumentOutOfRangeException();
                _array[_offset1 + index1][_offset2 + index2] = value;
            }
        }

        public Range2(T[][] array, int length1, int length2)
        {
            _array = array;
            _offset1 = 0;
            _offset2 = 0;
            Length1 = length1;
            Length2 = length2;
        }

        public Range2(T[][] array, int offset1, int length1, int offset2, int length2)
        {
            _array = array;
            _offset1 = offset1;
            _offset2 = offset2;
            Length1 = length1;
            Length2 = length2;
        }

        public Range2<T> GetRange(int offset1, int length1, int offset2, int length2)
        {
            if (offset1 + length1 > Length1)
                throw new ArgumentOutOfRangeException();
            if (offset2 + length2 > Length2)
                throw new ArgumentOutOfRangeException();
            return new Range2<T>(_array, _offset1 + offset1, length1, _offset2 + offset2, length2);
        }

        public IEnumerator<Range<T>> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class Enumerator : IEnumerator<Range<T>>
        {
            private readonly Range2<T> _range;
            private int _index = -1;

            public Enumerator(Range2<T> range) => _range = range;

            public void Dispose() { }
            public bool MoveNext() => ++_index < _range.Length1;
            public void Reset() => _index = -1;

            public Range<T> Current => new Range<T>(_range._array[_range._offset1 + _index], _range._offset2, _range.Length2);
            object IEnumerator.Current => Current;
        }
    }
}
