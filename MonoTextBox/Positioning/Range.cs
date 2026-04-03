namespace MonoTextBox.Positioning;

public record struct Range(
    float Start,
    float End
)
{
    public static Range operator +(Range left, Range right) 
        => left with { End = left.End + right.End };
    
    public static Range operator +(Range range, float addend) 
        => new(range.Start + addend, range.End + addend);

    public float Width => End - Start;
}