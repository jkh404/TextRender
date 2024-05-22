using System;
using System.Collections.Generic;
using System.Text;

namespace TextRender.Handles.Abstracts
{
    public interface ITextProviderHandle:IDisposable
    {
        public bool IsCountLine { get; }
        public bool IsReadOnly { get; }
        public bool IsCache { get; }
        public ReadOnlySpan<char> Slice(long start, int length);
        public ReadOnlySpan<char> Slice(long start);
        public ReadOnlySpan<char> Slice(Range range);
        public ReadOnlySpan<byte> SliceByte(long start, int length);
        public ReadOnlySpan<byte> SliceByte(long start);
        public ReadOnlySpan<byte> SliceByte(Range range);
        public long IndexOf(string text);
        public long IndexOf(ReadOnlySpan<char> text);
        public long IndexOf(ReadOnlySpan<char> text,long start);
        public long IndexOf(ReadOnlySpan<char> text,long start,long length);
        public long ByteCount { get; }
        public long Count { get; }

        /// <summary>
        /// 换行符为单位
        /// </summary>
        public string NewLine { get; }
        public long LineCount { get; }
        public bool CountRunning { get; }

        public IEnumerable<Range> Lines { get; }
        public Encoding Encoding { get; }


        public void InsertText(ReadOnlySpan<char> chars, long offset = 0);

        public void InsertText(ReadOnlySpan<byte> text, long offset = 0);
        public void InsertText(string text, long offset = 0);
        public void InsertChar(char c, long offset = 0);
        public void CopyTo(ReadOnlySpan<char> chars, long offset = 0);


    }
}
