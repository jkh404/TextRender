using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
namespace TextRender
{
    /// <summary>
    /// 文本渲染片段
    /// </summary>
    public class TextFrame : IDisposable
    {
        public delegate void OnRender(ReadOnlySpan<byte> data);
        private IntPtr _dataPtr;
        private SKBitmap _bitmap;
        private OnRender? _onRender;
        private Margin _lineMargin=new Margin();
        private Margin _pageMargin = new Margin();
        private string _text=string.Empty;
        //下次渲染，立即进行文本重排版
        private bool _nextFixupText = false;
        private bool NextFixupText
        {
            set { _nextFixupText = value || _nextFixupText; }
            get { return _nextFixupText; }
        }


        private Range _surplusRange;
        private List<(Range Range, byte[] IsFatCharArray)> _lines;
        private SKColor _backgroundColor= SKColors.Transparent;
        private string _currentFontkey=string.Empty;

        private object _lockRender = new object();
        //队列私有字段
        protected  ConcurrentQueue<Action<TextFrame>> _actionQueue = new ConcurrentQueue<Action<TextFrame>>();

        protected Range _textDisplayRange;
        protected SKCanvas _canvas;
        protected SKImageInfo  _imageInfo;
        protected SKFont _defaultFont;
        protected SKPaint _defaultPaint;
        protected SKFont? _currentFont;
        protected SKPaint? _currentPaint;
        
        public SKTextEncoding Encoding { get;protected set; }

        protected virtual IFontDictionary FontDictionary { get;private set; }
        public unsafe ReadOnlySpan<byte> ReadOnlyBytes
        {
            get
            {
                //lock (_lockRender)
                //{

                //    return new ReadOnlySpan<byte>(_dataPtr.ToPointer(), _imageInfo.BytesSize);
                //}
                return new ReadOnlySpan<byte>(_dataPtr.ToPointer(), _imageInfo.BytesSize);
            }
        }
        protected unsafe Span<byte> Bytes => new Span<byte>(_dataPtr.ToPointer(), _imageInfo.BytesSize);
        protected IEnumerable<string> Lines => _lines.Select(r => _text[r.Range]);
        /// <summary>
        /// 多余的文本
        /// </summary>
        public ReadOnlySpan<char> SurplusText => _text[_surplusRange];
        public int BytesSize => _imageInfo.BytesSize;
        public virtual int Width
        {
            get => _imageInfo.Width;
            set
            {
                _imageInfo.Width = value;
                NextFixupText=true;
                Alloc();
            }
        }
        public virtual int Height
        {
            get => _imageInfo.Height;
            set
            {
                _imageInfo.Height = value;
                NextFixupText=true;
                Alloc();
            }
        }
        protected SKFont CurrentFont=> _currentFont==null ? _defaultFont : GetFont(_currentFontkey)??_defaultFont; 
        protected SKPaint CurrentPaint=> _currentPaint==null ? _defaultPaint : GetPaint(_currentFontkey)??_defaultPaint; 
        public float FontSize
        {
            get => CurrentFont.Size;
            set
            {
                _defaultFont.Size = value;
                _defaultPaint.TextSize = value;
                CurrentFont.Size = _defaultFont.Size;
                CurrentPaint.TextSize = _defaultPaint.TextSize;
                FontDictionary?.Resize(_defaultFont.Size);
                NextFixupText=true;
            }
        }
        public string TextColor
        {
            get => $"{CurrentPaint.Color}";
            set
            {
                _defaultPaint.Color = SKColor.Parse(value);
                CurrentPaint.Color = _defaultPaint.Color;
                FontDictionary?.Resize(_defaultFont.Size);
            }
        }
        private float _charLeftOffset;
        public float CharWidth => MathF.Max(FontSize/2.0F+_charLeftOffset,1);
        public float DCharWidth => MathF.Max(FontSize+_charLeftOffset,1);
        
        public float LineHeight => MathF.Max(_lineMargin.Top+_lineMargin.Bottom+FontSize, 1);

        public float ContentHeight => MathF.Max(Height-_pageMargin.Top-_pageMargin.Bottom,0);
        public float ContentWidth => MathF.Max(Width-_pageMargin.Left-_pageMargin.Right-_lineMargin.Left-_lineMargin.Right,0);
        public int LineCount => Math.Max(Convert.ToInt32(MathF.Floor(ContentHeight/LineHeight)),0);
        public int LineCharCount=> Math.Max(Convert.ToInt32(MathF.Floor(ContentWidth/CharWidth)),0);

