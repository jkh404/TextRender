using System;
using System.IO;

namespace TextRender.Handles
{
    internal class StreamWapper: IDisposable
    {
        private readonly Stream _stream;
        private object _lockReadWrite = new object();
        public StreamWapper(Stream stream)
        {
            _stream =Stream.Synchronized(stream);
            _stream.Seek(0,SeekOrigin.Begin);
        }
        public long Length=>_stream.Length;
        public long Seek(long offset,SeekOrigin seekOrigin)
        {
            lock (_lockReadWrite)
            {
                return _stream.Seek(offset, seekOrigin);
            }
        }
        public int Read(byte[] buffer, long start,int length)
        {
            if(buffer==null)throw new ArgumentNullException(nameof(buffer));
            if(length>buffer.Length)throw new ArgumentOutOfRangeException(nameof(length));
            var maxLength = int.MaxValue-10240;
            length=(int)Math.Min((_stream.Length-start), buffer.Length);
            lock (_lockReadWrite)
            {
                _stream.Seek(start, SeekOrigin.Begin);
                _stream.Read(buffer, 0, length);
                _stream.Seek(0, SeekOrigin.Begin);
                return length;
            }
        }
        public long Write(byte[] buffer, long start, int length)
        {
            if (buffer==null) throw new ArgumentNullException(nameof(buffer));
            if (length>buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
            var maxLength = int.MaxValue-10240;
            length=(int)Math.Min((_stream.Length-start), buffer.Length);
            lock (_lockReadWrite)
            {
                _stream.Seek(start, SeekOrigin.Begin);
                _stream.Write(buffer, 0, length);
                _stream.Seek(0, SeekOrigin.Begin);
                return length;
            }
        }
        public int Read(Span<byte> buffer)
        {
            if (buffer==null) throw new ArgumentNullException(nameof(buffer));
            lock (_lockReadWrite)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                var count=_stream.Read(buffer);
                _stream.Seek(0, SeekOrigin.Begin);
                return count;
            }
        }
        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer==null) throw new ArgumentNullException(nameof(buffer));
            lock (_lockReadWrite)
            {
                _stream.Seek(0, SeekOrigin.Begin);
                _stream.Write(buffer);
                _stream.Seek(0, SeekOrigin.Begin);
            }
        }
        public void Dispose()
        {
            _stream?.Dispose();
        }
    }
}
