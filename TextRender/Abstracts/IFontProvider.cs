using System;
using System.Collections.ObjectModel;
using System.IO;
using TextRender.Command;

namespace TextRender.Abstracts
{
    public interface IFontProvider<TFont,IPaint>: IFontProvider<TFont>
    {
        IPaint GetPaint(string fontKey);
    }
    public interface IFontProvider<TFont> : IFontProvider,IDisposable
    {
        TFont GetFont(string fontKey);
    }
    public interface IFontProvider
    {
        ReadOnlyCollection<string> FontKeys { get; }
        bool LoadFont(FontInfo fontInfo, string fontKey = null);
        bool LoadFont(Stream FontStream, FontInfo fontInfo, string fontKey = null);

        FontInfo GetFontInfo(string fontKey);

        void Resize(string fontKey,float fontSize);
        void ResizeALL(float fontSize);
        bool ContainsKey(string fontKey);
        bool ChangeFontInfo(string fontKey, FontInfo newFontInfo);
    }
}
