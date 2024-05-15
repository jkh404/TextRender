using System;
using System.Runtime.InteropServices;
using System.Threading;
namespace TextRender
{
    public class PinGCTextBuffer : IDisposable
    {
        private object _lockTextBuffer;
        private IntPtr _textBuffer = IntPtr.Zero;
        private int _textLength = 0;
        private GCHandle _textBufferGCHandle;
        public PinGCTextBuffer(object lockTextBuffer,ReadOnlySpan<char> text)
        {
            if(!Monitor.TryEnter(lockTextBuffer))throw new InvalidOperationException("请先释放文本数据");
            _lockTextBuffer = lockTextBuffer;
            _textLength = text.Length;
            var _data=new char[_textLength];
            text.CopyTo(_data);
            _textBufferGCHandle=GCHandle.Alloc(_data, GCHandleType.Pinned);
            _textBuffer=_textBufferGCHandle.AddrOfPinnedObject();
        }
        public unsafe IntPtr Ptr => _textBuffer;
        public unsafe int BufferLength => _textLength;
        public unsafe ReadOnlySpan<char> Texts => Data;
        internal unsafe Span<char> Data => new Span<char>((void*)_textBuffer, _textLength);
        public void Dispose()
        {
            _textBufferGCHandle.Free();
            _textBuffer=IntPtr.Zero;
            Monitor.Exit(_lockTextBuffer);
            GC.Collect();
        }
    }
}
