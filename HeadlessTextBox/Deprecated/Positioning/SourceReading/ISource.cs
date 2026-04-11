using HeadlessTextBox.Compositing.Contract;
using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Deprecated.Positioning.SourceReading;

// DEPRECATED: interface disallow ref struct and may box struct uncarefully
public interface ISource
{
    int Length { get; }
    
    
    SourceEnumerator GetEnumerator();
    
    
    TextElement this[Index index] { get; }
    
    ISource this[System.Range range] { get; }
    
    ISource Slice(Slice slice);
    
    ISource Slice(System.Range range);

    ISource Slice(int offset, int length);
    
    
    ReadOnlySpan<char> GetTextSpan();
}


public readonly unsafe ref struct SourceEnumerator
{
    private readonly void* _state;
    private readonly delegate* managed<void*, bool> _moveNext;
    private readonly delegate* managed<void*, TextElement> _getCurrent;
    
    public SourceEnumerator(
        void* state,
        delegate* managed<void*, bool> moveNext, 
        delegate* managed<void*, TextElement> getCurrent)
    {
        _state = state;
        _getCurrent = getCurrent;
        _moveNext = moveNext;
    }

    public TextElement Current => _getCurrent(_state);

    public bool MoveNext() => _moveNext(_state);
}