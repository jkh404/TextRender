using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace TextRender.SkiaSharpRender
{
    internal static class SkiaApi_Ex
    {
        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern void sk_text_utils_get_path(void* text, IntPtr length, SKTextEncoding encoding, float x, float y, IntPtr font, IntPtr path);
        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void sk_textblob_unref(IntPtr blob);
        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sk_textblob_builder_make(IntPtr builder);
        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void sk_canvas_draw_text_blob(IntPtr param0, IntPtr text, float x, float y, IntPtr paint);

        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern void sk_textblob_builder_alloc_run_pos(IntPtr builder, IntPtr font, int count, SKRect* bounds, void* runbuffer);

        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern int sk_font_text_to_glyphs(IntPtr font, void* text, IntPtr byteLength, SKTextEncoding encoding, ushort* glyphs, int maxGlyphCount);
        [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern void sk_font_get_pos(IntPtr font, ushort* glyphs, int count, SKPoint* pos, SKPoint* origin);
    }
}
