using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextRender.Handles.Abstracts;

namespace TextRender.Handles
{
    public class BitList:IDisposable
    {

        private byte[] _array;
        private int _arrayLength=100;
        public int Capacity => _arrayLength*8;

        private int LastIndex = 0;
        public BitList()
        {
            _array=ArrayPool<byte>.Shared.Rent(_arrayLength);
            Array.Clear(_array,0, _arrayLength);
        }
        public BitList(int length)
        {

            _arrayLength=Convert.ToInt32(Math.Ceiling(length/8.0D));
            _array =ArrayPool<byte>.Shared.Rent(_arrayLength);
            Array.Clear(_array, 0, _arrayLength);
           
        }
        public bool this[int index]
        {
            get
            {
                if(index<0 || index>Capacity)throw new ArgumentOutOfRangeException(nameof(index));
                var i = index/8;
                var offset= index%8;
                return (_array[i] & (1<<offset))!=0;
            }
            set
            {
                if (index<0 || index>Capacity) throw new ArgumentOutOfRangeException(nameof(index));
                var i = index/8;
                var offset= index%8;
                if (value)
                {
                    _array[i]|=(byte)(1<<offset);
                    if (LastIndex<index) LastIndex=index;
                }
                else
                {
                    _array[i]&=(byte)~(1<<offset);
                }

            }
        }
        public void Dispose()
        {
            if(_array!=null) ArrayPool<byte>.Shared.Return(_array);
        }
        public void FixUp()
        {
            
            var newArrLength=Convert.ToInt32(Math.Ceiling(LastIndex/8.0D));
            if (newArrLength>0 && newArrLength<_arrayLength)
            {

                var _temp = ArrayPool<byte>.Shared.Rent(newArrLength);
                Array.Copy(_array, _temp, newArrLength);
                ArrayPool<byte>.Shared.Return(_array);
                _array=_temp;
                _arrayLength=newArrLength;
            }

        }
    }
    public class TextProviderHandle : ITextProviderHandle,IDisposable
    {
        private readonly Task<CountInfo> _taskCountChar;
        private readonly StreamWapper _stream;
        private readonly Encoding _encoding;
        private readonly long _byteCount;
        private long _charCount;
        private CountInfo _countInfo;

        public TextProviderHandle(Stream stream, Encoding? encoding=null)
        {
            
            _stream =new StreamWapper(stream);
            _byteCount=_stream.Length;
            _encoding =encoding??Encoding.Unicode;
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
            BitList bitList = new BitList((int)Math.Min(byteCount, SingleReadMax));
            byte[] newLineBytes= _encoding.GetBytes("\n");
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
                    throw new NotImplementedException("统计行数，还未实现");
                    var buffer = ArrayPool<byte>.Shared.Rent(ReadOneMB);
                    var count = _stream.Read(buffer, startRead, ReadOneMB);
                    countChar+=_encoding.GetCharCount(buffer.AsSpan(0, count));
                    startReadTemp+=count;
                    var isNewLine=buffer.AsSpan(0, count).IndexOf(newLineBytes);
                    
                    ArrayPool<byte>.Shared.Return(buffer,true);

                } while (readCount>startReadTemp);
                startRead+=startReadTemp;
            } while (byteCount>0);
            countInfo.CharCount=countChar;
            _countInfo = countInfo;
            bitList.FixUp();
            _countInfo.LineIndexs=bitList;
            return countInfo;
        }

        public long ByteCount => _byteCount;
        public bool CountRunning => !_taskCountChar.IsCompleted;
        public long Count => _taskCountChar.Result.CharCount;
        public long LineCount => _taskCountChar.Result.LineCount;
        public IEnumerable<Range> Lines => throw new NotImplementedException();

        public Encoding Encoding => _encoding;

        public bool IsReadOnly => throw new NotImplementedException();

        public bool IsEmpty => throw new NotImplementedException();

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
            throw new NotImplementedException();
        }

        public ReadOnlySpan<char> Slice(long start)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _stream?.Dispose(); 
        }
        private class  CountInfo
        {
            public long CharCount { get; set;}
            public long LineCount { get; set; }
            public BitList LineIndexs { get; set; }

            //public CountInfo(long charCount, long lenCount, BitArray lineIndex)
            //{
            //    CharCount=charCount;
            //    LenCount=lenCount;
            //    LineIndex=lineIndex;
            //}
        }
    }
}
