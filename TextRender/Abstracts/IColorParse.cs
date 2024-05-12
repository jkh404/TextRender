namespace TextRender.Abstracts
{
    
    public interface IColorParse
    {
        int Parse(string hexColorString);
        int From(byte r,byte g,byte b, byte a);
        int FromHsl(float h, float s, float l, byte a = byte.MaxValue);
        int FromHsv(float h, float s, float v, byte a = byte.MaxValue);
        string ToHexString(uint color);
        string ToHexString(byte a, byte r, byte g, byte b);

    }

    public interface IColorParse<TColor>: IColorParse where TColor:struct
    {
        TColor ToColor(uint color);
        TColor ToColor( byte r, byte g, byte b, byte a);
        TColor HexToColor(string hexColorString);
        TColor HslToColor(float h, float s, float l, byte a = byte.MaxValue);
        TColor HsvToColor(float h, float s, float v, byte a = byte.MaxValue);
    }
}
