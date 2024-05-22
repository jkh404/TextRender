using System;
using System.Buffers;
using System.Numerics;
using TextRender.Command;
using TextRender.Handles.Abstracts;
namespace TextRender
{
    public struct TextItem : IDisposable
    {
        //internal IntPtr _Source;
        //internal int _SourceLength;
        internal ITextProviderHandle _TextProvider;
        internal TextRange _Range;
        internal float _ContentWidth;
        public BoxSpacing _Margin;

        public FontInfo FontInfo;
        public byte[] WidthMultiple;
        public int _WidthMultipleLen;
        public byte[] Bitmap;
        public int BitmapWidth;
        internal unsafe TextItem(
            ITextProviderHandle textProvider,
            TextRange textRange,
            BoxSpacing margin,
            FontInfo fontInfo,
            ReadOnlySpan<byte> widthMultiple, byte[] bitmap, int bitmapWidth)
        {
            //_Source=source;
            //_SourceLength=sourceLength;
            _TextProvider=textProvider;
            _Range =textRange;
            _Margin=margin;
            FontInfo=fontInfo;
            Bitmap=bitmap;
            WidthMultiple=ArrayPool<byte>.Shared.Rent(widthMultiple.Length);
            widthMultiple.CopyTo(WidthMultiple.AsSpan(0, widthMultiple.Length));
            _WidthMultipleLen=widthMultiple.Length;
            BitmapWidth =bitmapWidth;

            _ContentWidth =0;
            _ContentWidth =GetContentWidth();
        }

        //private unsafe Span<char> SourceText => new Span<char>((void*)_Source, _SourceLength);
        //private unsafe ReadOnlySpan<char> ReadOnlySourceText => SourceText;
        public  ReadOnlySpan<char> Text => _TextProvider.Slice(Range);
        public  ReadOnlySpan<byte> TextBtyes => _TextProvider.SliceByte(Range);
        public BoxSpacing Margin => _Margin;
        public TextRange Range => _Range;
        public float FontSize => FontInfo.Size;
        public float Spacing => FontInfo.Spacing;
        public float ItemWidth => _Margin.Left+_Margin.Right+_ContentWidth;
        public float ItemHeight
        {
            get
            {
                if (BitmapWidth>0&& Bitmap!=null && Bitmap.Length>0) return Bitmap.Length/(BitmapWidth*4);
                return _Margin.Top+_Margin.Bottom+FontSize;
            }
        }
        public bool IsBitmap => BitmapWidth>0&& Bitmap!=null && Bitmap.Length>0;
        internal void Update()
        {
            _ContentWidth =GetContentWidth();
        }
        private float GetContentWidth()
        {
            if (IsBitmap) return BitmapWidth;
            if (WidthMultiple==null || WidthMultiple.Length<=0) return 0;
            var fontSize = FontSize/2;
            var spacing = Spacing;
            float sumNum = 0;
            try
            {
                if (Vector.IsHardwareAccelerated && WidthMultiple.Length/Vector<float>.Count>0)
                {
                    var spacingV = new Vector<float>(spacing);
                    var Len = WidthMultiple.Length;
                    var VCount = Vector<float>.Count;
                    var count = Len/VCount;
                    Vector<float>[] vectors = ArrayPool<Vector<float>>.Shared.Rent(count);
                    Vector<float> resultV = Vector<float>.Zero;
                    int startIndex = 0;
                    
                    for (startIndex = 0; startIndex < Len; startIndex+=VCount)
                    {
                        var data = WidthMultiple.AsSpan(startIndex, VCount);
                        Span<float> _data = stackalloc float[VCount];
                        for (int j = 0; j < VCount; j++)
                        {
                            _data[j]=data[j];
                        }
                        vectors[startIndex/VCount]=new Vector<float>(_data);
                    }
                    foreach (var data in vectors.AsSpan(0, count))
                    {
                        resultV+=(data*fontSize);
                        resultV+=spacingV;
                    }
                    ArrayPool<Vector<float>>.Shared.Return(vectors);
                    for (int i = 0; startIndex+i < startIndex+VCount;  i++)
                    {
                        sumNum+=resultV[i];
                        if (Len % VCount>0 && startIndex<Len) sumNum+=WidthMultiple[startIndex+i];
                    }

                    var _tempData = WidthMultiple.AsSpan();
                    for (int i = 0; i < _tempData.Length; i++)
                    {
                        if (_tempData[i]>0) sumNum+=spacing;
                    }
                    return sumNum;

                }
            }
            catch
            {

            }
            for (int i = 0; i < WidthMultiple.Length; i++)
            {


                if (WidthMultiple[i]>0)
                {
                    var num = (WidthMultiple[i]*fontSize)+spacing;
                    sumNum+=num;
                }
                else
                {
                    continue;
                }
            }
            return sumNum;
        }

        public void FillWidthMultiple(ReadOnlySpan<byte> widthMultiple)
        {
            WidthMultiple=ArrayPool<byte>.Shared.Rent(widthMultiple.Length);
            widthMultiple.CopyTo(WidthMultiple.AsSpan(0, widthMultiple.Length));
            _WidthMultipleLen=widthMultiple.Length;
        }

        public string GetText()
        {
            return new string(Text);
        }
        public int GetCharWidthIndex(int i)
        {
            var result = WidthMultiple[i] * (FontSize/2);
            if (result>0) result+=Spacing;
            return Convert.ToInt32(MathF.Ceiling(result));
        }

        public void Dispose()
        {
            if(WidthMultiple!=null) ArrayPool<byte>.Shared.Return(WidthMultiple);
            WidthMultiple=null;
            Bitmap=null;
            BitmapWidth=0;
            _WidthMultipleLen=0;
        }
    }
}
