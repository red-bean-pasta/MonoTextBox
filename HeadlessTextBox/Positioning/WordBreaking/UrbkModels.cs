using System.Runtime.InteropServices;
using Icu;

namespace HeadlessTextBox.Positioning.Manual.WordBreaking;

public static unsafe class UrbkModels
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr UbrkOpenDelegate(
        int type, 
        [MarshalAs(UnmanagedType.LPStr)] string locale, 
        char* text, 
        int textLen,
        out int status);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UbrkSetTextDelegate(
        IntPtr breakIterator, 
        char* text, 
        int textLength, 
        out ErrorCode errorCode);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int UbrkFirstDelegate(IntPtr breakIterator);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int UbrkNextDelegate(IntPtr breakIterator);
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UbrkCloseDelegate(IntPtr breakIterator);
}