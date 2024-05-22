using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextRender.Command;
using TextRender.Handles.Abstracts;

namespace TextRender.Handles
{
    public class TextProviderHandle : ITextProviderHandle
    {
        private readonly Task<CountInfo> _taskCountChar;
        private readonly StreamWapper _stream;
        private readonly Encoding _encoding;
        private readonly long _byteCount;
        private long _charCount;
        private CountInfo _countInfo;
        private bool _isReadOnly;
        private bool _isCountLine;
        private bool _isCache;
        private int _newLineByteCount;
        private readonly string _newLine="\n";
        public TextProviderHandle(Stream stream, Encoding? encoding=null)
        {
            
            _stream =new StreamWapper(stream);
            _byteCount=_stream.Length;
            _encoding =encoding??Encoding.Unicode;
            _newLineByteCount=_encoding.GetByteCount(_newLine);
            _isCountLine =false;
            _taskCountChar=Task.Run(CountTask);
        }
        public ReadOnlySpan<char> this[Range range]
        {
            get
            {
                return this.Slice(range.Start.Value, range.End.Value);
            }
        }
        private CountInfo CountTask()
        {
            CountInfo countInfo = new CountInfo();
            const int OneMB = 1024*1024;
            int ReadOneMB = _encoding.GetMaxByteCount(1)*OneMB;
            const int SingleReadMax = int.MaxValue-102400;
            long countChar = 0;
            long byteCount = ByteCount;
            long startRead = 0;
            List<long> newLineIndex=null;
            if(IsCountLine) newLineIndex=new List<long>(Convert.ToInt32(Math.Min(byteCount*0.01, SingleReadMax*0.01)));
            List<long> newLineIndexTemp = new List<long>();
            long lineCount = 0;
            byte[] newLineBytes= _encoding.GetBytes(_newLine);
            do
            {
                int readCount = 0;
                if (byteCount>SingleReadMax)
                {
                    byteCount-=SingleReadMax;
                    readCount=SingleReadMax;
                }
                else
                {
                    readCount=(int)byteCount;
                    byteCount-=byteCount;
                }
                long startReadTemp = 0;
                do
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(ReadOneMB);
                    var count = _stream.Read(buffer, startRead, ReadOneMB);
                    countChar+=_encoding.GetCharCount(buffer.AsSpan(0, count));
                    newLineIndexTemp.Clear();
                    lineCount+=(buffer.AsSpan(0, count).IndexOfALL(newLineBytes, newLineIndexTemp, startReadTemp));
                    startReadTemp+=count;
                    ArrayPool<byte>.Shared.Return(buffer,true);
                    if (IsCountLine) newLineIndex?.AddRange(newLineIndexTemp);

                } while (readCount>startReadTemp);
                startRead+=startReadTemp;
            } while (byteCount>0);
            countInfo.CharCount=countChar;
            _countInfo = countInfo;
            _countInfo.LineCount=lineCount;
            _countInfo.LineIndexs=newLineIndex;
            newLineIndexTemp.Clear();
            newLineIndexTemp.Capacity=0;
            return countInfo;
        }

