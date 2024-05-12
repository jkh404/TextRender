using TextRender.Abstracts;

namespace TextRender.SkiaSharpRender
{
    public static  class SkiaSharpFrameInstance
    {
        public static IGraphic CreateSkiaSharpFrame(this GraphicInstances instances)
        {
            return new SkiaSharpFrame();
        }
    }
}
