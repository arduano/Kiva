using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kiva_MIDI
{
    class TimedMerger<T> : IEnumerable<T>
        where T : struct
    {
        class Iterator : IEnumerator<T>
        {
            public Iterator(IEnumerable<T> t1, IEnumerable<T> t2, Func<T, double> getTime)
            {
                this.getTime = getTime;
                this.t1 = t1.GetEnumerator();
                this.t2 = t2.GetEnumerator();
                if (!this.t1.MoveNext()) t1Ended = true;
                if (!this.t2.MoveNext()) t2Ended = true;
            }

            public T Current { get; set; }

            T IEnumerator<T>.Current => Current;

            object IEnumerator.Current => Current;

            IEnumerator<T> t1, t2;

            Func<T, double> getTime;

            bool t1Ended = false, t2Ended = false;

            public bool MoveNext()
            {
                if (t1Ended)
                {
                    if (t2Ended) return false;
                    else
                    {
                        Current = t2.Current;
                        if (!t2.MoveNext()) t2Ended = true;
                    }
                }
                else
                {
                    if (t2Ended)
                    {
                        Current = t1.Current;
                        if (!t1.MoveNext()) t1Ended = true;
                    }
                    else
                    {
                        if (getTime(t2.Current) < getTime(t1.Current))
                        {
                            Current = t2.Current;
                            if (!t2.MoveNext()) t2Ended = true;
                        }
                        else
                        {
                            Current = t1.Current;
                            if (!t1.MoveNext()) t1Ended = true;
                        }
                    }
                }
                return true;
            }

            public void Reset()
            {
                t1.Reset();
                t2.Reset();
                t2Ended = false;
                t1Ended = false;
            }

            public void Dispose()
            {
                t1.Dispose();
                t2.Dispose();
            }
        }

        Func<T, double> getTime;
        IEnumerable<T> t1, t2;

        public TimedMerger(IEnumerable<T> t1, IEnumerable<T> t2, Func<T, double> getTime)
        {
            this.t1 = t1;
            this.t2 = t2;
            this.getTime = getTime;
        }

        public static IEnumerable<T> MergeMany(IEnumerable<T>[] t, Func<T, double> getTime)
        {
            List<IEnumerable<T>> t1 = new List<IEnumerable<T>>(t);
            List<IEnumerable<T>> t2 = new List<IEnumerable<T>>();

            while (t1.Count != 1)
            {
                while (t1.Count != 0)
                {
                    if (t1.Count == 1)
                    {
                        t2.Add(t1.First());
                        t1.RemoveAt(0);
                    }
                    else
                    {
                        var a1 = t1.First();
                        t1.RemoveAt(0);
                        var a2 = t1.First();
                        t1.RemoveAt(0);

                        t2.Add(new TimedMerger<T>(a1, a2, getTime));
                    }
                }
                t1 = t2;
                t2 = new List<IEnumerable<T>>();
            }
            return t1[0];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Iterator(t1, t2, getTime);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
