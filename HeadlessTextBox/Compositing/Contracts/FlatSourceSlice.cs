using HeadlessTextBox.Formatting;
using HeadlessTextBox.TextStoring;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

/// <summary>
/// Flattened stored text sliced from whole buffer. 
/// Useful for tasks that expect continuous address, such as word wrapping and new line breaking.
/// <br/>
/// This is a fat struct. Remember to pass with `in`.
/// </summary>
public readonly ref struct FlatSourceSlice
{
    private int Offset { get; }
    public int Length { get; }
    
    private readonly FormatTree _formatTree;

    private readonly ReadOnlySpan<char> _textCache;
    
    
    public FlatSourceSlice(
        int offset, 
        int length, 
        SourceBuffer source)
    {
        Offset = offset;
        Length = length;
        
        _formatTree = source.Format;
        
        var (firstPiece, relativeIndex) = source.Text.TextTree.Locate(offset);
        _textCache = firstPiece.Length - relativeIndex > length 
            ? SliceContinuousPiece(firstPiece.Source, firstPiece.Start + relativeIndex, length, source)
            : StitchPieceSlice(source, offset, length, new char[length]);
    }

    private FlatSourceSlice(
        int offset,
        int length,
        FormatTree formatTree,
        ReadOnlySpan<char> textCache)
    {
        Offset = offset;
        Length = length;
        _formatTree = formatTree;
        _textCache = textCache;
    }


    public SourceSliceEnumerator GetEnumerator() => new(_formatTree, _textCache);

    
    public FlatSourceSlice this[Range range] => Slice(range);

    public FlatSourceSlice Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public FlatSourceSlice Slice(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(Length);
        return Slice(offset, length);
    }

    public FlatSourceSlice Slice(int offset, int length)
    {
        var newOffset = Offset + offset;
        var newTextCache = _textCache.Slice(newOffset, length);
        return new FlatSourceSlice(newOffset, length, _formatTree, newTextCache);
    }

    
    public ReadOnlySpan<char> GetTextSpan() => _textCache;
    

    private static ReadOnlySpan<char> SliceContinuousPiece(
        TextPiece.SourceType sourceType,
        int sourceStart, 
        int length,
        SourceBuffer source)
    {
        var storage = source.Text;
        return sourceType == TextPiece.SourceType.Original
            ? storage.Original.AsSpan(sourceStart, length)
            : storage.Added.GetSpan(sourceStart, length);
    }

    private static Span<char> StitchPieceSlice(
        SourceBuffer source,
        int sourceStart, 
        int length, 
        Span<char> span)
    {
        foreach (var pieceSpan in source.Text.SlicedEnumerate(sourceStart, length)) 
            pieceSpan.CopyTo(span);
        return span;
    }
}


public ref struct SourceSliceEnumerator
{
    private int _index;
    
    private readonly FormatTree _formatTree;
    private readonly ReadOnlySpan<char> _textCache;

    private int _currentFormatRoom;
    private FormatPiece _currentFormat;
    
    
    public TextElement Current => GetCurrent();
    
    
    public SourceSliceEnumerator(FormatTree formatTree, ReadOnlyMemory<char> textCache)
        : this(formatTree, textCache.Span)
    { }
    
    public SourceSliceEnumerator(FormatTree formatTree, ReadOnlySpan<char> textCache)
    {
        _index = 0;

        _currentFormatRoom = 0;
        _currentFormat = default;
        
        _textCache = textCache;
        _formatTree = formatTree;
    }


    private TextElement GetCurrent()
    {
        var c = _textCache[_index];

        RefreshCurrentFormat();
        var f =  _currentFormat.Format;
        _currentFormatRoom--;
        
        return new TextElement(c, f);
    }
    
    public bool MoveNext()
    {
        _index++;
        return _index <= _textCache.Length;
    }


    private void RefreshCurrentFormat()
    {
        if (_currentFormatRoom > 0)
            return;

        var (format, relativeIndex) = _formatTree.Locate(_index);
        _currentFormat = format;
        _currentFormatRoom = format.Length - relativeIndex;
    }
}