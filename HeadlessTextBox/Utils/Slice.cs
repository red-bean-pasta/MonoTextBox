namespace HeadlessTextBox.Utils;

public readonly record struct Slice(
    int Start,
    int Length)
{
    public int End => Start + Length;
    
    public Slice Offset(int offset) 
        => new(Start + offset, Length);

    public Slice Extend(int length)
        => new(Start, Length + length);

    public Range ToRange() => new(Start, End);
}