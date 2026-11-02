using System.Runtime.InteropServices;

namespace ByeByte.Core.Scanner.Win32;

internal static class NativeMethods
{
    internal enum FindExInfoLevel
    {
        Standard = 0,
        Basic = 1
    }

    internal enum FindExSearchMode
    {
        NameMatch = 0
    }

    internal const uint FindFirstExLargeFetch = 0x00000002; // Corresponds to int value 2.
    
    [DllImport(
        "kernel32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true,
        EntryPoint = "FindFirstFileExW")]
    internal static extern SafeHandle FindFirstFileEx(
        string fileName,
        FindExInfoLevel infoLevel,
        out Win32FindDataW findFileData,
        FindExSearchMode searchMode,
        IntPtr searchFilter,
        uint flags);

    [DllImport(
        "kernel32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true,
        EntryPoint = "FindNextFileW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FindNextFile(
        SafeHandle findFile,
        out Win32FindDataW findFileData);
    
    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FindClose(IntPtr findFile);
}