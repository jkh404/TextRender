using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using TextRender.Abstracts;
using TextRender.Command;
namespace TextRender
{
    /// <summary>
    /// 文本渲染片段
    /// </summary>
    public  partial class TextFrame : IDisposable
    {

        public TextFrame(IGraphic graphic, int? width = null, int? height = null)
        {
            if (graphic==null) throw new ArgumentNullException(nameof(graphic));
            if (width<0 || height<0) throw new ArgumentException("width and height must be greater than 0");
            _graphic=graphic;
            _graphic.Resize(width??0, height??0);
        }


        public delegate void RenderFinished(ReadOnlySpan<byte> data);

        private object _lockRender = new object();
        private object _lockAlloc = new object();
        private object _lockSetText = new object();


        private RenderFinished? _onRenderFinished;
        private Margin _lineMargin=new Margin();
        private Margin _pageMargin = new Margin();
        private string _text=string.Empty;
        private ConcurrentStack<int> _lineLenStack = new ConcurrentStack<int>();
        private static readonly int NewLineLen = Environment.NewLine.Length;
        private float _charLeftOffset;
        //下次渲染，立即进行文本重排版
        private bool _nextFixupText = false;
        private TextRange _surplusRange;
        private List<(Range Range, byte[] IsFatCharArray)> _lines;
        private uint _backgroundColor = Colors.Transparent;


        private bool NextFixupText
        {
            set { _nextFixupText = value || _nextFixupText; }
            get { return _nextFixupText; }
        }
        


        //队列私有字段
        protected  ConcurrentQueue<Action<TextFrame>> _actionQueue = new ConcurrentQueue<Action<TextFrame>>();
        protected TextRange _textDisplayRange;
        protected IGraphic _graphic;

        protected unsafe ReadOnlySpan<byte> ReadOnlyBytes
        {
            get
            {
                return Bytes;
            }
        }
        protected unsafe Span<byte> Bytes => _graphic.Bytes;
        protected IEnumerable<string> Lines => _lines.Select(r => _text[r.Range]);


        /// <summary>
        /// 多余的文本
        /// </summary>
        public ReadOnlySpan<char> SurplusText => _text[_surplusRange.AsRange()];
        public int BytesSize => _graphic.BytesSize;
        public virtual int Width
        {
            get => _graphic.Width;
        }
        public virtual int Height
        {
            get => _graphic.Height;
        }
        public RenderFinished OnRenderFinished
        {
            set
            {
                if(value==null)throw new ArgumentNullException("OnRenderFinished");    
                _onRenderFinished=value;
            }
        }


        //public float CharWidth => MathF.Max(FontSize/2.0F+_charLeftOffset,1);
        //public float DCharWidth => MathF.Max(FontSize+_charLeftOffset,1);

        //public float LineHeight => MathF.Max(_lineMargin.Top+_lineMargin.Bottom+FontSize, 1);

        //public int LineCount => Math.Max(Convert.ToInt32(MathF.Floor(ContentHeight/LineHeight)), 0);
        //public int LineCharCount => Math.Max(Convert.ToInt32(MathF.Floor(ContentWidth/CharWidth)), 0);

        public float ContentHeight => MathF.Max(Height-_pageMargin.Top-_pageMargin.Bottom,0);
        public float ContentWidth => MathF.Max(Width-_pageMargin.Left-_pageMargin.Right-_lineMargin.Left-_lineMargin.Right,0);


        

        public string CurrentFontkey
        {
            get => _graphic.CurrentFontKey;
            set
            {
                _graphic.CurrentFontKey=value;
                NextFixupText =true;
            }
        }
        public float PageMarginTop
        {
            get => _pageMargin.Top;
            set { _pageMargin.Top = value; NextFixupText=true; }
        }
        public float PageMarginBottom
        {
            get => _pageMargin.Bottom;
            set { _pageMargin.Bottom = value; NextFixupText=true; }
        }
        public float PageMarginLeft
        {
            get => _pageMargin.Left;
            set { _pageMargin.Left = value; NextFixupText=true; }
        }
        public float PageMarginRight
        {
            get => _pageMargin.Right;
            set
            {
                _pageMargin.Right = value;
                NextFixupText=true;
            }
        }

        public float LineMarginLeft
        {
            get => _lineMargin.Left;
            set
            {
                _lineMargin.Left = value;
                NextFixupText=true;
            }
        }
        public float LineMarginRight
        {
            get => _lineMargin.Right;
            set
            {
                _lineMargin.Right = value;
                NextFixupText=true;
            }
        }
        public float LineMarginTop
        {
            get => _lineMargin.Top;
            set
            {
                _lineMargin.Top = value;
                NextFixupText=true;
            }
        }
        public float LineMarginBottom
        {
            get => _lineMargin.Bottom;
            set
            {
                _lineMargin.Bottom = value;
                NextFixupText=true;
            }
        }
        public float CharLeftOffset
        {
            get => _charLeftOffset;
            set
            {
                _charLeftOffset = value;
                NextFixupText=true;
            }
        }

