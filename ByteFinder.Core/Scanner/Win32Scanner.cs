using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using ByteFinder.Core.Types;
using ByteFinder.Core.Scanner.Win32;

namespace ByteFinder.Core.Scanner;

public class Win32Scanner
{
    public IReadOnlyList<DriveDetails> GetReadyDrives()
    {
        var drives = new List<DriveDetails>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                    continue;

                string rootPath = drive.RootDirectory.FullName;
                string name = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                    ? rootPath
                    : drive.VolumeLabel;

                drives.Add(new DriveDetails(
                    Name: name,
                    RootPath: rootPath,
                    TotalSize: drive.TotalSize,
                    AvailableFreeSpace: drive.AvailableFreeSpace));
            }
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
        }

        return drives;
    }

    // Same performance improvement can be done on this method as well.
    public ScanResult ScanDirectory(string path, IProgress<long>? progress = null)
    {
        var result = new ScanResult();
        long scannedBytes = 0;
        long lastReportedBytes = 0;
        var progressTimer = Stopwatch.StartNew();
        
        result.Entries.Add(
            new FileEntry(
                Name: path,
                ParentIndex: -1,
                Size: 0,
                Attributes: FileAttributes.Directory));

        const int rootIndex = 0;
        
        var rootSize = ScanDirectoryContents(
            path,
            rootIndex,
            result,
            progress,
            ref scannedBytes,
            ref lastReportedBytes,
            progressTimer);

        result.Entries[rootIndex] = result.Entries[rootIndex] with
        {
            Size = rootSize
        };

        progress?.Report(scannedBytes);
        
        return result;
    }

    // POSSIBLE PERFORMANCE IMPROVEMENT: This method creates copies of each FileEntry with the calculated size.
    // Since FileEntry is a record struct we cannot change the size property directly.
    private long ScanDirectoryContents(
        string path,
        int parentIndex,
        ScanResult result,
        IProgress<long>? progress,
        ref long scannedBytes,
        ref long lastReportedBytes,
        Stopwatch progressTimer)
    {
        var childDirectories = 
            ScanSingleDirectory(
                path,
                parentIndex,
                result,
                out var nonDirectorySize,
                progress,
                ref scannedBytes,
                ref lastReportedBytes,
                progressTimer);
        
        var totalSize = nonDirectorySize;

        foreach (var child in childDirectories)
        {
            long childSize = ScanDirectoryContents(
                child.Path,
                child.Index,
                result,
                progress,
                ref scannedBytes,
                ref lastReportedBytes,
                progressTimer);

            result.Entries[child.Index] = result.Entries[child.Index] with
            {
                Size = childSize
            };
            
            totalSize += childSize;
        }
        
        return totalSize;
    }

    private List<(string Path, int Index)> ScanSingleDirectory(
        string path, 
        int parentIndex, 
        ScanResult result,
        out long nonDirectorySize,
        IProgress<long>? progress,
        ref long scannedBytes,
        ref long lastReportedBytes,
        Stopwatch progressTimer)
    {
        nonDirectorySize = 0;
        
        var childDirectories = new List<(string Path, int Index)>();
        
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

            // These are the native error codes of windows: 
            // 2 = ERROR_FILE_NOT_FOUND
            // 3 = ERROR_PATH_NOT_FOUND
            // 5 = ERROR_ACCESS_DENIED
            if (errorCode is 2 or 3 or 5)
                return childDirectories;
            
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

            int entryIndex = result.Entries.Count;

            result.Entries.Add(new FileEntry(
                ParentIndex: parentIndex,
                Name: name,
                Size: size,
                Attributes: data.FileAttributes));

            if (isDirectory)
            {
                // ReparsePoint files can also be detected as directories so this prevents that.
                bool isReparsePoint = (data.FileAttributes & FileAttributes.ReparsePoint) != 0;

                if (!isReparsePoint)
                {
                    childDirectories.Add((Path.Combine(path, name), entryIndex));
                }
            }
            else
            {
                nonDirectorySize += size;
                scannedBytes += size;

                if (progress is not null &&
                    (scannedBytes - lastReportedBytes >= 4 * 1024 * 1024 ||
                     progressTimer.ElapsedMilliseconds >= 100))
                {
                    progress.Report(scannedBytes);
                    lastReportedBytes = scannedBytes;
                    progressTimer.Restart();
                }
            }
        }
        while (NativeMethods.FindNextFile(handle, out data));
        
        return childDirectories;
    }
}
