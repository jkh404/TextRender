namespace TextRender.Abstracts
{
    public interface IBox
    {
        public BoxInfo? ParentBox { get; }
        public BoxInfo Box { get; }
    }
}
