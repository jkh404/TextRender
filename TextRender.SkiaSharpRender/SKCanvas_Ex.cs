using System;
using SkiaSharp;


namespace TextRender.SkiaSharpRender
{
    internal static class SKCanvas_Ex
    {
        //static SKPath sKPath = new SKPath();
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
    }
    
}
