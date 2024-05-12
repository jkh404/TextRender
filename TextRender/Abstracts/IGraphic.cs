namespace TextRender.Abstracts
{
    public interface IGraphic: IFrame,IDraw, IColorParse
    {

    }
    public interface IGraphic<TColor> : IGraphic, IColorParse<TColor> where TColor:struct
    {

    }
}
