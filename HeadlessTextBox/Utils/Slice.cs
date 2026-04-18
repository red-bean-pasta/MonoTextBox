namespace HeadlessTextBox.Utils;

public readonly record struct Slice(
    int Start,
    int Length)
{
    public int End => Start + Length;
    
    public static Slice operator +(Slice slice, int offset) 
        => new(slice.Start + offset, slice.Length);
}