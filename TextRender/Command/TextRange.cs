using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace TextRender.Command
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public  struct TextRange : IEquatable<TextRange>
    {
        [FieldOffset(0)] public long Start;
        [FieldOffset(8)] public long End;

        public TextRange(long start, long end)
        {
            Start=start;
            End=end;
        }

        public long Length => End-Start;

        public static implicit operator TextRange(Range r) => new TextRange() { Start = r.Start.Value, End = r.End.Value };
        public static implicit operator Range(TextRange r) => new Range((int)r.Start,(int)r.End);

        public void SetStartAndEnd(long start, long end)
        {
            Start = start;
            End = end;
        }
        public Range ToRange()
        {
            return new Range((int)Start, (int)End);
        }

        public bool Equals(TextRange other)
        {
            return this.Start==other.Start && this.End==other.End;
        }
        public override string ToString()
        {
            return $"{Start}..{End}";
        }
        //public Range AsRange()
        //{
        //    return Unsafe.As<TextRange, Range>(ref this);
        //}
    }
}
