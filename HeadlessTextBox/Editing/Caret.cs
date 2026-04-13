namespace HeadlessTextBox.Editing;

public readonly record struct Caret(
    int Start,
    int Selection)
{
    public int End => Start + Selection;
    public int Left => Selection >= 0 ? Start : Start + Selection;
    public int Right => Left + Length;
    public int Length => Math.Abs(Selection);

    
    public bool CheckInRange(int index) 
        => Left <= index && index <= Left + Length;
    
    public ReadOnlySpan<char> Slice(ReadOnlySpan<char> source)
        => source.Slice(Left, Length);
}