using System;
using System.Threading;

namespace TextRender
{
    public struct ReadDataLock<T> : IDisposable
    {
        private bool _disposed;
        private readonly T[] _data;
        private readonly int _count;
        private readonly object _lockObj;

        public  ReadOnlySpan<T> Datas
        {
            get
            {
                if ( _disposed) throw new InvalidOperationException();
                else return _data.AsSpan(0, _count);
            }
        }

        internal  ReadDataLock(T[] data,int count,object lockObj)
        {

            _disposed=false;
            _data=data;
            _count=count;
            _lockObj=lockObj;
            
        }

        public void UnLock()
        {
            if (_disposed) throw new InvalidOperationException();
            if(_lockObj==null) throw new InvalidOperationException();
            Monitor.Exit(_lockObj);

        }
        public void Dispose()
        {
            UnLock();
            _disposed=true;
            
        }
        public unsafe static ReadDataLock<TData> Create<TData>(TData[] data,int dataLen,object lockObj)
        {
            if (lockObj==null) throw new InvalidOperationException();
            if(!Monitor.TryEnter(lockObj,2000)) throw new InvalidOperationException();
            return new ReadDataLock<TData>(data, dataLen, lockObj);
        }
    }
}
