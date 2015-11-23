using System;
using System.Collections;
using Microsoft.SPOT;

namespace CoreCommunication
{
    class FrameQueue
    {
        private class FrameWithHandler
        {
            public Frame frame;
            public Callback callback;
        }

        private FrameWithHandler[] queue;
        private int count;

        public int Count
        {
            get { return count; }
        }

        public FrameQueue(int capacity = 16)
        {
            queue = new FrameWithHandler[capacity];
        }

        public void Enqueue(Frame frame, Callback callback)
        {
            if (count == queue.Length)
            {
                expandQueue();
            }

            queue[count] = new FrameWithHandler { frame = frame, callback = callback };
            count++;
        }

        public Frame this[int index]
        {
            get { return queue[index].frame; }
        }

        public Callback CallbackForFrameAtIndex(int index)
        {
            if (index < 0 || index >= Count) { return null; }
            return queue[index].callback;
        }

        public delegate bool Test(Frame frame);
        public int IndexOfFramePassingTest(Test test)
        {
            for (int i = 0; i < Count; ++i)
            {
                if (test(queue[i].frame)) { return i; }
            }

            return -1;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > queue.Length)
            {
                return;
            }

            FrameWithHandler element = queue[index];
            --count;
            Array.Copy(queue, index + 1, queue, index, Count - index);
            queue[Count] = null;
        }

        private void expandQueue()
        {
            FrameWithHandler[] tmp_queue = new FrameWithHandler[queue.Length * 2];
            Array.Copy(queue, tmp_queue, queue.Length);
            queue = tmp_queue;
        }
    }
}
