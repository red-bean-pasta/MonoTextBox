using HeadlessTextBox.Formatting;
using HeadlessTextBox.Storage;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

public class SourceBuffer
{
    public TextStorage Storage { get; }
    public FormatTree FormatTree { get; }

    
    public int Length => Storage.PieceTree.Length;
    
    
    public SourceBuffer()
    : this(string.Empty, new FormatTree())
    { }
    
    public SourceBuffer(string text, FormatTree format)
    {
        var originalBuffer = text;
        var addedBuffer = new AddBuffer();
        var piece = new Piece(0, originalBuffer.Length, Piece.SourceType.Original);
        var pieceTree = new PieceTree(piece, null, null);
        Storage = new TextStorage(originalBuffer, addedBuffer, pieceTree);
        
        FormatTree = format;
    }


    public TextElement this[Index index] => GetValueAt(index);

    public SourceSlice this[Range range] => Slice(range);

    private SourceSlice Slice(Range range)
    {
        var (start, end) = range.GetOffsetAndLength(Length);
        return Slice(start, end);
    }
    
    public SourceSlice Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public SourceSlice Slice(int start, int length)
    {
        return new SourceSlice(start, length, this);
    }

    private TextElement GetValueAt(Index index)
    {
        var character = Storage.GetValueAt(index);
        var format= FormatTree.Find(index).Value.Format;
        return new TextElement(character, format);
    }
}