namespace MonoTextBox.Positioning;

public class Line
{
    private const float LeftEdge = 0f;
        
    
    private readonly List<Range> _positions = new();

    
    public IReadOnlyList<Range> Positions => _positions;
    public bool Empty => Positions.Count == 0;
    public float RightEdge => Empty ? 0 : Positions[^1].End;


    public void Append(Range range) 
        => _positions.Add(LineRange(range, this));


    public static Range LineRange(Range range, Line line)
    {
        var appended = range + line.RightEdge;
        var clamped = ClampLeft(appended);
        return clamped;
    }
    
    private static Range ClampLeft(Range range)
    {
        if (range.Start >= LeftEdge)
            return range;
        
        var addend = LeftEdge - range.Start;
        return range + addend;
    }
}