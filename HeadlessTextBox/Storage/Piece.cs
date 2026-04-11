using HeadlessTextBox.Utils;
using HeadlessTextBox.Utils.WeightedTree;

namespace HeadlessTextBox.Storage;

public readonly struct Piece: IBranch<Piece>
{
    public enum SourceType
    {
        Original,
        Add
    }
    
    
    public SourceType Source { get; }
    
    public int Start { get; }
    
    public int Length { get; }


    public Piece(
        int start, 
        int length, 
        SourceType source = SourceType.Add)
    {
        Start = start;
        Source = source;
        Length = length;
    }
    
    
    public (Piece, Piece) Split(int index)
    {
        var left = new Piece(Start, index, Source);
        var right = new Piece(Start + index, Length - index, Source);
        return (left, right);
    }
}