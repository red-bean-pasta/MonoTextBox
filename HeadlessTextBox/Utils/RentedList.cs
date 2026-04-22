using System.Buffers;
using JetBrains.Annotations;

namespace HeadlessTextBox.Utils;

[MustDisposeResource]
public struct RentedList<T> : IDisposable
{
    public int Count { get; private set; }
    private T[] _buffer;
    
    
    public T LastItem => _buffer[Count - 1];
    
    
    public RentedList(int capacity)
    {
        _buffer = ArrayPool<T>.Shared.Rent(capacity);
        Count = 0;
    }
    
    
    public ReadOnlySpan<T> AsSpan() => _buffer.AsSpan();

    
    public void Add(T value)
    {
        _buffer[Count] = value;
        Count++;

        if (Count >= _buffer.Length)
            ExpandAndCopy();
    }

    private void ExpandAndCopy()
    {
        var expanded = ArrayPool<T>.Shared.Rent(Count * 2);
        Array.Copy(_buffer, 0, expanded, 0, Count);
        
        Dispose();
        _buffer = expanded;
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