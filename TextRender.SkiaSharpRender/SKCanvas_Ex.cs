using System;
using SkiaSharp;


namespace TextRender.SkiaSharpRender
{
    
    internal static class SKCanvas_Ex
    {
        internal static unsafe int CountGlyphs(IntPtr FontHandle, void* text, int length, SKTextEncoding encoding)
        {
            if (text==null|| length==0)
            {
                return 0;
            }

            return SkiaApi_Ex.sk_font_text_to_glyphs(FontHandle, text, (IntPtr)length, encoding, null, 0);
        }
        internal static unsafe int CountGlyphs(IntPtr FontHandle, ReadOnlySpan<byte> text, int length, SKTextEncoding encoding)
        {
            if (text==null|| length==0)
            {
                return 0;
            }
            fixed (byte* ptr = text)
            {
                void* text2 = ptr;
                return CountGlyphs(FontHandle,text2, text.Length, encoding);
            }
        }
        public unsafe static void DrawText_NoGC(this SKCanvas canvas,
            IntPtr textBlobBuilderHandle,
            ReadOnlySpan<char> text,
            float x,
            float y,
            SKPaint paint,
            SKFont font,
            SKPoint origin = default(SKPoint))
        {

            if (paint == null)
            {
                throw new ArgumentNullException("paint");
            }
            if (font == null)
            {
                throw new ArgumentNullException("font");
            }
            var length = text.Length*2;
            int num = font.CountGlyphs(text);
            if (num <= 0)
            {
                return ;
            }
            fixed (void* text2 = text)
            {
                Span<IntPtr> ptrs = stackalloc IntPtr[4];
                fixed (void* tptr = ptrs)
                {

                    SkiaApi_Ex.sk_textblob_builder_alloc_run_pos(textBlobBuilderHandle, font.Handle, num, null, tptr);
                    if (ptrs[0]==IntPtr.Zero) return;
                    if (ptrs[1]==IntPtr.Zero) return;
                    SkiaApi_Ex.sk_font_text_to_glyphs(font.Handle, text2, (IntPtr)length, SKTextEncoding.Utf16, (ushort*)ptrs[0], num);
                    SkiaApi_Ex.sk_font_get_pos(font.Handle, (ushort*)ptrs[0], num, (SKPoint*)ptrs[1],&origin);
                    var textHandle=SkiaApi_Ex.sk_textblob_builder_make(textBlobBuilderHandle);
                    SkiaApi_Ex.sk_canvas_draw_text_blob(canvas.Handle,textHandle,x,y,paint.Handle);
                    SkiaApi_Ex.sk_textblob_unref(textHandle);
                }
            }
        }
        public unsafe static void DrawText_NoGC(this SKCanvas canvas,
            IntPtr textBlobBuilderHandle,
            ReadOnlySpan<byte> text,
            float x,
            float y,
            SKPaint paint,
            SKFont font,
            SKPoint origin = default(SKPoint))
        {
            if (paint == null)
            {
                throw new ArgumentNullException("paint");
            }
            if (font == null)
            {
                throw new ArgumentNullException("font");
            }
            var length = text.Length;
            int num = CountGlyphs(font.Handle,text, text.Length,SKTextEncoding.Utf16);
            if (num <= 0)
            {
                return;
            }
            fixed (void* text2 = text)
            {
                Span<IntPtr> ptrs = stackalloc IntPtr[4];
                fixed (void* tptr = ptrs)
                {

                    SkiaApi_Ex.sk_textblob_builder_alloc_run_pos(textBlobBuilderHandle, font.Handle, num, null, tptr);
                    if (ptrs[0]==IntPtr.Zero) return;
                    if (ptrs[1]==IntPtr.Zero) return;
                    SkiaApi_Ex.sk_font_text_to_glyphs(font.Handle, text2, (IntPtr)length, SKTextEncoding.Utf16, (ushort*)ptrs[0], num);
                    SkiaApi_Ex.sk_font_get_pos(font.Handle, (ushort*)ptrs[0], num, (SKPoint*)ptrs[1], &origin);
                    var textHandle = SkiaApi_Ex.sk_textblob_builder_make(textBlobBuilderHandle);
                    SkiaApi_Ex.sk_canvas_draw_text_blob(canvas.Handle, textHandle, x, y, paint.Handle);
                    SkiaApi_Ex.sk_textblob_unref(textHandle);
                }
            }
        }
    }
    
}
