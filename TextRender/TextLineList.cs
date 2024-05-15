using System;
using System.Buffers;
using System.Threading;
namespace TextRender
{
    public class TextLineList:IDisposable
    {
        private  object _lockList = new object();
        private int _capacity = 32;
        private int _count = 0;
        private TextLine[] _textLines;
        private IntPtr _source;
        private int _sourceLength;
        public TextLineList(IntPtr source,int sourceLength)
        {
            _source = source;
            _sourceLength = sourceLength;
            _textLines =ArrayPool<TextLine>.Shared.Rent(_capacity);
        }
        private Span<TextLine> TextLines => _textLines.AsSpan(0, _count);
        public int Count => _textLines==null ? 0 : _count;

        public int Capacity => _textLines==null ? 0 : _capacity;
        public int Height
        {
            get
            {
                var height = 0;
                if(!Monitor.TryEnter(_lockList,1000))throw new InvalidOperationException();
                foreach (var item in TextLines)
                {
                    height+=item.LineHeight;
                }
                Monitor.Exit(_lockList);
                return height;
            }
        }
        public ReadDataLock<TextLine> GetData()
        {
            return ReadDataLock<TextLine>.Create(_textLines, _count, _lockList);
        }
        public int Width
        {
            get
            {
                var maxWidth = 0;
                if(!Monitor.TryEnter(_lockList, 1000))throw new InvalidOperationException();
                foreach (var item in TextLines)
                {
                    var lineW = item.LineWidth;
                    if (lineW>maxWidth) maxWidth=lineW;
                }
                Monitor.Exit(_lockList);
                return maxWidth;
            }
        }

        public void Add(ref TextLine textLine)
        {
            
            Monitor.Enter(_lockList);
            try
            {
                if (_textLines == null) throw new InvalidOperationException();
                if (_capacity==_count)
                {

                    var _tempTextLines = ArrayPool<TextLine>.Shared.Rent(_capacity*=2);
                    _textLines.AsSpan(0, _count).CopyTo(_tempTextLines.AsSpan(0, _count));
                    ArrayPool<TextLine>.Shared.Return(_textLines, true);
                    _textLines=_tempTextLines;
                }
                _textLines[_count++]=textLine;
            }
            finally
            {
                Monitor.Exit(_lockList);
            }
            
        }
        internal ref TextLine GenerateReturn()
        {
            Monitor.Enter(_lockList);
            try
            {
                if (_textLines == null) throw new InvalidOperationException();
                if (_capacity==_count)
                {

                    var _tempTextLines = ArrayPool<TextLine>.Shared.Rent(_capacity*=2);
                    _textLines.AsSpan(0, _count).CopyTo(_tempTextLines.AsSpan(0, _count));
                    ArrayPool<TextLine>.Shared.Return(_textLines, true);
                    _textLines=_tempTextLines;
                }
                ref TextLine TextLine = ref _textLines[_count++];
                TextLine.Init(_source, _sourceLength);
                return ref TextLine;
            }
            finally
            {

                Monitor.Exit(_lockList);
            }
            
        }
        public void Clear()
        {
            Monitor.Enter(_lockList);
            try
            {
                foreach (var item in TextLines)
                {
                    item.Clear();
                }
                _count =0;
            }finally { Monitor.Exit(_lockList); }
            
        }
        private void Release()
        {
            Monitor.Enter(_lockList);
            try
            {
                if (_textLines != null && _textLines.Length>0)
                {
                    foreach (var item in TextLines)
                    {
                        item.Clear();
                    }
                    _count =0;
                    ArrayPool<TextLine>.Shared.Return(_textLines);
                    _textLines=null;
                    _capacity=0;
                }
            }finally { Monitor.Exit(_lockList); }
        }
        public void Dispose()
        {
            Release();
        }
        

    }
}
