namespace MonoTextBox.Editing.Buffer;

public struct Piece
{
    public enum SourceType
    {
        Save,
        Add
    }
    
    
    public SourceType Source { get; }
    
    public int StartIndex { get; }
    
    public int Length { get; }
    
    
    public Piece(
        int startIndex, 
        int length, 
        SourceType source = SourceType.Add)
    {
        StartIndex = startIndex;
        Source = source;
        Length = length;
    }
}