using System;
using TextRender.Command;

namespace TextRender.Abstracts
{
    public interface IFrame : IDisposable
    {
        uint BackgroundColor{ get; set; }
        int Width { get;  }
        int Height { get; }
        int BytesSize { get; }
        Span<byte> Bytes { get; }
        bool Resize(int width,int height);
        void Alloc();
        void Free();
    }
}
