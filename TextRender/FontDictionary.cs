using System.Collections.Concurrent;
using SkiaSharp;
namespace TextRender
{
    public interface IFontAdd
    {
        void AddFromFamilyName(string fontKey, string familyName, float skewX = 0);
        void AddFromStream(string fontKey, Stream stream, float skewX = 0);
    }
    public interface IFontDictionary : IFontAdd, IDisposable
    {
        internal SKFont? this[string fontKey] { get; }
        internal SKPaint? GetPaint(string fontKey);
        public void Resize(float fontSize);
        public bool ContainsKey(string key);
    }
    internal sealed class DefaultFontDictionary : IFontDictionary
    {
        private readonly float fontSize;
        private IDictionary<string, SKFont> _fonts;
        private IDictionary<string, SKPaint> _paints;
        public DefaultFontDictionary(bool isSafe,float fontSize)
        {
            if(isSafe)
            {

                _fonts = new ConcurrentDictionary<string, SKFont>();
                _paints = new ConcurrentDictionary<string, SKPaint>();
            }
            else
            {
                _fonts = new Dictionary<string, SKFont>();
                _paints = new Dictionary<string, SKPaint>();
            }
            this.fontSize=fontSize;
        }
        public SKFont? this[string fontKey]
        {
            get
            {
                if (_fonts.TryGetValue(fontKey, out var font))
                {
                    return font;
                }
                return null;
            }
        }
        public  void AddFromFamilyName(string fontKey,string familyName,float skewX=0)
        {
            if(string.IsNullOrWhiteSpace(fontKey))throw new ArgumentException("fontKey", nameof(fontKey));
            SKFont font = new SKFont(SKTypeface.FromFamilyName(familyName), size:fontSize, skewX: skewX);
            _fonts.Add(fontKey, font);
            _paints.Add(fontKey, new SKPaint(font)
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                TextAlign = SKTextAlign.Left,
            });
        }
        public  void AddFromStream(string fontKey,Stream stream, float skewX = 0)
        {
            if (string.IsNullOrWhiteSpace(fontKey)) throw new ArgumentException("fontKey", nameof(fontKey));
            SKFont font = new SKFont(SKTypeface.FromStream(stream), size: fontSize, skewX: skewX);
            _fonts.Add(fontKey, font);
            _paints.Add(fontKey, new SKPaint(font)
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                TextAlign = SKTextAlign.Left,
            });
        }
        public void Dispose()
        {

            foreach (var item in _paints.Values)
            {
                item?.Dispose();
            }
            _paints.Clear();
            foreach (var item in _fonts.Values)
            {
                item?.Dispose();
            }
            _fonts.Clear();
        }

        public void Resize(float fontSize)
        {
            foreach (var item in _fonts.Values)
            {
                if(item!=null) item.Size=fontSize;
            }
            foreach (var item in _paints.Values)
            {
                if (item!=null) item.TextSize=fontSize;
            }
        }

        public SKPaint? GetPaint(string fontKey)
        {
            if (_paints.TryGetValue(fontKey, out var paint))
            {
                return paint;
            }
            return null;
        }

        public bool ContainsKey(string key)
        {
            return _fonts.ContainsKey(key);
        }
    }
}
