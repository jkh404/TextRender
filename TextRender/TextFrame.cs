using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using TextRender.Abstracts;
using TextRender.Command;
using TextRender.Handles.Abstracts;
namespace TextRender
{
    /// <summary>
    /// 文本渲染片段
    /// </summary>
    public  partial class TextFrame : IBox,IDisposable
    {

        public TextFrame(IGraphic graphic,ITextProviderHandle textProvider, int? width = null, int? height = null)
        {
            if (graphic==null) throw new ArgumentNullException(nameof(graphic));
            if (width<0 || height<0) throw new ArgumentException("width and height must be greater than 0");
            _graphic=graphic;
            this._textProvider=textProvider;
            _graphic.Resize(width??0, height??0);
        }


        public delegate void RenderFinished(ReadOnlySpan<byte> data);

        private object _lockRender = new object();
        private object _lockAlloc = new object();
        private object _lockSetText = new object();
        private object _lockTextBuffer = new object();
        private bool _initOk;
        private RenderFinished? _onRenderFinished;
        private BoxSpacing _lineMargin=new BoxSpacing();
        private BoxSpacing _pageMargin = new BoxSpacing();
        //private string _text=string.Empty;
        private BoxInfo _box;




        private ConcurrentStack<int> _lineLenStack = new ConcurrentStack<int>();
        private static readonly int NewLineLen = Environment.NewLine.Length;
        private float _charLeftOffset;
        //下次渲染，立即进行文本重排版
        private bool _nextFixupText = false;
        private TextRange _surplusRange;
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
        protected ITextProviderHandle _textProvider;
        protected TextLineList _textLineList;
        
        //protected PinGCTextBuffer _pinGCTextBuffer;
        protected unsafe ReadOnlySpan<byte> ReadOnlyBytes
        {
            get
            {
                return Bytes;
            }
        }
        protected unsafe Span<byte> Bytes => _graphic.Bytes;
        //protected IEnumerable<string> Lines => _lines.Select(r => Text[r.Range]);

        //protected ReadOnlySpan<char> Text=>_pinGCTextBuffer.Data;

        /// <summary>
        /// 多余的文本
        /// </summary>
        //public ReadOnlySpan<char> SurplusText => Text[_surplusRange.AsRange()];
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

        //public int LineCount => _textLineList?.Count??0;
        //public int LineCharCount => Math.Max(Convert.ToInt32(MathF.Floor(ContentWidth/CharWidth)), 0);

        public float ContentHeight => MathF.Max(Height-_pageMargin.Top-_pageMargin.Bottom,0);
        public float ContentWidth => MathF.Max(Width-_pageMargin.Left-_pageMargin.Right-_lineMargin.Left-_lineMargin.Right,0);


        public TextRange DisplayRange=>_textDisplayRange;
        public long TextLength=> _textProvider.Count;

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

        public BoxInfo? ParentBox => null;

