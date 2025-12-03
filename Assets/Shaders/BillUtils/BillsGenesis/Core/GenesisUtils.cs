using System;
using System.Collections.Generic;

namespace BillsGenesis.Core
{
    public class GenesisPriorityQueue<T> where T : IComparable<T>
    {
        private T[] _data;
        public int Count { get; private set; }
        public int Capacity => _data.Length;

        public GenesisPriorityQueue(int capacity = 1024)
        {
            _data = new T[capacity];
            Count = 0;
        }

        public void Enqueue(T item)
        {
            if (Count == _data.Length) Resize();
            _data[Count] = item;
            HeapifyUp(Count);
            Count++;
        }

        public T Dequeue()
        {
            T firstItem = _data[0];
            Count--;
            _data[0] = _data[Count];
            _data[Count] = default;
            HeapifyDown(0);
            return firstItem;
        }

        public T Peek() => _data[0];

        public void Clear()
        {
            Array.Clear(_data, 0, Count);
            Count = 0;
        }

        private void Resize()
        {
            T[] newArray = new T[_data.Length * 2];
            Array.Copy(_data, newArray, _data.Length);
            _data = newArray;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_data[index].CompareTo(_data[parentIndex]) >= 0) break;
                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                int rightChild = 2 * index + 2;
                int smallest = index;

                if (leftChild < Count && _data[leftChild].CompareTo(_data[smallest]) < 0)
                    smallest = leftChild;
                if (rightChild < Count && _data[rightChild].CompareTo(_data[smallest]) < 0)
                    smallest = rightChild;

                if (smallest == index) break;
                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            T temp = _data[a];
            _data[a] = _data[b];
            _data[b] = temp;
        }
    }
}