using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MonoTextBox.Utils;

public static class IcuHooks
{
    private static IntPtr _commonHandle;
    private static int _version = -1;
    
    private static IntPtr CommonHandle => GetHandle();
    private static int Version => GetVersion();


    private static void Initialize()
    {
        var nativeMethodsType = typeof(Icu.Wrapper).Assembly.GetType("Icu.NativeMethods");
        Debug.Assert(nativeMethodsType is not null);
        
        var handle = nativeMethodsType
            .GetProperty("IcuCommonLibHandle", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null);
        Debug.Assert(handle is not null);
        _commonHandle = (IntPtr)handle;
        if (_commonHandle == IntPtr.Zero)
            throw new InvalidOperationException("Accessing CommonLibHandle before Icu.NativeMethods is initialized");
        
        var version = nativeMethodsType
            .GetField("IcuVersion", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null);
        Debug.Assert(version is not null);
        _version = (int)version;
    }
    
    
    public static T GetMethodPtr<T>(string name)
    {
        var mangled = $"{name}_{Version}";
        if (NativeLibrary.TryGetExport(CommonHandle, mangled, out var ptr) 
            || NativeLibrary.TryGetExport(CommonHandle, name, out ptr))
            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        throw new Exception($"ICU Error: Failed to locate method '{name}'");
    }


    private static int GetVersion()
    {
        if (_version == -1)
            Initialize();
        
        return _version;
    }

    private static IntPtr GetHandle()
    {
        if (_version == -1)
            Initialize();
        
        return _commonHandle;
    }
}