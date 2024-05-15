using System.Runtime.InteropServices;
using TextRender.Command;

namespace TextRender
{
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public readonly struct BoxInfo
    {

        [FieldOffset(0)] public readonly int Width;
        [FieldOffset(4)] public readonly int Height;
        [FieldOffset(8)] public readonly BoxSpacing Margin;
        [FieldOffset(24)] public readonly BoxSpacing Padding;
    }
}
