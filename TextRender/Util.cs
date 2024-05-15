using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TextRender
{
    public static class Util
    {
        /// <summary>
        /// 强制修改结构体的值
        /// </summary>
        /// <typeparam name="TStruct"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="thisStruct"></param>
        /// <param name="value"></param>
        /// <param name="offsetIndex"></param>
        public static unsafe void ForceChanges<TStruct, TValue>(this ref TStruct thisStruct, TValue value,int offsetByte=0)
            where TStruct:struct  
            where TValue : struct
        {
            var thisSize = Unsafe.SizeOf<TStruct>();
            var valueSize = Unsafe.SizeOf<TValue>();
            if (offsetByte+valueSize<=thisSize)
            {
                var thisBytes = (byte*)Unsafe.AsPointer(ref thisStruct);
                var valueBytes = (byte*)Unsafe.AsPointer(ref value);
                if(thisBytes!=null && valueBytes!=null)
                {
                    Unsafe.CopyBlock(thisBytes+offsetByte, valueBytes, (uint)valueSize);
                }
                
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
        public static unsafe void ForceChanges<TStruct, TValue>(this ref TStruct thisStruct, ReadOnlySpan<TValue> values, int offsetByte = 0)
            where TStruct : struct
            where TValue : struct
        {
            var thisSize = Unsafe.SizeOf<TStruct>();
            var valueSize = Unsafe.SizeOf<TValue>()*values.Length;
            if (offsetByte+valueSize<=thisSize)
            {
                var thisBytes = (byte*)Unsafe.AsPointer(ref thisStruct);
                MemoryMarshal.AsBytes(values).CopyTo(new Span<byte>(thisBytes+offsetByte, valueSize));
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
