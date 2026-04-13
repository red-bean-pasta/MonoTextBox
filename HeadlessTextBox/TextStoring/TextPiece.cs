using HeadlessTextBox.Storage.WeightedTree;

namespace HeadlessTextBox.TextStoring;

public readonly struct TextPiece: IBranch<TextPiece>
{
    public enum SourceType
    {
        Original,
        Add
    }
    
    
    public SourceType Source { get; }
    
    public int Start { get; }
    
    public int Length { get; }


    public TextPiece(
        int start, 
        int length, 
        SourceType source)
    {
        Start = start;
        Source = source;
        Length = length;
    }
    
    
    public (TextPiece, TextPiece) Split(int index)
    {
        var left = new TextPiece(Start, index, Source);
        var right = new TextPiece(Start + index, Length - index, Source);
        return (left, right);
    }
}