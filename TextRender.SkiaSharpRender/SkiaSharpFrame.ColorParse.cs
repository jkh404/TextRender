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

        public int Parse(string hexColorString)
        {
            return (int)((uint)SKColor.Parse(hexColorString));
        }

        public int From(byte r, byte g, byte b, byte a)
        {
            SKColor sKColor = new SKColor(r,g,b,a);

            return(int)((uint)sKColor);
        }

        public int FromHsl(float h, float s, float l, byte a = 255)
        {
            return (int)((uint)SKColor.FromHsl(h,s,l,a));
        }

        public int FromHsv(float h, float s, float v, byte a = 255)
        {
            return (int)((uint)SKColor.FromHsv(h, s, v, a));
        }

        public string ToHexString(uint color)
        {
            SKColor sKColor = new SKColor(color);
            return sKColor.ToString();
        }

        public string ToHexString(byte r, byte g, byte b, byte a)
        {
            SKColor sKColor = new SKColor(r, g, b, a);
            return sKColor.ToString();
        }
        public SKColor ToColor(uint color)
        {
            return new SKColor(color);
        }

        public SKColor ToColor(byte r, byte g, byte b, byte a)
        {
            return new SKColor(r, g, b, a);
        }

        public SKColor HexToColor(string hexColorString)
        {
            return SKColor.Parse(hexColorString);
        }

        public SKColor HslToColor(float h, float s, float l, byte a = 255)
        {
            return SKColor.FromHsl(h, s, l, a);
        }

        public SKColor HsvToColor(float h, float s, float v, byte a = 255)
        {
            return SKColor.FromHsv(h, s, v, a);
        }

    }
}
