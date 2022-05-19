using System;
using System.Collections.Generic;

namespace SimulateCollision
{
    public class PriorityQueue<T>
    {
        public IComparer<T> Comparer { get; private set; }
        public int Count { get { return _count; } }

        private T[] _array = new T[1];
        private int _count = 0;

        private void Add(T item)
        {
            if (_count == _array.Length)
                Array.Resize(ref _array, _count * 2);

            _array[_count] = item;
            _count++;
        }

        private void RemoveAt(int index)
        {
            _count--;
            if (index != _count)
            {
                Array.Copy(_array, index + 1, _array, index, _count - index);
            }
        }

        public PriorityQueue(IComparer<T> comparer)
        {
            this.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        private void Exchange(int i, int j)
        {
            T t = _array[i];
            _array[i] = _array[j];
            _array[j] = t;
        }

        //private bool lessThan(int i, int j)
        //{
        //    return this.Comparer.Compare(list[i], list[j]) < 0;
        //}

        private bool greaterThan(int i, int j)
        {
            return this.Comparer.Compare(_array[i], _array[j]) > 0;
        }

        private void shifDown(int k)
        {
            int n = _count;

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
            Add(v);
            shifUp(_count - 1);
        }

        private T removeTop()
        {
            int n = _count - 1;

            T max = _array[0];
            Exchange(0, n);
            RemoveAt(n);
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
