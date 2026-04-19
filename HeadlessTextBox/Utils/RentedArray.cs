using System.Buffers;

namespace HeadlessTextBox.Utils;

public struct RentedArray<T> : IDisposable
{
    private int _count;
    private readonly T[] _buffer;
    
    
    public T LastItem => _buffer[_count - 1];
    
    
    public RentedArray(int length)
    {
        _buffer = ArrayPool<T>.Shared.Rent(length);
        _count = 0;
    }
    
    
    public ReadOnlySpan<T> AsSpan() => _buffer.AsSpan();

    
    public void Add(T value)
    {
        _buffer[_count] = value;
        _count++;
    }


    public T this[Index index]
    {
        get => _buffer[index];
        set => _buffer[index] = value;
    }
    public ReadOnlySpan<T> this[Range range] => _buffer.AsSpan()[range];
    
    
    public void Dispose()
    {
        if (_buffer != null) 
            ArrayPool<T>.Shared.Return(_buffer, clearArray: true);
    }
}