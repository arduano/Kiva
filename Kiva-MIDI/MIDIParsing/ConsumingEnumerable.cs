using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class ConsumingEnumerable<T> : IEnumerable<T>
    {
        class Iterator : IEnumerator<T>
        {
            T[] a;

            int current = -1;

            public Iterator(T[] a)
            {
                this.a = a;
            }

            public T Current => a[current];

            object IEnumerator.Current => a[current];

            public void Dispose()
            {
                a = null;
            }

            public bool MoveNext()
            {
                current++;
                if (a.Length <= current) return false;
                if (current > 64)
                {
                    for (int i = current; i < a.Length; i++)
                    {
                        a[i - current] = a[i];
                    }
                    Array.Resize(ref a, a.Length - current);
                    current = 0;
                }
                return true;
            }

            public void Reset()
            {

            }
        }

        T[] a;
        public ConsumingEnumerable(T[] a)
        {
            this.a = a;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Iterator(a);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
