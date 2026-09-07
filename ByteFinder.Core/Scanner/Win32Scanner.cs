using System.ComponentModel;
using System.Runtime.InteropServices;
using ByteFinder.Core.Types;
using ByteFinder.Core.Scanner.Win32;

namespace ByteFinder.Core.Scanner;

public class Win32Scanner
{
    public ScanResult ScanDirectory(string path)
    {
        var result = new ScanResult();
        
        result.Entries.Add(
            new FileEntry(
                Name: path,
                ParentIndex: -1,
                Size: 0,
                Attributes: FileAttributes.Directory));

        const int rootIndex = 0;
        
        ScanSingleDirectory(path, rootIndex, result);
        
        return result;
    }

    private void ScanSingleDirectory(string path, int parentIndex, ScanResult result)
    {
        // Results in (Drive Letter)/Path/* so everything in path can be name matched.
        string searchPath = Path.Combine(path, "*");
        
        using Win32.SafeHandle handle = NativeMethods.FindFirstFileEx(
            searchPath,
            NativeMethods.FindExInfoLevel.Basic,
            out Win32FindDataW data,
            NativeMethods.FindExSearchMode.NameMatch,
            IntPtr.Zero,  // This is a null native pointer telling windows there aren't any search filters.
            NativeMethods.FindFirstExLargeFetch);

        if (handle.IsInvalid)
        {
            int errorCode = Marshal.GetLastWin32Error();

            // This is the file not found error so we return from here.
            if (errorCode == 2)
                return;
            
            throw new Win32Exception(errorCode);
        }

        do
        {
            string name = data.FileName;

            if (name is "." or "..")
                continue;

            bool isDirectory = (data.FileAttributes & FileAttributes.Directory) != 0;

            long size = isDirectory
                ? 0
                // This is the way I construct 64 bit size data.
                // The casting to long adds 32 zero bits to the left. The bitwise shift shifts the high 32 bits to the left.
                // The bitwise or replaces the empty 32 bits on the right with the low 32 bits.
                // FileSizeHigh and FileSizeLow are just 64 bit size data divided in two. High is 0 unless necessary.
                : ((long)data.FileSizeHigh << 32) | data.FileSizeLow;

            result.Entries.Add(new FileEntry(
                ParentIndex: parentIndex,
                Name: name,
                Size: size,
                Attributes: data.FileAttributes));
        }
        while (NativeMethods.FindNextFile(handle, out data));
    }
}