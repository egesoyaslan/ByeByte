namespace ByteFinder.Core.Types;

public sealed class ScanResult
{
    public List<FileEntry> Entries { get; } = [];
}

public readonly record struct ScanProgress(long ScannedBytes, int ScannedFileCount);
