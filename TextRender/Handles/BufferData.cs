using System;
using System.Buffers;

namespace TextRender.Handles
{
    public class BufferData<T>:IDisposable where T:struct
    {
        private T[] _buffer;
        private int _capacity;
        public BufferData(int capacity)
        {
            _capacity=capacity;
            _buffer = ArrayPool<T>.Shared.Rent(_capacity);
        }
        public Span<T> Data => _buffer.AsSpan(0, _capacity);
        public void Dispose()
        {
            if (_buffer!=null) ArrayPool<T>.Shared.Return(_buffer);
        }
    }
}
