using System;
using System.Buffers;
using TextRender.Command;
using static TextRender.TextLine;
namespace TextRender
{
    public  struct TextLine
    {
        private object _lockUpdate;
        internal IntPtr _Source;
        internal int _SourceLength;
        private float _MaxWidth;
        private float _MaxHeight;
        internal float PageMaxWidth;
        public Margin Margin;
        public TextRange Range;

        private TextItem[] _TextItems;
        private int  _ItemCount;
        internal TextLine(IntPtr source,int sourceLength, Margin margin,float pageMaxWidth, ReadOnlySpan<TextItem> textItems)
        {
            if (textItems==null || textItems.Length<=0) throw new ArgumentException(nameof(textItems));
            _Source=source;
            _SourceLength=sourceLength;
            Margin =margin;
            Range=0..0;
            _TextItems =null;
            _ItemCount=0;
            _MaxWidth =0;
            _MaxHeight=0;
            PageMaxWidth=pageMaxWidth;
            _lockUpdate=new object();
            //FillItems(textItems);
            //Update();
        }
        public int LineWidth=>Convert.ToInt32(MathF.Ceiling(_MaxWidth+Margin.Left+Margin.Right));
        public int LineHeight => Convert.ToInt32(MathF.Ceiling(_MaxHeight+Margin.Top+Margin.Bottom));
        public int ItemCount=> _ItemCount;
        private unsafe Span<char> SourceText => new Span<char>((void*)_Source, _SourceLength);
        private unsafe ReadOnlySpan<char> ReadOnlySourceText => SourceText;
        public unsafe ReadOnlySpan<char> Text => ReadOnlySourceText[Range.AsRange()];
        public ReadOnlySpan<TextItem> Items => _TextItems.AsSpan(0, _ItemCount);
        public void Update()
        {
            if (_lockUpdate==null) throw new InvalidOperationException("更新文本行排版失败");
            if (_ItemCount>0)
            {
                lock (_lockUpdate)
                {
                    if (_ItemCount<=0) return;
                    Range=new TextRange(Items[0].Range.Start, Items[^1].Range.End);
                    var maxWidth = 0F;
                    foreach (var item in Items)
                    {
                        item.Update();
                        if (item.ItemHeight>_MaxHeight) _MaxHeight=item.ItemHeight;
                        maxWidth+=item.ItemWidth;
                    }
                    _MaxWidth=maxWidth;
                }
                

            }
            else
            {
                throw new InvalidOperationException("子项必须大于0");
            }
        }
        public void FillItems(ReadOnlySpan<TextItem> textItems)
        {
            _ItemCount=textItems.Length;
            _TextItems =ArrayPool<TextItem>.Shared.Rent(_ItemCount);
            textItems.CopyTo(_TextItems.AsSpan(0, _ItemCount));
            Range =new TextRange(_TextItems[0].Range.Start, _TextItems[_ItemCount-1].Range.End);
        }
        internal void Init(IntPtr source, int sourceLength)
        {
            _Source=source;
            _SourceLength=sourceLength;
            _lockUpdate??=new object();
        }
        public void Clear()
        {
            lock (_lockUpdate)
            {
                if (_TextItems!=null)
                {
                    foreach (var item in _TextItems.AsSpan(0, _ItemCount))
                    {
                        item.Dispose();
                    }
                    ArrayPool<TextItem>.Shared.Return(_TextItems);
                    _TextItems=null;
                    _ItemCount=0;
                }
                
            }
        }

        public unsafe static TextItem CreateItem(IntPtr source,int sourceLength, TextRange textRange, Margin margin,FontInfo fontInfo, byte[] widthMultiple)
        {

            TextItem textItem=new TextItem(source, sourceLength, textRange, margin, fontInfo, widthMultiple,null,0);
            return  textItem;
        }
        public unsafe static TextItem CreateItem(IntPtr source, int sourceLength,  Margin margin, byte[] bitmap, int bitmapWidth)
        {
            if (bitmap==null || bitmap.Length==0 || bitmapWidth==0 || bitmap.Length%(bitmapWidth*4)>0) throw new ArgumentException();
            TextItem textItem = new TextItem(source, sourceLength, new TextRange(0,0), margin, null, null, bitmap, bitmapWidth);
            return textItem;
        }
        public static void MakeItem(ref TextItem item,IntPtr source, int sourceLength, TextRange? textRange)
        {
            item._Source=source;
            item._SourceLength=sourceLength;
            item._Range=textRange??(0..0);
        }
        
    }
}