        public long ByteCount => _byteCount;
        public bool CountRunning => !_taskCountChar.IsCompleted;
        public long Count => _taskCountChar.Result.CharCount;
        public long LineCount => _taskCountChar.Result.LineCount;
        public bool IsCountLine => _isCountLine;
        public bool IsCache => _isCache;
        public IEnumerable<Range> Lines
        {
            get
            {
                if (IsCountLine)
                {

                    long start = 0;
                    foreach (var index in _taskCountChar!.Result!.LineIndexs!)
                    {
                        yield return new Range((int)start, (int)index);
                        start=index+_newLineByteCount;
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public Encoding Encoding => _encoding;

        public bool IsReadOnly => _isReadOnly;


        public string NewLine => _newLine;

        public void CopyTo(ReadOnlySpan<char> chars, long offset = 0)
        {
            throw new NotImplementedException();
        }


        public void InsertChar(char c, long offset = 0)
        {
            throw new NotImplementedException();
        }

        public void InsertText(ReadOnlySpan<char> chars, long offset = 0)
        {
            throw new NotImplementedException();
        }

        public void InsertText(ReadOnlySpan<byte> text, long offset = 0)
        {
            throw new NotImplementedException();
        }

        public void InsertText(string text, long offset = 0)
        {
            throw new NotImplementedException();
        }

        public ReadOnlySpan<char> Slice(long start, int length)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            _stream.Read(buffer, start, length);
            var result = _encoding.GetChars(buffer, 0, length);
            ArrayPool<byte>.Shared.Return(buffer);
            return result;
            //return "123456789";
        }

        public ReadOnlySpan<char> Slice(long start)
        {

            var end = ByteCount;
            int length = (int)(end-start);

            var buffer = ArrayPool<byte>.Shared.Rent(length);
            _stream.Read(buffer, start, length);
            var result = _encoding.GetChars(buffer, 0, length);
            ArrayPool<byte>.Shared.Return(buffer);
            return result;
        }
        public ReadOnlySpan<char> Slice(Range range)
        {
            var start=range.Start.Value;
            var end=range.End.Value;
            var length=end-start;

            var buffer = ArrayPool<byte>.Shared.Rent(length);
            _stream.Read(buffer, start, length);
            var result = _encoding.GetChars(buffer, 0, length);
            ArrayPool<byte>.Shared.Return(buffer);
            return result;

        }

        public void Dispose()
        {
            _stream?.Dispose(); 
        }

        public ReadOnlySpan<byte> SliceByte(long start, int length)
        {

            byte[] buffer = new byte[length];
            _stream.Read(buffer, start, length);
            return buffer;
        }

        public ReadOnlySpan<byte> SliceByte(Range range)
        {
            int start = range.Start.Value;
            int length = range.End.Value-start;
            byte[] buffer = new byte[length];
            _stream.Read(buffer, start, length);
            return buffer;


        }
        public ReadOnlySpan<byte> SliceByte(long start)
        {
            int length = (int)(ByteCount-start);
            byte[] buffer=new byte[length];
            _stream.Read(buffer, start, length);
            return buffer;
        }

        public long IndexOf(string text)
        {
            const int OneMB = 1024*1024;
            int ReadOneMB = _encoding.GetMaxByteCount(1)*OneMB;
            var _textByte=_encoding.GetBytes(text);
            var byteCount = ByteCount;
            long startRead = 0;
            var SingleReadMax = int.MaxValue-102400;
            do
            {
                int readCount = 0;
                if (byteCount>SingleReadMax)
                {
                    byteCount-=SingleReadMax;
                    readCount=SingleReadMax;
                }
                else
                {
                    readCount=(int)byteCount;
                    byteCount-=byteCount;
                }
                long startReadTemp = 0;
                do
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(ReadOneMB);
                    var count = _stream.Read(buffer, startRead, ReadOneMB);
                    var reuslt=buffer.AsSpan(0,count).IndexOf(_textByte);

                    ArrayPool<byte>.Shared.Return(buffer, true);
                    if (reuslt>=0) return startRead+reuslt;

                } while (readCount>startReadTemp);
                startRead+=startReadTemp;
            } while (byteCount>0);
            return -1;
        }
        public long IndexOf(ReadOnlySpan<char> text)
        {
            throw new NotImplementedException();
        }
        public long IndexOf(ReadOnlySpan<char> text, long start)
        {
            throw new NotImplementedException();
        }

        //public long IndexOf(ReadOnlySpan<char> text, long start, long length)
        //{
        //    throw new NotImplementedException();
        //}
        public long IndexOf(ReadOnlySpan<char> text, long start, long length)
        {
            //return 100;
            const int OneMB = 1024*1024;
            int ReadOneMB = _encoding.GetMaxByteCount(1)*OneMB;
            
            var byteCount = length;
            long startRead = start;

            byte[] _textByte = new byte[text.Length*2];
            var _textByteLen = _encoding.GetBytes(text, _textByte);
            var SingleReadMax = int.MaxValue-102400;
            do
            {
                int readCount = 0;
                if (byteCount>SingleReadMax)
                {
                    byteCount-=SingleReadMax;
                    readCount=SingleReadMax;
                }
                else
                {
                    readCount=(int)byteCount;
                    byteCount-=byteCount;
                }
                long startReadTemp = 0;
                do
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(ReadOneMB);
                    var count = _stream.Read(buffer, startRead, ReadOneMB);
                    var reuslt = buffer.AsSpan(0, count).IndexOf(_textByte.AsSpan(0, _textByteLen));

                    ArrayPool<byte>.Shared.Return(buffer, true);
                    if (reuslt>=0) return startRead+reuslt;

                } while (readCount>startReadTemp);
                startRead+=startReadTemp;
            } while (byteCount>0);
            return -1;
        }
        private class  CountInfo
        {
            public long CharCount { get; set;}
            public long LineCount { get; set; }
            public List<long>? LineIndexs { get; set; }

        }
    }
}
