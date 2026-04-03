using System.Runtime.InteropServices;
using Icu;
using MonoTextBox.Utils;

namespace MonoTextBox.Positioning.Helpers;

public unsafe class LineBreaker: ILineBreaker
{
    private enum UBreakIteratorType
    {
        Character = 0,
        Word = 1,
        Line = 2,
        Sentence = 3,
        Title = 4
    }
    
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr UbrkOpenDelegate(
        int type, 
        [MarshalAs(UnmanagedType.LPStr)] string locale, 
        char* text, 
        int textLen,
        out int status);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UbrkSetTextDelegate(
        IntPtr breakIterator, 
        char* text, 
        int textLength, 
        out ErrorCode errorCode);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int UbrkFirstDelegate(IntPtr breakIterator);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int UbrkNextDelegate(IntPtr breakIterator);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UbrkCloseDelegate(IntPtr breakIterator);

    private readonly UbrkFirstDelegate _first;
    private readonly UbrkNextDelegate _next;
    private readonly UbrkCloseDelegate _close;
    private readonly UbrkSetTextDelegate _setText;
    
    private IntPtr _handle;

    
    public LineBreaker(Locale locale)
    {
        var open = IcuHooks.GetMethodPtr<UbrkOpenDelegate>("ubrk_open");
        _setText = IcuHooks.GetMethodPtr<UbrkSetTextDelegate>("ubrk_setText");
        _first = IcuHooks.GetMethodPtr<UbrkFirstDelegate>("ubrk_first");
        _next = IcuHooks.GetMethodPtr<UbrkNextDelegate>("ubrk_next");
        _close = IcuHooks.GetMethodPtr<UbrkCloseDelegate>("ubrk_close");

        _handle = open((int)UBreakIteratorType.Line, locale.Id, null, 0, out var status);
        if (IsFailure(status)) 
            throw new Exception($"ICU initialization failed: {status}");
    }

    public void Break(
        ReadOnlySpan<char> text,
        Action<int, int> onBreak)
    {
        if (_handle == IntPtr.Zero)
            throw new ObjectDisposedException(nameof(LineBreaker));
        
        fixed (char* ptr = text)
        {
            _setText(_handle, ptr, text.Length, out var status);
            if (IsFailure(status))
                throw new Exception($"ICU failed to set text: {status}");
            
            for (int start = _first(_handle), end = _next(_handle); 
                 end != -1; 
                 start = end, end = _next(_handle)) 
            {
                onBreak.Invoke(start, end);
            }
        }
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