using System.Runtime.InteropServices;
namespace TextRender
{
    [StructLayout(LayoutKind.Explicit,Size =16)]
    public struct Margin
    {
        [FieldOffset(0)]public float Top;
        [FieldOffset(4)] public float Bottom;
        [FieldOffset(8)] public float Left;
        [FieldOffset(12)] public float Right;

        public Margin(float top, float bottom, float left, float right)
        {
            if (top<0 || bottom<0 || left<0 || right<0) throw new System.ArgumentException("margin must be greater than 0");
            if(bottom-top<0) throw new System.ArgumentException("bottom must be greater than top");
            if(right-left<0) throw new System.ArgumentException("right must be greater than left");
            Top=top;
            Bottom=bottom;
            Left=left;
            Right=right;
        }
    }
}
