using Microsoft.Win32.SafeHandles;

namespace ByteFinder.Core.Scanner.Win32;

internal sealed class SafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeHandle() : base(true) {}

    protected override bool ReleaseHandle()
    {
        return NativeMethods.FindClose(handle);
    }
}