using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace TextRender.Command
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct TextRange
    {
        [FieldOffset(0)] public int Start;
        [FieldOffset(4)] public int End;

        public static implicit operator TextRange(Range r) => new TextRange() { Start = r.Start.Value, End = r.End.Value };

        public void SetStartAndEnd(int start, int end)
        {
            Start = start;
            End = end;
        }
        public Range AsRange()
        {
            return Unsafe.As<TextRange, Range>(ref this);
        }
    }
}
