using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Positioning.Manual.SpanEnumerating;


public ref struct OffsetResult
{
    public bool IsMoved { get; }
    public Slice Current { get; }

    
    public static OffsetResult Finish => new(false, new Slice(0, 0));
    
    
    public OffsetResult(
        bool isMoved, 
        Slice current)
    {
        IsMoved = isMoved;
        Current = current;
    }
}


public ref struct OffsetContext
{
    public bool IsFinished { get; private set; }
    public int AbsoluteOffset { get; private set; }
    public ReadOnlySpan<char> Remains { get; private set; }
    
    
    public OffsetContext(bool isFinished, int absoluteOffset, ReadOnlySpan<char> remains)
    {
        IsFinished = isFinished;
        AbsoluteOffset = absoluteOffset;
        Remains = remains;
    }

    public void Update(
        bool isFinished,
        int absoluteOffset,
        ReadOnlySpan<char> remains)
    {
        IsFinished = isFinished;
        AbsoluteOffset = absoluteOffset;
        Remains = remains;
    }
    
    
    public void Finish()
    {
        IsFinished = true;
        AbsoluteOffset += Remains.Length;
        Remains = Remains[..0];
    }
}


public delegate OffsetResult MoveNext(OffsetContext context);


public ref struct OffsetEnumerator
{
    private readonly OffsetContext _context;
    
    private readonly MoveNext _moveNext;


    public Slice Current { get; private set; }


    public OffsetEnumerator(
        ReadOnlySpan<char> input,
        MoveNext moveNext)
    {
        var finished = input.IsEmpty;
        _context = new OffsetContext(finished, 0, input);
        
        Current = default;
        
        _moveNext = moveNext;
    }
    
    
    public OffsetEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        var result = _moveNext.Invoke(_context);
        Current = result.Current;
        return result.IsMoved;
    }
}