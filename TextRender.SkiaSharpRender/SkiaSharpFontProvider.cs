using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using SkiaSharp;
using TextRender.Abstracts;
using TextRender.Command;

namespace TextRender.SkiaSharpRender
{
    internal class SkiaSharpFontProvider : IFontProvider<SKFont, SKPaint>
    {
        private IList<string> _fontKeys;
        private IDictionary<string, FontInfo> _fontInfos;
        private IDictionary<string, SKFont> _fonts;
        private IDictionary<string, SKPaint> _paints;
        private readonly IColorParse<SKColor> _colorParse;

        private SkiaSharpFontProvider() { }
        public SkiaSharpFontProvider(IColorParse<SKColor> colorParse)
        {
            _fontKeys= new List<string>(128);
            _fontInfos= new ConcurrentDictionary<string, FontInfo>();
            _fonts= new ConcurrentDictionary<string, SKFont>();
            _paints= new ConcurrentDictionary<string, SKPaint>();
            _colorParse=colorParse;
        }

        public ReadOnlyCollection<string> FontKeys=> new ReadOnlyCollection<string>(_fontKeys);
        public SKFont GetFont(string fontKey)
        {
            if (_fonts?.TryGetValue(fontKey, out var  font)??false)
            {
                return font;
            }
            else
            {
                return null;
            }
        }
        public SKPaint GetPaint(string fontKey)
        {
            if (_paints?.TryGetValue(fontKey, out var paint)??false)
            {
                return paint;
            }
            else
            {
                return null;
            }
        }



        public FontInfo GetFontInfo(string fontKey)
        {
            if (_fontInfos?.TryGetValue(fontKey, out FontInfo fontInfo)??false)
            {
                return fontInfo;
            }
            else
            {
                return null;
            }

        }

        

        public bool LoadFont(FontInfo fontInfo, string fontKey = null)
        {
            if (fontInfo==null) throw new ArgumentNullException(nameof(fontInfo));
            if (string.IsNullOrWhiteSpace(fontInfo.FamilyName)) throw new ArgumentNullException(nameof(fontInfo.FamilyName));

            string fontKeyTemp = fontKey;
            if (string.IsNullOrWhiteSpace(fontKey)) fontKeyTemp=fontInfo.FamilyName;
            if (ContainsKey(fontKeyTemp)) fontKeyTemp=$"g_{fontInfo.GetHashCode()}";
            if (ContainsKey(fontKeyTemp))throw new ArgumentException(nameof(fontKey));

            SKFont font = new SKFont();
            SKPaint paint = new SKPaint(font);
            _fontKeys.Add(fontKeyTemp);
            _fontInfos.Add(fontKeyTemp, fontInfo);
            _fonts.Add(fontKeyTemp, font);
            _paints.Add(fontKeyTemp, paint);

            if (!ChangeFontInfo(fontKeyTemp, fontInfo)) throw new Exception();
            return true;
        }

        public bool LoadFont(Stream FontStream, FontInfo fontInfo, string fontKey = null)
        {
            if (fontInfo==null) throw new ArgumentNullException(nameof(fontInfo));
            

            string fontKeyTemp = fontKey;
            if (string.IsNullOrWhiteSpace(fontKey)) fontKeyTemp=fontInfo.FamilyName;
            if (ContainsKey(fontKeyTemp)) fontKeyTemp=$"g_{fontInfo.GetHashCode()}";
            if (ContainsKey(fontKeyTemp)) throw new ArgumentException(nameof(fontKey));

            SKFont font = new SKFont();
            SKPaint paint = new SKPaint(font);
            _fontKeys.Add(fontKey);
            _fontInfos.Add(fontKey, fontInfo);
            _fonts.Add(fontKey, font);
            _paints.Add(fontKey, paint);

            if (!ChangeFontInfo(fontKeyTemp, fontInfo)) throw new Exception();
            return true;
        }

        public void Resize(string fontKey, float fontSize)
        {
            var font = GetFont(fontKey);
            var paint = GetPaint(fontKey);
            font.Size=fontSize;
            //paint.TextSize=fontSize;
        }

        public bool ContainsKey(string fontKey)
        {
            return _fontKeys.Contains(fontKey);
        }

        public void Dispose()
        {
            if (_paints!=null)
            {
                foreach (var item in _paints.Values)
                {
                    item?.Dispose();
                }
                _paints.Clear();
                _paints=null;
            }
            if (_fonts!=null)
            {
                foreach (var item in _fonts.Values)
                {
                    item?.Dispose();
                }
                _fonts.Clear();
                _fonts=null;
            }
            _fontKeys.Clear();
            _fontInfos.Clear();
        }

        public void ResizeALL(float fontSize)
        {
            if (_fonts!=null)
            {
                foreach (var item in _fonts.Values)
                {
                    item.Size=fontSize;
                }
            }
            if (_paints!=null)
            {
                foreach (var item in _paints.Values)
                {
                    item.TextSize=fontSize;
                }
            }
            
        }

        public bool ChangeFontInfo(string fontKey, FontInfo newFontInfo)
        {
            if (newFontInfo==null) throw new ArgumentNullException(nameof(newFontInfo));
            if (!ContainsKey(fontKey)) return false;
            var font = GetFont(fontKey);
            var paint = GetPaint(fontKey);

            font?.Typeface?.Dispose();
            font?.Dispose();
            //paint?.Typeface?.Dispose();
            paint?.Dispose();

            font=new SKFont(SKTypeface.FromFamilyName(newFontInfo.FamilyName), newFontInfo.Size, newFontInfo.ScaleX, newFontInfo.SkewX);
            font.Embolden=newFontInfo.Embolden;
            paint=new SKPaint(font);
            paint.Color=_colorParse.ToColor(newFontInfo.Color);
            
            paint.TextAlign=(SKTextAlign)newFontInfo.Align;
            
            paint.IsAntialias=newFontInfo.IsAntialias;

            font.ForceAutoHinting=newFontInfo.IsAutohinted;
            font.LinearMetrics= newFontInfo.IsLinearText;
            paint.IsStroke=newFontInfo.IsStroke;
            paint.Style=SKPaintStyle.Fill;

            _fonts[fontKey]=font;
            _paints[fontKey]=paint;

            return true;
        }
    }
}
