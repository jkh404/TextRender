using System;
using System.Buffers;

namespace TextRender.Handles
{
    public class BitList:IDisposable
    {

        private byte[] _array;
        private int _arrayLength=100;
        public int Capacity => _arrayLength*8;

        private long LastIndex = 0;
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

        public bool this[long index]
        {
            get
            {
                if(index<0 || index>Capacity)throw new ArgumentOutOfRangeException(nameof(index));
                int i = (int)(index/8);
                int offset = (int)(index%8);
                return (_array[i] & (1<<offset))!=0;
            }
            set
            {
                if (index<0 || index>Capacity) throw new ArgumentOutOfRangeException(nameof(index));
                int i = (int)(index/8);
                int offset = (int)(index%8);
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
}