        public TextFrame(int width, int height, OnRender? onRender=null) : this(new SKImageInfo(width, height), onRender)
        {
            if (width<=0 || height<=0) throw new ArgumentException("width and height must be greater than 0");
        }

        public string CurrentFontkey
        {
            get => _currentFontkey;
            set
            {
                _currentFontkey=value;
                _currentPaint=FontDictionary.GetPaint(_currentFontkey);
                _currentFont=FontDictionary[_currentFontkey];
                NextFixupText=true;
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

        protected TextFrame(SKImageInfo sKImageInfo, OnRender? onRender)
        {
            _onRender=onRender;
            _imageInfo = sKImageInfo;
            
            _defaultFont= new SKFont(SKTypeface.FromFamilyName("宋体"));
            _defaultPaint=new SKPaint(_defaultFont) {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Color = SKColors.Black,
                TextAlign = SKTextAlign.Left,
            };
            Encoding = SKTextEncoding.Utf16;
            _dataPtr=IntPtr.Zero;
            _lines=new List<(Range range, byte[] IsFatCharArray)>(1000);
            Alloc();
        }
        public void Alloc()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
            if (_dataPtr!=IntPtr.Zero) Marshal.FreeHGlobal(_dataPtr);
            _dataPtr = Marshal.AllocHGlobal(_imageInfo.BytesSize);
            _bitmap = new SKBitmap();
            _bitmap.InstallPixels(_imageInfo, _dataPtr);
            _canvas = new SKCanvas(_bitmap);
        }
        public void Free()
        {
            if (_dataPtr!=IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_dataPtr);
                _dataPtr=IntPtr.Zero;
            }
            _canvas?.Dispose();
            _bitmap?.Dispose();
        }
        public void InitFont(float fontSize,string hexColor, Action<IFontAdd>? action=null)
        {
            _defaultPaint.Color=SKColor.Parse(hexColor);
            _defaultFont.Size=fontSize;
            _defaultPaint.TextSize=fontSize;
            _defaultPaint.TextEncoding=Encoding;
            FontDictionary?.Dispose();
            FontDictionary =new DefaultFontDictionary(false, fontSize);
            action?.Invoke(FontDictionary);
        }
        protected virtual SKFont? GetFont(string fontKey)
        {
            return FontDictionary[fontKey];
        }
        protected virtual SKPaint? GetPaint(string fontKey)
        {
            return FontDictionary.GetPaint(fontKey);
        }
        private int _SetText(ReadOnlySpan<char> text, out Range surplusRange)
        {
            if(_lines!=null && _lines.Count>0)_lines.Clear();
            surplusRange=new Range(0, 0);
            if (text==null || text.Length<=0) return 0;
            if (_lines.Count>=LineCount) return 0;
            int addCharCount = 0;
            Range _tempRange=0..text.Length;
            List<byte>? isFatCharArray = new List<byte>(1000);
            var _start = _textDisplayRange.Start.Value;
            for (int i = _lines.Count; i < LineCount; i++)
            {
                var lastLineEndIndex = addCharCount;
                addCharCount+=AddLineText(i, text[addCharCount..text.Length], out _tempRange, isFatCharArray);
                _lines.Add(((_start+lastLineEndIndex)..(_start+addCharCount), isFatCharArray.ToArray()));
                isFatCharArray.Clear();
                if ((_tempRange.End.Value-_tempRange.Start.Value)<=0)
                {
                    break;
                }
                
            }
            //if(string.IsNullOrEmpty(_text))_text=text[0..Math.Min(addCharCount, text.Length)].ToString();
            _surplusRange=0..0;
            return addCharCount;

        }
        public int InitText(string text)
        {
            _text=text;
            _textDisplayRange=new Range(0, _text.Length);
            var result=FixupText();
            _textDisplayRange=new Range(_textDisplayRange.Start, result);
            return result;
        }
        public bool MoveDisplayStart(int num)
        {
            var _start=_textDisplayRange.Start.Value;
            var _end = _textDisplayRange.End.Value;
            if (_start+num>=0 && _end+num<_text.Length)
            {
                _start+=num;
                _end=_text.Length;
                _textDisplayRange=new Range(_start, _end);
                var result = FixupText();
                _end=_start+result;
                _textDisplayRange=new Range(_start, _end);
                return true;
            }
            else
            {
                return false;
            }
        }
        public void UpdateDisplayRange(int _start,int _end)
        {
            if (_start>=0 && _end<=_text.Length)
            {
                _textDisplayRange=new Range(_start, _end);
                var result = FixupText();
                _end=_start+result;
                _textDisplayRange=new Range(_start, _end);
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
            return _SetText(_text.AsSpan()[_textDisplayRange], out _surplusRange);
        }
        private int AddLineText(int lineIndex,ReadOnlySpan<char> text,out Range surplusRange, List<byte>? isFatCharArray=null)
        {

            surplusRange =new Range(0,0);
            if (text.Length<=0) return 0;
            ReadOnlySpan<char> _temp;
            var endIndex = text.Length-1;
            var newLineLen = Environment.NewLine.Length;
            var newLineIndex = text.IndexOf(Environment.NewLine);
            if (newLineIndex>=0) endIndex=newLineIndex+newLineLen;
            var contentWidth = ContentWidth;
            var startIndex = 0;
            for (startIndex = 0; startIndex <endIndex; startIndex++)
            {
                _temp=text[startIndex..(startIndex+1)];
                var width = CurrentPaint.MeasureText(_temp);
                if (width/(FontSize/2)>1.0F)
                {
                    //宽字符
                    if (contentWidth>=DCharWidth)
                    {
                        isFatCharArray?.Add(2);
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
                        isFatCharArray?.Add(1);
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
                    isFatCharArray?.Add(0);
                }
                
                if ((startIndex >=endIndex || contentWidth<CharWidth)) break;
            }
            endIndex=startIndex;
            //if (newLineIndex>=0 && newLineLen==2)
            //{
            //    endIndex++;
            //    //isFatCharArray?.Add(0);
            //}
            //else endIndex++;
            surplusRange =new Range(endIndex,text.Length);
            return endIndex;
        }
        public  void Render(string? useFontKey=null)
        {
            lock (_lockRender)
            {
                while (!(_actionQueue?.IsEmpty ?? true))
                {
                    if (_actionQueue.TryDequeue(out var action))
                    {
                        action?.Invoke(this);
                    }
                    else
                    {
                        break;
                    }
                }
                if (NextFixupText)
                {
                    FixupText();
                    _nextFixupText = false;
                }
                
                var paint = string.IsNullOrEmpty(useFontKey) ? CurrentPaint : GetPaint(useFontKey);
                _canvas.Clear(_backgroundColor);
                //_canvas.DrawRoundRectDifference(new SKRoundRect(new SKRect(1,1,Width-1,Height-1),1), 
                //    new SKRoundRect(new SKRect(2, 2, Width-2, Height-2), 1), paint);
                for (int i = 0; i < LineCount && i<_lines.Count; i++)
                {
                    var x = PageMarginLeft;
                    var y = (i+1)*LineHeight+PageMarginTop;
                    var item = _text[_lines[i].Range];
                    for (int j = 0; j < item.Length; j++)
                    {
                        _canvas.DrawText(item[j..(j+1)], x, y, paint);
                        if (_lines[i].IsFatCharArray[j]>0) x+=(MathF.Max(_lines[i].IsFatCharArray[j]*FontSize/2+_charLeftOffset, 1));
                    }
                }
                _onRender?.Invoke(ReadOnlyBytes);
            }
        }

        public void Invoke(Action<TextFrame> action)
        {

            if (action!=null)
            {
                const int MAX_INVOKE_TRY = 100;
                int i = 0;
                while (_actionQueue.Count>MAX_INVOKE_TRY)
                {
                    if (i>MAX_INVOKE_TRY) return;
                    Thread.Sleep(100);
                    i++;
                }
                _actionQueue.Enqueue(action);
            }
        }
        public void Dispose()
        {
            _canvas?.Dispose();
            _bitmap?.Dispose();
            if(_dataPtr!=IntPtr.Zero)Marshal.FreeHGlobal(_dataPtr);
            FontDictionary?.Dispose();
            _defaultFont?.Dispose();
            _defaultPaint?.Dispose();

        }
    }
}
