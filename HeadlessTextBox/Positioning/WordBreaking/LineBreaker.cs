using HeadlessTextBox.Utils;
using Icu;

namespace HeadlessTextBox.Positioning.WordBreaking;

public unsafe class LineBreaker
{
    private enum UBreakIteratorType
    {
        Character = 0,
        Word = 1,
        Line = 2,
        Sentence = 3,
        Title = 4
    }

    private readonly UrbkModels.UbrkFirstDelegate _first;
    private readonly UrbkModels.UbrkNextDelegate _next;
    private readonly UrbkModels.UbrkCloseDelegate _close;
    private readonly UrbkModels.UbrkSetTextDelegate _setText;
    
    private IntPtr _handle;

    
    public LineBreaker(Locale locale)
    {
        var open = IcuHooks.GetMethodPtr<UrbkModels.UbrkOpenDelegate>("ubrk_open");
        _setText = IcuHooks.GetMethodPtr<UrbkModels.UbrkSetTextDelegate>("ubrk_setText");
        _first = IcuHooks.GetMethodPtr<UrbkModels.UbrkFirstDelegate>("ubrk_first");
        _next = IcuHooks.GetMethodPtr<UrbkModels.UbrkNextDelegate>("ubrk_next");
        _close = IcuHooks.GetMethodPtr<UrbkModels.UbrkCloseDelegate>("ubrk_close");

        _handle = open((int)UBreakIteratorType.Line, locale.Id, null, 0, out var status);
        if (IsFailure(status)) 
            throw new Exception($"ICU initialization failed: {status}");
    }

    
    public BreakEnumerator Enumerate(
        char* pinnedText, 
        int textLength)
    {
        if (_handle == IntPtr.Zero)
            throw new ObjectDisposedException(nameof(LineBreaker));

        _setText(_handle, pinnedText, textLength, out var status);
        return IsFailure(status) 
            ? throw new Exception($"ICU failed to set text: {status}") 
            : new BreakEnumerator(_handle, _first, _next);
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero)
            return;
        
        _close(_handle);
        _handle = IntPtr.Zero;
    }


    private static bool IsFailure(int status) => status > 0;
    
    private static bool IsFailure(ErrorCode code) => IsFailure((int)code);
}