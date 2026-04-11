using HeadlessTextBox.Storage;

namespace HeadlessTextBox.Compositing.Contracts;

public record TextStorage(
    string Original,
    AddBuffer Added,
    PieceTree PieceTree
)
{
    public int Length => PieceTree.Length;
    
    
    public char GetValueAt(Index index)
    {
        var (piece, relativeIndex) = PieceTree.Find(index);
        var span = GetContinuousSpan(piece.Start + relativeIndex, 1, piece.Source);
        return span[0];
    }


    public ReadOnlySpan<char> GetPieceSpan(Piece piece) => GetContinuousSpan(piece.Start, piece.Length, piece.Source);


    private ReadOnlySpan<char> GetContinuousSpan(
        int start, 
        int length, 
        Piece.SourceType type)
    {
        return type == Piece.SourceType.Original
            ? Original.AsSpan(start, length)
            : Added.GetSpan(start, length);
    }
}