        //protected TextFrame(IGraphic graphic)
        //{
        //    if (graphic==null) throw new ArgumentNullException(nameof(graphic));
        //    _graphic=graphic;


        //    //_imageInfo = sKImageInfo;
        //    //_defaultFont= new SKFont(SKTypeface.FromFamilyName("宋体"));
        //    //_defaultPaint=new SKPaint(_defaultFont) {
        //    //    Style = SKPaintStyle.Fill,
        //    //    IsAntialias = true,
        //    //    Color = SKColors.Black,
        //    //    TextAlign = SKTextAlign.Left,
        //    //};
        //    //Encoding = SKTextEncoding.Utf16;
        //    //_dataPtr=IntPtr.Zero;
        //    //_lines=new List<(Range range, byte[] IsFatCharArray)>(1000);
        //    //Alloc();
        //}
        public void Alloc()
        {
            Monitor.Enter(_lockAlloc);
            _graphic.Alloc();
            Monitor.Exit(_lockAlloc);

        }
        public void Free()
        {
            Monitor.Enter(_lockAlloc);
            _graphic.Free();
            Monitor.Exit(_lockAlloc);
        }
        public void Resize(int w,int h)
        {
            if(_graphic.Resize(w, h)) NextFixupText =true;

        }
        //public void InitFont(float fontSize,string hexColor, Action<IFontAdd>? action=null)
        //{
        //    _defaultPaint.Color=SKColor.Parse(hexColor);
        //    _defaultFont.Size=fontSize;
        //    _defaultPaint.TextSize=fontSize;
        //    _defaultPaint.TextEncoding=Encoding;
        //    FontDictionary?.Dispose();
        //    FontDictionary =new DefaultFontDictionary(false, fontSize);
        //    action?.Invoke(FontDictionary);
        //}
        //protected virtual SKFont? GetFont(int? FontIndex)
        //{
        //    return FontDictionary.GetFont(FontIndex);
        //}
        //protected virtual SKPaint? GetPaint(int? FontIndex)
        //{
        //    return FontDictionary.GetPaint(FontIndex);
        //}
        private int _SetText(ReadOnlySpan<char> text, out TextRange surplusRange)
        {
            lock (_lockSetText)
            {
                surplusRange=new Range(0, 0);

                if (_lines!=null)
                {
                    foreach (var item in _lines)
                    {
                        ArrayPool<byte>.Shared.Return(item.IsFatCharArray, true);
                    }
                    _lines.Clear();
                }
                else return 0;
                if (text==null || text.Length<=0) return 0;
                if (_lines.Count>=LineCount) return 0;
                int addCharCount = 0;
                Range _tempRange = 0..text.Length;

                var _start = _textDisplayRange.Start;
                for (int i = _lines.Count; i < LineCount; i++)
                {
                    var lastLineEndIndex = addCharCount;
                    addCharCount+=AddLineText(i, text[addCharCount..text.Length], out _tempRange, out var tempFatCharArray);
                    _lines.Add(((_start+lastLineEndIndex)..(_start+addCharCount), tempFatCharArray));
                    if ((_tempRange.End.Value-_tempRange.Start.Value)<=0)
                    {
                        break;
                    }

                }
                _surplusRange=0..0;
                return addCharCount;
            }
            

        }
        public int InitText(string text)
        {
            _text=text;
            _textDisplayRange=new Range(0, _text.Length);
            var result=FixupText();
            _textDisplayRange.SetStartAndEnd(_textDisplayRange.Start, result);
            return result;
        }
        public bool MoveDisplayStart(int num)
        {
            var _start=_textDisplayRange.Start;
            var _end = _textDisplayRange.End;
            if (_start+num>=0 && _end+num<_text.Length)
            {
                _start+=num;
                _end=_text.Length;
                _textDisplayRange.SetStartAndEnd(_start, _end);
                var result = FixupText();
                _end=_start+result;
                _textDisplayRange.SetStartAndEnd(_start, _end);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool MoveDisplayLine(int num)
        {
            if (this._lines.Count>0 && num>0)
            {
                var len = _lines[0].Range.End.Value-_lines[0].Range.Start.Value;
                if (MoveDisplayStart(len))
                {
                    _lineLenStack.Push(len);
                    return true;
                }
                
            }
            else if (num<0 && _lineLenStack.Count>0)
            {
                if(_lineLenStack.TryPeek(out var len) && MoveDisplayStart(-len))
                {
                    int i = 0;
                    while (!_lineLenStack.TryPop(out var _))
                    {
                        if(i>100)throw new TimeoutException();
                        Thread.Sleep(100);
                        i++;
                    }
                    return true;
                }
            }
            return false;
        }
        public void UpdateDisplayRange(int _start,int _end)
        {
            if (_start>=0 && _end<=_text.Length)
            {
                _textDisplayRange.SetStartAndEnd(_start, _end);
                var result = FixupText();
                _end=_start+result;
                _textDisplayRange.SetStartAndEnd(_start, _end);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        /// <summary>
        /// 重新排版
        /// </summary>
        protected int FixupText()
        {
            return _SetText(_text.AsSpan()[_textDisplayRange.AsRange()], out _surplusRange);
        }
        private int AddLineText(int lineIndex,ReadOnlySpan<char> text,out Range surplusRange,out byte[] isFatCharArray)
        {

            surplusRange =new Range(0,0);
            isFatCharArray=null;
            if (text.Length<=0) return 0;
            ReadOnlySpan<char> _temp;
            var endIndex = text.Length-1;
            var newLineIndex = text.IndexOf(Environment.NewLine);
            if (newLineIndex>=0) endIndex=newLineIndex+NewLineLen;
            var contentWidth = ContentWidth;
            var startIndex = 0;
            isFatCharArray=ArrayPool<byte>.Shared.Rent(endIndex);
            for (startIndex = 0; startIndex <endIndex; startIndex++)
            {
                _temp=text[startIndex..(startIndex+1)];
                var width = _graphic.MeasureText(_temp);
                if (width/(FontSize/2)>1.0F)
                {
                    //宽字符
                    if (contentWidth>=DCharWidth)
                    {
                        isFatCharArray[startIndex]=2;
                        contentWidth-=width;
                    }
                    else
                    {
                        startIndex=Math.Max(0, startIndex-1);
                        break;
                    }

                }
                else if (width>0)
                {
                    //窄字符
                    if (contentWidth>=CharWidth)
                    {
                        isFatCharArray[startIndex]=(1);
                        contentWidth-=width;
                    }
                    else
                    {
                        startIndex=Math.Max(0, startIndex-1);
                        break;
                    }
                }
                else
                {
                    isFatCharArray[startIndex]=(0);
                }
                
                if ((startIndex >=endIndex || contentWidth<CharWidth)) break;
            }
            endIndex=startIndex;
            surplusRange =new Range(endIndex,text.Length);
            return endIndex;
        }
        private readonly Task[] _invokeTask=new Task[100];
        private SKTextBlob _currentSKTextBlob;
        public unsafe  void Render(string fontKey = null)
        {
            lock (_lockRender)
            {

                if (_actionQueue!=null && _actionQueue.Count>0)
                {

                    var queueCount = _actionQueue.Count;
                    var len = Math.Min(_invokeTask.Length, queueCount);
                    do
                    {
                        for (int tIndex = 0; tIndex < len; tIndex++)
                        {
                            if (_actionQueue.TryDequeue(out var action))
                            {
                                if (action!=null) _invokeTask[tIndex]=Task.Run(() =>
                                {
                                    action?.Invoke(this);
                                });
                                else _invokeTask[tIndex]=Task.CompletedTask;
                            }
                            else
                            {
                                _invokeTask[tIndex]=Task.CompletedTask;
                            }
                        }

                        for (int tIndex = 0; tIndex < len; tIndex++)
                        {
                            _invokeTask[tIndex].Wait();
                        }
                        len=queueCount-len;
                    } while (len>0);
                }

                if (NextFixupText)
                {
                    FixupText();
                    _nextFixupText = false;
                }

                Monitor.Enter(_lockAlloc);
                if (_graphic==null) return;
                _graphic?.Clear(_backgroundColor);
                //_canvas.DrawRoundRectDifference(new SKRoundRect(new SKRect(1, 1, Width-1, Height-1), 1),
                //    new SKRoundRect(new SKRect(2, 2, Width-2, Height-2), 1), paint);
                for (int i = 0; i < LineCount && i<_lines.Count; i++)
                {
                    var x = PageMarginLeft;
                    var y = (i+1)*LineHeight+PageMarginTop;
                    var item = _text.AsSpan()[_lines[i].Range];
                    for (int j = 0; j < item.Length; j++)
                    {
                        //_graphic?.DrawText_NoGC(_textBlobBuilder.Handle,item[j..(j+1)], x, y, paint, font);
                        _graphic?.DrawText(item[j..(j+1)],x,y, fontKey);
                        var multiple = _lines[i].IsFatCharArray[j];
                        if (multiple>0) x+=(MathF.Max(multiple*FontSize/2+_charLeftOffset, 1));
                    }
                }
                _onRenderFinished?.Invoke(ReadOnlyBytes);
                Monitor.Exit(_lockAlloc);
            }
        }

        public void Invoke(Action<TextFrame> action)
        {

            if (action!=null)
            {
                const int MAX_INVOKE_TRY = 10;
                int i = 0;
                while (_actionQueue.Count>100)
                {
                    if (i>MAX_INVOKE_TRY) return;
                    Thread.Sleep(100);
                    i++;
                }
                _actionQueue.Enqueue(action);
            }
        }
        public bool CopyTo(Span<byte> target,out Range range)
        {
            range=new Range(0,0);
            if (target==null || target.Length<=0) return true;
            Monitor.Enter(_lockAlloc);
            var size = Math.Min(target.Length, BytesSize);
            range=new Range(0, size);
            Bytes[range].CopyTo(target);
            Monitor.Exit(_lockAlloc);
            return true;    
        }
        public void Dispose()
        {
            _graphic?.Dispose();

        }

        
    }
}
