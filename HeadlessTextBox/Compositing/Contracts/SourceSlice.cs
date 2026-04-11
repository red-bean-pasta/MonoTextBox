using HeadlessTextBox.Formatting;
using HeadlessTextBox.Storage;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

// Fat struct. Should always be passed with `in`
public readonly ref struct SourceSlice
{
    private int Offset { get; }
    public int Length { get; }
    
    private readonly FormatTree _formatTree;

    private readonly ReadOnlySpan<char> _textCache;
    
    
    public SourceSlice(
        int offset, 
        int length, 
        SourceBuffer source)
    {
        Offset = offset;
        Length = length;
        
        _formatTree = source.FormatTree;
        
        var (firstPiece, relativeIndex) = source.PieceTree.Find(offset);
        _textCache = firstPiece.Length - relativeIndex > length 
            ? SliceContinuousPiece(firstPiece.Source, firstPiece.Start + relativeIndex, length, source)
            : StitchPieceSlice(source, offset, length, new char[length]);
    }

    private SourceSlice(
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


    // Interface
    public SourceSliceEnumerator GetEnumerator() => new SourceSliceEnumerator(_formatTree, _textCache);

    
    public TextElement this[Index index] => throw new NotImplementedException();

    public SourceSlice this[Range range] => throw new NotImplementedException();

    public SourceSlice Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public SourceSlice Slice(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(Length);
        return Slice(offset, length);
    }

    public SourceSlice Slice(int offset, int length)
    {
        var newOffset = Offset + offset;
        var newTextCache = _textCache.Slice(newOffset, length);
        return new SourceSlice(newOffset, length, _formatTree, newTextCache);
    }

    
    public ReadOnlySpan<char> GetTextSpan() => _textCache;
    
    
    private TextElement GetValueAt(int index)
    {
        var c = _textCache[index];
        var f = _formatTree.Find(index).Value.Format;
        return new TextElement(c, f);
    }
    

    private static ReadOnlySpan<char> SliceContinuousPiece(
        Piece.SourceType sourceType,
        int sourceStart, 
        int length,
        SourceBuffer source)
    {
        return sourceType == Piece.SourceType.Original
            ? source.Original.AsSpan(sourceStart, length)
            : source.Added.GetSpan(sourceStart, length);
    }

    private static Span<char> StitchPieceSlice(
        SourceBuffer source,
        int sourceStart, 
        int length, 
        Span<char> span)
    {
        var i = sourceStart;
        var l = length;
        while (true)
        {
            var (piece, relativeIndex) = source.PieceTree.Find(i);
            var pieceSpace = piece.Length - relativeIndex;
            
            var spanStart = piece.Start + relativeIndex;
            var spanLength = Math.Min(l, pieceSpace);
            CopyPieceSpan(source, piece.Source, spanStart, spanLength, span);
            
            i += spanLength;
            l -= spanLength; 
            
            if (l <= 0) break;
        }

        return span;
    }

    private static void CopyPieceSpan(
        SourceBuffer source,
        Piece.SourceType sourceType, 
        int sourceStart, 
        int length, 
        Span<char> span)
    {
        if (sourceType == Piece.SourceType.Add)
            source.Added.GetSpan(sourceStart, length).CopyTo(span);
        else
            source.Original.AsSpan(sourceStart, length).CopyTo(span);
    }
}


public ref struct SourceSliceEnumerator
{
    private int _index;
    
    private readonly FormatTree _formatTree;
    private readonly ReadOnlySpan<char> _textCache;

    private int _currentFormatRoom;
    private FormatBranch _currentFormat;
    
    
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

        var (format, relativeIndex) = _formatTree.Find(_index);
        _currentFormat = format;
        _currentFormatRoom = format.Length - relativeIndex;
    }
}