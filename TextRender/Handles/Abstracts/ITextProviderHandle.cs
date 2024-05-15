using System;
using System.Collections.Generic;
using System.Text;

namespace TextRender.Handles.Abstracts
{
    public interface ITextProviderHandle
    {
        public ReadOnlySpan<char> Slice(long start, int length);
        public ReadOnlySpan<char> Slice(long start);
        public long ByteCount { get; }
        public long Count { get; }

        /// <summary>
        /// 换行符为单位
        /// </summary>
        public long LineCount { get; }
        public bool CountRunning { get; }
        public IEnumerable<Range> Lines { get; }
        public Encoding Encoding { get; }
        public bool IsReadOnly { get; }

        public bool IsEmpty { get; }
        public void InsertText(ReadOnlySpan<char> chars, long offset = 0);

        public void InsertText(ReadOnlySpan<byte> text, long offset = 0);
        public void InsertText(string text, long offset = 0);
        public void InsertChar(char c, long offset = 0);
        public void CopyTo(ReadOnlySpan<char> chars, long offset = 0);

    }
}
