using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextRender.Command;

namespace TextRender.Abstracts
{
    public interface IDraw:IDisposable
    {
        IFontProvider FontProvider { get; }
        FontInfo CurrentFontInfo { get; }
        string CurrentFontKey { get; set; }
        float MeasureText(char c, string? fontKey=null);
        float MeasureText(ReadOnlySpan<byte> text, string? fontKey=null);
        float MeasureText(ReadOnlySpan<char> text, string? fontKey=null);
        void DrawText(char c,float X=0.0F,float Y=0.0F, string? fontKey=null);
        void DrawText(ReadOnlySpan<char> text,float X=0.0F,float Y=0.0F, string? fontKey=null);
        void DrawText(ReadOnlySpan<byte> text,float X=0.0F,float Y=0.0F, string? fontKey=null);
        void Clear(uint? color=null);
    }


}
