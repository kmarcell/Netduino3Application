using System;
using System.Collections;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class Queue<T> : IEnumerable
    {
        private T[] queue;
        private int count;

        public int Count
        {
            get { return count; }
        }

        public Queue(int capacity = 16)
        {
            queue = new T[capacity];
        }

        public void Enqueue(T element)
        {
            if (count == queue.Length)
            {
                expandQueue();
            }

            queue[count] = element;
            count++;
        }

        public T this[int index]
        {
            get { return queue[index]; }
        }

        public T RemoveAt(int index)
        {
            if (index < 0 || index > queue.Length)
            {
                return default(T);
            }

            T element = queue[index];
            --count;
            Array.Copy(queue, index + 1, queue, index, Count - index);
            queue[Count] = default(T);

            return element;
        }
        public IEnumerator GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        private void expandQueue()
        {
            T[] tmp_queue = new T[queue.Length * 2];
            Array.Copy(queue, tmp_queue, queue.Length);
            queue = tmp_queue;
        }
    }
}
