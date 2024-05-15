namespace TextRender.Abstracts
{
    public interface ITextDocument : IBox
    {
        public int CharCount { get; }
        public int LineCount { get; }
        public int PageCount { get; }

        public BoxInfo Box { get; }
        public BoxInfo? ParentBox { get; }
    }
}
