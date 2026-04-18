using HeadlessTextBox.Compositing.Storage;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

/// <summary>
/// Flattened stored text sliced from whole buffer. 
/// Useful for tasks that expect continuous address, such as word wrapping and new line breaking.
/// <br/>
/// This is a fat struct. Remember to pass with `in`.
/// </summary>
public readonly ref struct SourceRef
{
    private int Offset { get; }
    public int Length { get; }
    
    private readonly FormatStorage _formatStorage;

    private readonly ReadOnlySpan<char> _textCache;
    
    
    public SourceRef(
        int offset, 
        int length, 
        TextStorage text,
        FormatStorage format)
    {
        Offset = offset;
        Length = length;
        _formatStorage = format;
        _textCache = StitchPieceSlice(text, offset, length, new char[length]);
    }

    private SourceRef(
        int offset,
        int length,
        ReadOnlySpan<char> textCache,
        FormatStorage formatStorage)
    {
        Offset = offset;
        Length = length;
        _textCache = textCache;
        _formatStorage = formatStorage;
    }


    public SourceSliceEnumerator GetEnumerator() => new(Offset, Length, _formatStorage, _textCache);

    
    public SourceRef this[Range range] => Slice(range);

    public SourceRef Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public SourceRef Slice(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(Length);
        return Slice(offset, length);
    }

    public SourceRef Slice(int offset, int length)
    {
        var newOffset = Offset + offset;
        var newTextCache = _textCache.Slice(newOffset, length);
        return new SourceRef(newOffset, length, newTextCache, _formatStorage);
    }

    
    public ReadOnlySpan<char> GetTextSpan() => _textCache;
    

    private static Span<char> StitchPieceSlice(
        TextStorage storage,
        int start, 
        int length, 
        Span<char> span)
    {
        foreach (var pieceSpan in storage.SlicedEnumerate(start, length)) 
            pieceSpan.CopyTo(span);
        return span;
    }
}


public ref struct SourceSliceEnumerator
{
    private int _index;
    
    private readonly ReadOnlySpan<char> _textCache;
    
    private FormatStorage.FormatEnumerator _formatEnumerator;
    
    
    public TextElement Current => GetCurrent();
    
    
    public SourceSliceEnumerator(
        int offset,
        int length,
        FormatStorage formatStorage, 
        ReadOnlyMemory<char> textCache
    ) : this(offset, length, formatStorage, textCache.Span)
    { }
    
    public SourceSliceEnumerator(
        int offset,
        int length,
        FormatStorage formatStorage, 
        ReadOnlySpan<char> textCache)
    {
        AssertException.ThrowIf(textCache.Length != length);
        
        _index = 0;
        _textCache = textCache;
        _formatEnumerator = formatStorage.SlicedEnumerate(offset, length);
    }


    private TextElement GetCurrent()
    {
        var c = _textCache[_index];
        var f =  _formatEnumerator.Current;
        return new TextElement(c, f);
    }
    
    public bool MoveNext()
    {
        _index++;
        _formatEnumerator.MoveNext();
        return _index <= _textCache.Length;
    }


    public void Dispose()
    {
        _formatEnumerator.Dispose();
    }
}