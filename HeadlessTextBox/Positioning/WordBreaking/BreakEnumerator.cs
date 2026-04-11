using HeadlessTextBox.Utils;

namespace HeadlessTextBox.Positioning.WordBreaking;

public ref struct BreakEnumerator
{
    private readonly IntPtr _handle;
    private readonly UrbkModels.UbrkNextDelegate _next;
    
    private int _currentStart;
    private int _currentEnd;
    
    private bool _isFinished;

    
    public Slice Current => new(_currentStart, _currentEnd);
    
    
    public BreakEnumerator(
        IntPtr handle, 
        UrbkModels.UbrkFirstDelegate first, 
        UrbkModels.UbrkNextDelegate next)
    {
        _isFinished = false;
        
        _handle = handle;
        _next = next;
        
        _currentStart = first(handle);
        _currentEnd = _currentStart;
    }

    
    public BreakEnumerator GetEnumerator() => this;
    
    public bool MoveNext()
    {
        if (_isFinished) 
            return false;

        var nextEnd = _next(_handle);
        if (nextEnd == -1) 
        {
            _isFinished = true;
            return false;
        }

        _currentStart = _currentEnd;
        _currentEnd = nextEnd;
        return true;
    }
}