        public BoxInfo Box => _box;

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
            //_textLineList?.Dispose();
            //_textLineList =new TextLineList(_pinGCTextBuffer.Ptr, _pinGCTextBuffer.BufferLength);
            //_textLineList =new TextLineList(this._textProvider);
            Monitor.Exit(_lockAlloc);

        }
        public void Free()
        {
            Monitor.Enter(_lockAlloc);
            _graphic.Free();
            //_textLineList?.Dispose();
            //_textLineList=null;
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
        private long _SetText(TextRange textRange, out TextRange surplusRange)
        {
            lock (_lockSetText)
            {
                surplusRange=new Range(0, 0);
                if (textRange.Length<=0) return 0;

                if (_textLineList==null) return 0;
                else _textLineList.Clear();
                var start=this._textDisplayRange.Start;
                long addCharCount = 0;
                TextRange _tempRange = textRange;
                long addCount = 0;
                var fontInfo = _graphic.FontProvider.GetFontInfo(CurrentFontkey);
                do
                {
                    addCount=AddLineText(new TextRange(start+addCharCount, textRange.End), CurrentFontkey, out _tempRange);
                    if (_tempRange.Length<=0) break;
                    addCharCount+=addCount;
                    if(this.ContentHeight-_textLineList.Height<fontInfo.Size)break;
                } while (addCount>0);
                _surplusRange=_tempRange;
                return addCharCount;
            }
        }
        public long InitText()
        {
            _initOk=false;
            _textLineList?.Dispose();
            _textLineList =new TextLineList(this._textProvider);

            _textDisplayRange.SetStartAndEnd(0, this._textProvider.ByteCount);
            var result = FixupText();
            _textDisplayRange.SetStartAndEnd(_textDisplayRange.Start, result);
            _initOk=true;
            return result;
        }
        //public void ReleaseText()
        //{
        //    lock (_lockRender)
        //    {

        //        _pinGCTextBuffer?.Dispose();
        //        _pinGCTextBuffer=null;
        //    }
        //}
        public bool MoveStartDisplay(int num)
        {
            if(num==0)return false;
            long textLen = _textProvider.ByteCount;
            long _start = Math.Min(Math.Max(0, _textDisplayRange.Start+num), textLen);
            if(num<0 && _textDisplayRange.Start==0 && _start==0) return false;
            long _end = Math.Min(textLen, _textDisplayRange.End+num);
            if (num>0 && _textDisplayRange.End==textLen && _end==_textDisplayRange.End) return false;
            //_start =Math.Max(0, _textDisplayRange.Start+num);
            _end=textLen;
            _textDisplayRange.SetStartAndEnd(_start, _end);
            long result = FixupText();
            _end=_start+result;
            _textDisplayRange.SetStartAndEnd(_start, _end);
            return true;
        }
        public bool JumpToDisplay(double rate)
        {
            if (rate<0) rate=0;
            if(rate>1) rate=1;
            long textLen = (int)_textProvider.Count;
            long _start =Convert.ToInt32(Math.Ceiling(rate*textLen));
            long _end =textLen;
            _textDisplayRange.SetStartAndEnd(_start, _end);
            long result = FixupText();
            _end=_start+result;
            _textDisplayRange.SetStartAndEnd(_start, _end);
            return true;
        }
        public bool MoveLineDisplay(int LineCount)
        {
            if (LineCount==0) return false;
            int dy = LineCount>0 ? 1 : -1;
            LineCount=Math.Abs(LineCount);
            if (this._textLineList.Count>0 && dy>0)
            {
                var dataLock = _textLineList.GetData();
                try
                {
                    int lineCount = dataLock.Datas.Length;
                    LineCount= Math.Min(LineCount, lineCount);
                    int aLLLen = 0;
                    for (int i = 0; i < LineCount; i++)
                    {
                        int len = (int)dataLock.Datas[i].Range.Length;
                        _lineLenStack.Push(len);
                        aLLLen+=len;
                    }
                    return MoveStartDisplay(aLLLen);
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                    dataLock.Dispose();
                }


            }
            else if (dy<0 && _lineLenStack.Count>0)
            {
                var dataLock = _textLineList.GetData();
                try
                {
                    int lineCount = dataLock.Datas.Length;
                    LineCount= Math.Min(LineCount, lineCount);
                    int aLLLen = 0;
                    for (int i = 0; i <LineCount; i++)
                    {
                        _lineLenStack.TryPop(out var len);
                        aLLLen+=len;

                    }
                    return MoveStartDisplay(-aLLLen);
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                    dataLock.Dispose();
                }
                
                //if (_lineLenStack.TryPeek(out var len) && MoveDisplayStart(-len))
                //{
                //    int i = 0;
                //    while (!_lineLenStack.TryPop(out var _))
                //    {
                //        if (i>100) throw new TimeoutException("_lineLenStack.TryPop 超时");
                //        Thread.Sleep(100);
                //        i++;
                //    }
                //    return true;
                //}
            }
            return false;
        }
        public bool MoveDisplayOneLine(int num)
        {
            
            if (this._textLineList.Count>0 && num>0)
            {
                using var dataLock = _textLineList.GetData();
                var len = (int)dataLock.Datas[0].Range.Length;
                if (MoveStartDisplay(len))
                {
                    _lineLenStack.Push(len);
                    return true;
                }
                
            }
            else if (num<0 && _lineLenStack.Count>0)
            {
                if(_lineLenStack.TryPeek(out var len) && MoveStartDisplay(-len))
                {
                    int i = 0;
                    while (!_lineLenStack.TryPop(out var _))
                    {
                        if(i>100)throw new TimeoutException("_lineLenStack.TryPop 超时");
                        Thread.Sleep(100);
                        i++;
                    }
                    return true;
                }
            }
            return false;
        }
        public void UpdateDisplayRange(long _start, long _end)
        {
            if (_start>=0 && _end<=_textProvider.Count)
            {
                _textDisplayRange.SetStartAndEnd(_start, _end);
                long result = FixupText();
                _end=_start+result;
                _textDisplayRange.SetStartAndEnd(_start, _end);
            }
            else
            {
                throw new ArgumentException("必须>=0 且 <=Text.Length");
            }
        }

        /// <summary>
        /// 重新排版
        /// </summary>
        protected long FixupText()
        {
            return _SetText(_textDisplayRange, out _surplusRange);
        }
        private long AddLineText(TextRange textRange,string FontKey,out TextRange surplusRange)
        {
            surplusRange =new TextRange(0, 0);

            if (textRange.Length<=0) return 0;
            var startOffset = textRange.Start;
            ref TextLine line=ref _textLineList.GenerateReturn();
            line.Range.Start=0; 
            line.Range.End=0;
            var contentWidth=ContentWidth;
            int startIndex = 0;
            long endIndex = textRange.Length-1;

            int newLineIndex = (int)_textProvider.IndexOf(Environment.NewLine, textRange.Start, textRange.Length);//查找第一个出现的换行符
            if (newLineIndex>=0) endIndex=newLineIndex+NewLineLen;//存在则结束下标移动到换行符后面
            line.PageMaxWidth= contentWidth;//页面可以显示的最大内容宽度
            var fontInfo=_graphic.FontProvider.GetFontInfo(FontKey);
            
            var fontSize = fontInfo.Size;
            var text=_textProvider.Slice(startOffset, (int)(endIndex-startOffset));
            int charLength = text.Length;
            Span<byte> widthMultiple = stackalloc byte[charLength];
            int index = 0;
            float WideCharWidth = fontInfo.Spacing+fontSize;//宽字符宽度
            float NarrowCharWidth = fontInfo.Spacing+fontSize/2;//窄字符宽度

            foreach (var charObj in text)
            {
                
                float width = _graphic.MeasureText(charObj, FontKey);
                if (width/(fontSize/2)>1.0F)
                {
                    //宽字符
                    if (contentWidth>=(WideCharWidth))
                    {
                        //宽度够还可以容纳
                        widthMultiple[index]=2;
                        contentWidth-=WideCharWidth;
                    }
                    else
                    {
                        break;
                    }
                }
                else if (width>0)
                {
                    //窄字符
                    if (contentWidth>=NarrowCharWidth)
                    {
                        //宽度够还可以容纳
                        widthMultiple[index]=1;
                        contentWidth-=NarrowCharWidth;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    widthMultiple[index]=0;
                }
                index++;
                if ((index >=endIndex || contentWidth<NarrowCharWidth)) break;
            }
            var byteCount = _textProvider.Encoding.GetByteCount(text[0..index]);
            endIndex=Math.Min(endIndex, startOffset+byteCount);
            surplusRange =new TextRange(endIndex, textRange.End);

            line.Range.Start=startOffset+startIndex;//一行的范围
            line.Range.End=endIndex;
            TextItem[] textItems = ArrayPool<TextItem>.Shared.Rent(1);
            textItems[0]._Range=line.Range;
            textItems[0].FontInfo=fontInfo;
            textItems[0]._TextProvider=this._textProvider;
            //textItems[0]._Source=_pinGCTextBuffer.Ptr;
            //textItems[0]._SourceLength=_pinGCTextBuffer.BufferLength;
            textItems[0].FillWidthMultiple(widthMultiple);
            textItems[0].Update();
            line.FillItems(textItems.AsSpan(0,1));
            line.Update();
            ArrayPool<TextItem>.Shared.Return(textItems);
            return endIndex-startOffset;
            
        }
        private readonly Task[] _invokeTask=new Task[100];
        private SKTextBlob _currentSKTextBlob;
        public unsafe  void Render(string fontKey = null)
        {
            lock (_lockRender)
            {
                if (!_initOk) return;
                if (_actionQueue!=null && _actionQueue.Count>0)
                {
                    //using Timer timer = new Timer((state) =>
                    //{
                    //    Debug.WriteLine("执行超时");
                    //},null,1000,1000);
                    int queueCount = _actionQueue.Count;
                    int len = Math.Min(_invokeTask.Length, queueCount);
                    do
                    {
                        for (int tIndex = 0; tIndex < len; tIndex++)
                        {
                            if (_actionQueue.TryDequeue(out var action))
                            {
                                //if (action!=null) _invokeTask[tIndex]=Task.Run(() =>
                                //{
                                //    Debug.WriteLine($"S:{action}");
                                //    action?.Invoke(this);
                                //    Debug.WriteLine($"E:{action}");
                                //});
                                //else _invokeTask[tIndex]=Task.CompletedTask;
                                action?.Invoke(this);
                            }
                            else
                            {
                                _invokeTask[tIndex]=Task.CompletedTask;
                            }
                        }

                        //for (int tIndex = 0; tIndex < len; tIndex++)
                        //{
                        //    try
                        //    {

                        //        _invokeTask[tIndex].Wait(1000);
                        //    }
                        //    catch (Exception)
                        //    {

                        //        throw;
                        //    }
                        //}
                        len=queueCount-len;
                    } while (len>0);
                    
                }

                if (NextFixupText)
                {
                    //var t=this._textProvider.Slice(this._textDisplayRange);
                    FixupText();
                    _nextFixupText = false;
                }

                Monitor.Enter(_lockAlloc);
                if (_graphic==null) return;
                _graphic?.Clear(_backgroundColor);
                //_canvas.DrawRoundRectDifference(new SKRoundRect(new SKRect(1, 1, Width-1, Height-1), 1),
                //    new SKRoundRect(new SKRect(2, 2, Width-2, Height-2), 1), paint);
                using var dataLock=_textLineList.GetData();

                
                var y = PageMarginTop;
                foreach (var Line in dataLock.Datas)
                {
                    y+=Line.LineHeight;
                    var x = PageMarginLeft;
                    foreach (var item in Line.Items)
                    {
                        x+=item.Margin.Left;
                        //_graphic?.DrawText();
                        //for (int i = 0; i < item.Text.Length; i++)
                        //{
                        //    var c= item.Text[i];
                        //    int dx=item.GetCharWidthIndex(i);
                        //    _graphic?.DrawText(c, x, y, CurrentFontkey);
                        //    x+=dx;
                        //}

                        _graphic.DrawText(item.Text, x, y, CurrentFontkey);

                        for (int i = 0; i < item.Text.Length; i++)
                        {
                            //var c = item.Text[i];
                            int dx = item.GetCharWidthIndex(i);
                            //_graphic?.DrawText(c, x, y, CurrentFontkey);
                            x+=dx;
                        }
                        

                    }

                }

                //for (int i = 0; i < LineCount && i<_lines.Count; i++)
                //{
                //    var x = PageMarginLeft;
                //    var y = (i+1)*LineHeight+PageMarginTop;
                //    var item = _text.AsSpan()[_lines[i].Range];
                //    for (int j = 0; j < item.Length; j++)
                //    {
                //        //_graphic?.DrawText_NoGC(_textBlobBuilder.Handle,item[j..(j+1)], x, y, paint, font);
                //        _graphic?.DrawText(item[j..(j+1)],x,y, fontKey);
                //        var multiple = _lines[i].IsFatCharArray[j];
                //        if (multiple>0) x+=(MathF.Max(multiple*FontSize/2+_charLeftOffset, 1));
                //    }
                //}
                _onRenderFinished?.Invoke(ReadOnlyBytes);
                Monitor.Exit(_lockAlloc);
            }
        }

        public void Invoke(Action<TextFrame> action)
        {
            lock (_lockRender)
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
            
        }
        public bool CopyTo(Span<byte> target,out Range range)
        {
            range=new Range(0,0);
            if (target==null || target.Length<=0) return true;
            Monitor.Enter(_lockAlloc);
            int size = Math.Min(target.Length, BytesSize);
            range=new Range(0, size);
            Bytes[range].CopyTo(target);
            Monitor.Exit(_lockAlloc);
            return true;    
        }
        public void Dispose()
        {
            _graphic?.Dispose();
            _textProvider?.Dispose();
            //_pinGCTextBuffer?.Dispose();
        }

        
    }
}
