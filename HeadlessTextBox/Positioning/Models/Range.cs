namespace HeadlessTextBox.Positioning.Models;

// To distinguish with Slice:
//  Range represents position;
//  Slice represents slice.
public readonly record struct Range(
    float StartPos,
    float EndPos)
{
    public static Range operator +(Range left, Range right) 
        => left with { EndPos = left.EndPos + right.EndPos };
    
    public static Range operator +(Range range, float addend) 
        => new(range.StartPos + addend, range.EndPos + addend);

    public float Width => EndPos - StartPos;
}