using System.Diagnostics;
using MonoTextBox.Formatting;
using MonoTextBox.Positioning.SpanEnumerating;
using MonoTextBox.Utils;

namespace MonoTextBox;

public readonly ref struct Buffer
{
    public ReadOnlySpan<char> Text { get; }
    public ReadOnlySpan<Format> Format { get; }
    
    
    public int Length => Text.Length;
    
    
    public static Buffer Empty => default;
    
    public Buffer(
        ReadOnlySpan<char> text, 
        ReadOnlySpan<Format> format)
    {
        Debug.Assert(text.Length == format.Length);
        
        Text = text;
        Format = format;
    }
    
    
    public BufferEnumerator GetEnumerator() => new(this);
    

    public (char Char, Format Format) this[Index index] => (Text[index], Format[index]);
    
    public Buffer this[System.Range range] => this.Slice(range);

    
    public Buffer Slice(int start, int length) => new(Text.Slice(start, length), Format.Slice(start, length));

    private Buffer Slice(System.Range range) => new(Text[range], Format[range]);
    
    public Buffer Slice(Slice slice) => new(Text.Slice(slice), Format.Slice(slice));
}


public ref struct BufferEnumerator
{
    private int _index;
    
    private readonly Buffer _buffer;
    
    
    public (char Char, Format format) Current => _buffer[_index];
    

    public BufferEnumerator(Buffer buffer)
    {
        _buffer = buffer;
        _index = -1;
    }

    
    public bool MoveNext() 
    {
        _index++;
        return _index < _buffer.Length;
    }
}


public static class BufferExtensions
{
    public static OffsetEnumerator EnumerateNewLines(this Buffer buffer)
    {
        return buffer.Text.EnumerateNewLines();
    }
}