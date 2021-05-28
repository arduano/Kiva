using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class SkipIterator<T> : IEnumerable<T>
    {
        class Iterator : IEnumerator<T>
        {
            T[] a;
            int start;
            int skip;

            int current;

            public Iterator(T[] a, int start, int skip)
            {
                this.a = a;
                this.start = start;
                this.skip = skip;
                current = start - skip;
            }

            public T Current => a[current];

            object IEnumerator.Current => a[current];

            public void Dispose()
            {
                a = null;
            }

            public bool MoveNext()
            {
                if ((current += skip) >= a.Length) return false;
                return true;
            }

            public void Reset()
            {
                current = start;
            }
        }

        T[] a;
        int start;
        int skip;
        public SkipIterator(T[] a, int start, int skip)
        {
            this.a = a;
            this.start = start;
            this.skip = skip;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Iterator(a, start, skip);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
