using System;
using System.Runtime.InteropServices;
using System.Threading;
using SkiaSharp;
using TextRender.Abstracts;
using TextRender.Command;

namespace TextRender.SkiaSharpRender
{

    internal partial class SkiaSharpFrame : IGraphic<SKColor>
    {
        public IFontProvider FontProvider => _fontProvider;
        public FontInfo CurrentFontInfo=>_fontProvider.GetFontInfo(_currentFontKey);
        public string CurrentFontKey
        {
            get => _currentFontKey;
            set
            {
                if (FontProvider?.ContainsKey(value)??false) _currentFontKey=value;
                else throw new Exception();
            }
        }
        public float MeasureText(ReadOnlySpan<char> text, string? fontKey = null)
        {
            var paint= _fontProvider.GetPaint(fontKey??CurrentFontKey);
            return paint.MeasureText(text);
        }
        public float MeasureText(char c, string? fontKey = null)
        {
            var paint = _fontProvider.GetPaint(fontKey??CurrentFontKey);
            Span<char> chars=stackalloc char[1];
            chars[0]=c;
            return paint.MeasureText(chars);
        }
        public void DrawText(char c, float X = 0, float Y = 0, string? fontKey = null)
        {
            Monitor.Enter(_lockResize);
            Monitor.Enter(_lockAlloc);
            fontKey??=CurrentFontKey;
            var font = _fontProvider.GetFont(fontKey);
            var paint = _fontProvider.GetPaint(fontKey);
            if (font==null) throw new ArgumentException(nameof(fontKey));
            if (paint==null) throw new ArgumentException(nameof(fontKey));
            Span<char> text = stackalloc char[1];
            text[0]=c;
            _canvas?.DrawText_NoGC(_textBlobBuilder.Handle, text, X, Y, paint, font);
            Monitor.Exit(_lockAlloc);
            Monitor.Exit(_lockResize);
        }
        public void DrawText(ReadOnlySpan<char> text, float X = 0, float Y = 0, string? fontKey=null)
        {
            Monitor.Enter(_lockResize);
            Monitor.Enter(_lockAlloc);
            fontKey??=CurrentFontKey;
            var font = _fontProvider.GetFont(fontKey);
            var paint = _fontProvider.GetPaint(fontKey);
            if (font==null) throw new ArgumentException(nameof(fontKey));
            if (paint==null) throw new ArgumentException(nameof(fontKey));
            _canvas?.DrawText_NoGC(_textBlobBuilder.Handle, text,X,Y, paint, font);
            Monitor.Exit(_lockAlloc);
            Monitor.Exit(_lockResize);
        }

        public void Clear(uint? color = null)
        {
            var _color = ToColor(color??_backgroundColor);
            _canvas?.Clear(_color);
        }
    }
}
