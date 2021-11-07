using System;
using System.Collections.Generic;

namespace SimulateCollision
{
    public class PriorityQueue<T>
    {
        public IComparer<T> Comparer { get; private set; }
        public int Count { get { return list.Count; } }

        private readonly List<T> list = new();

        public PriorityQueue(IComparer<T> comparer)
        {
            this.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        private void Exchange(int i, int j)
        {
            T t = list[i];
            list[i] = list[j];
            list[j] = t;
        }

        //private bool lessThan(int i, int j)
        //{
        //    return this.Comparer.Compare(list[i], list[j]) < 0;
        //}

        private bool greaterThan(int i, int j)
        {
            return this.Comparer.Compare(list[i], list[j]) > 0;
        }

        private void shifDown(int k)
        {
            int n = list.Count;

            while (2 * (k + 1) - 1 < n)
            {
                int j = 2 * (k + 1) - 1;
                if (j + 1 < n && greaterThan(j, j + 1)) j++;
                if (!greaterThan(k, j)) break;
                Exchange(k, j);
                k = j;
            }
        }

        private void shifUp(int k)
        {
            while (k > 0 && greaterThan((k - 1) / 2, k))
            {
                Exchange((k - 1) / 2, k);
                k = (k - 1) / 2;
            }
        }

        private void Insert(T v)
        {
            list.Add(v);
            shifUp(list.Count - 1);
        }

        private T removeTop()
        {
            int n = list.Count - 1;

            T max = list[0];
            Exchange(0, n);
            list.RemoveAt(n);
            shifDown(0);
            return max;
        }


        public T Dequeue()
        {
            return removeTop();
        }

        public void Enqueue(T item)
        {
            Insert(item);
        }
    }
}
