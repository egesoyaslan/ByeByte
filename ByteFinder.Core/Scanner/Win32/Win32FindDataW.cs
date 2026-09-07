using System.Runtime.InteropServices;

namespace ByteFinder.Core.Scanner.Win32;

// This struct represents the native windows WIN32_FIND_DATA_W in C#.
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct Win32FindDataW
{
    public FileAttributes FileAttributes;
    
    public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
    public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
    
    public uint FileSizeHigh;
    public uint FileSizeLow;
    
    public uint Reserved0;
    public uint Reserved1;
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string FileName;
    
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
    public string AlternateFileName;
}