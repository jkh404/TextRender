using TextRender.Abstracts;

namespace TextRender
{
    public class TextDocument: ITextDocument
    {
        private TextFrame _parent;
        private BoxInfo _box;

        public int CharCount { get; private set; }
        public int LineCount { get; private set; }
        public int PageCount { get; private set; }

        public BoxInfo Box => _box;
        public BoxInfo? ParentBox=> _parent?.Box;   




    }
}
