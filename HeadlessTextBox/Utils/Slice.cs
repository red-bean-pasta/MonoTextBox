namespace HeadlessTextBox.Utils;

public ref struct Slice
{
    public int Start { get; }
    public int End { get; }
    
    
    public int Length => End - Start;
    
    
    public Slice(int start, int end) => (Start, End) = (start, end);
    
    
    public static Slice operator +(Slice slice, int offset) 
        => new(slice.Start + offset, slice.End + offset);
}