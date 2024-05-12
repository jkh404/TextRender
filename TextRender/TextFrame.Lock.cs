using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace TextRender
{
    /// <summary>
    /// 文本渲染片段
    /// </summary>
    public partial class TextFrame : IDisposable
    {
        public ReadDataLock CreateReadDataLock()
        {
            return new ReadDataLock(this);
        }
        public struct ReadDataLock : IDisposable
        {
            private bool _disposed;
            private TextFrame? _textFrame;
            public ReadOnlySpan<byte> Bytes
            {
                get
                {
                    if(_textFrame==null || _disposed) throw new InvalidOperationException();
                    else return _textFrame.Bytes;
                }
            }

            public ReadDataLock(TextFrame textFrame)
            {
                if (!Monitor.TryEnter(textFrame._lockAlloc, 2000)) throw new InvalidOperationException();
                _disposed=false;
                _textFrame = textFrame;
                
            }

            public void UnLock()
            {
                if (_textFrame!=null)
                {
                    var _lockObj = _textFrame._lockAlloc;
                    _textFrame=null;
                    Monitor.Exit(_lockObj);
                }
            }
            public void Dispose()
            {
                _disposed=true;
                UnLock();
            }
        }
    }
}
