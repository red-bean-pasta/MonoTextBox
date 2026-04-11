using HeadlessTextBox.Formatting;
using HeadlessTextBox.Storage;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Compositing.Contracts;

public class SourceBuffer
{
    public string Original { get; }
    public AddBuffer Added { get; }
    
    public PieceTree PieceTree { get; }
    public FormatTree FormatTree { get; }

    
    public SourceBuffer()
    {
        Original = string.Empty;
        Added = new AddBuffer();
        
        var piece = new Piece(0, Original.Length, Piece.SourceType.Original);
        PieceTree = new PieceTree(piece, null, null);

        var format = new FormatBranch();
        FormatTree = new FormatTree(format, null, null);
    }
    
    public SourceBuffer(string text, FormatTree format)
    {
        Original = text;
        Added = new AddBuffer();
        
        var piece = new Piece(0, Original.Length, Piece.SourceType.Original);
        PieceTree = new PieceTree(piece, null, null);

        FormatTree = format;
    }


    public TextElement this[Index index] => GetValueAt(index);

    public SourceSlice this[Range range] => Slice(range);

    private SourceSlice Slice(Range range)
    {
        var (start, end) = range.GetOffsetAndLength(PieceTree.Length);
        return Slice(start, end);
    }
    
    public SourceSlice Slice(Slice slice) => Slice(slice.Start, slice.Length);

    public SourceSlice Slice(int start, int length)
    {
        return new SourceSlice(start, length, this);
    }

    public TextElement GetValueAt(Index index)
    {
        var (piece, pRelativeIndex) = PieceTree.Find(index);
        var content = GetContinuousPieceSpan(piece.Start + pRelativeIndex, 1, piece.Source);
        var c = content[0];
        
        var (format, fRelativeIndex) = FormatTree.Find(index);
        var f = format.Format;

        return new TextElement(c, f);
    }


    private ReadOnlySpan<char> GetContinuousPieceSpan(
        int start, 
        int length, 
        Piece.SourceType type)
    {
        return type == Piece.SourceType.Original
            ? Original.AsSpan(start, length)
            : Added.GetSpan(start, length);
    }
}