using HeadlessTextBox.Positioning.Manual.Models;
using Range = HeadlessTextBox.Positioning.Manual.Models.Range;

namespace HeadlessTextBox.Positioning.Manual;


public class Line
{
    private const float LeftEdge = 0f;


    private readonly float _height;
    private readonly List<Slot> _positions;

    
    public float Height => _height;
    public IReadOnlyList<Slot> Positions => _positions;
    public bool Empty => Positions.Count == 0;
    public int Length => Positions.Count;
    public float RightEdge => Empty ? 0 : Positions[^1].Range.EndPos;


    public Line()
    {
        _height = 0;
        _positions = new List<Slot>();
    }

    public Line(IEnumerable<Slot> positions) : this(positions.ToList())
    { }
    
    public Line(List<Slot> positions)
    {
        if (positions.Count <= 0)
        {
            _positions = positions;
            _height = positions.Max(s => s.Height);
            return;
        }

        var addend = LeftEdge - positions[0].Range.StartPos;
        if (addend == 0f)
        {
            _positions = positions;
            _height = positions.Max(s => s.Height);
            return;
        }

        for (var i = 0; i < positions.Count; i++)
        {
            positions[i] = OffsetSlotRange(positions[i], addend);
            _height = Math.Max(_height, positions[i].Height);
        }
        _positions = positions;
    }
    

    public void Append(Slot slot) 
        => _positions.Add(LineSlot(slot, this));

    public void Patch(int index, Slot slot)
    {
        if (_positions.Count <= index)
            _positions[index] = slot;
        else
            Append(slot);
    }

    
    public static Slot LineSlot(Slot slot, Line line)
    {
        var linedRange = LineRange(slot.Range, line);
        return slot with {Range = linedRange};
    }

    public static Range LineRange(Range range, Line line)
    {
        var appended = range + line.RightEdge;
        var clamped = ClampLeft(appended);
        return clamped;
    }
    
    private static Range ClampLeft(Range range)
    {
        if (range.StartPos >= LeftEdge)
            return range;
        
        var addend = LeftEdge - range.StartPos;
        return range + addend;
    }

    private static Slot OffsetSlotRange(Slot slot, float offset)
    {
        var range = slot.Range;
        return slot with { Range = range + offset };
    }
}