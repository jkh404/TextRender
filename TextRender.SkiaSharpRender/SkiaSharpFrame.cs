using System;
using System.Runtime.InteropServices;
using System.Threading;
using SkiaSharp;
using TextRender.Abstracts;
using TextRender.Command;

namespace TextRender.SkiaSharpRender
{
    internal partial class SkiaSharpFrame : IGraphic<SKColor>
    {

        protected SKBitmap? _bitmap;
        protected SKCanvas? _canvas;
        private SKTextBlobBuilder _textBlobBuilder;
        protected SKImageInfo _imageInfo;
        private IntPtr _dataPtr;
        private object _lockResize=new object();
        private object _lockAlloc=new object();
        private IFontProvider<SKFont,SKPaint> _fontProvider;
        private uint _backgroundColor= Colors.Transparent;
        private string _currentFontKey;

        internal SkiaSharpFrame()
        {
            _imageInfo=new SKImageInfo(0, 0);
            _bitmap=new SKBitmap();
            _canvas=new SKCanvas(_bitmap);
        }
        
        public int Width
        {
            get
            {
                Monitor.Enter(_lockResize);
                var result = _imageInfo.Width;
                Monitor.Exit(_lockResize);
                return result;
            }
        }

        public int Height
        {
            get
            {
                Monitor.Enter(_lockResize);
                var result = _imageInfo.Height;
                Monitor.Exit(_lockResize);
                return result;
            }
        }
        public int BytesSize
        {
            get
            {
                Monitor.Enter(_lockResize);
                var result = _imageInfo.BytesSize;
                Monitor.Exit(_lockResize);
                return result;
            }
        }
        public unsafe Span<byte> Bytes
        {
            get
            {
                Monitor.Enter(_lockResize);
                var result=new Span<byte>((void*)_dataPtr, _dataPtr!=IntPtr.Zero ? _imageInfo.BytesSize : 0);
                Monitor.Exit(_lockResize);
                return result;
            }
        }

        public uint BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor=value;
        }

        public bool Resize(int width, int height)
        {
            Monitor.Enter(_lockResize);
            if (width<0 || height<0) return false;
            _imageInfo.Width=width;
            _imageInfo.Height=height;
            Monitor.Exit(_lockResize);
            Alloc();
            return true;
        }

        public void Alloc()
        {
            Monitor.Enter(_lockAlloc);
            _textBlobBuilder?.Dispose();
            _canvas?.Dispose();
            _bitmap?.Dispose();
            if (_dataPtr!=IntPtr.Zero) Marshal.FreeHGlobal(_dataPtr);
            _dataPtr = Marshal.AllocHGlobal(_imageInfo.BytesSize);
            _bitmap = new SKBitmap();
            _bitmap.InstallPixels(_imageInfo, _dataPtr);
            _canvas = new SKCanvas(_bitmap);
            _textBlobBuilder=new SKTextBlobBuilder();
            Monitor.Exit(_lockAlloc);
        }

        public void Free()
        {
            Monitor.Enter(_lockAlloc);
            _textBlobBuilder?.Dispose();
            _canvas?.Dispose();
            _bitmap?.Dispose();
            Monitor.Exit(_lockAlloc);
        }
        public void Dispose()
        {
            Free();
            _textBlobBuilder=null;
            _canvas=null;
            _bitmap=null;
        }




        //private SKColor GetColor(int color)
        //{

        //    SKColor sKColor = new SKColor((uint)color);
        //    return sKColor;
        //}


    }
}
