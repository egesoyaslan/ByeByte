namespace ByteFinder.Core.Types;

public sealed class ScanResult
{
    public List<FileEntry> Entries { get; } = [];

    public string GetFullPath(int fileIndex)
    {
        var files = new Stack<string>();
        for (var i = fileIndex; i >= 0; i = Entries[i].ParentIndex) // Pushes parent folders until it pushes the root on the stack.
        {
            files.Push(Entries[i].Name);
        }

        string path = "";
        foreach (var fileName in files) // Constructs the path starting from the root at the top of the stack.
        {
            path = Path.Combine(path, fileName);
        }

        return path;
    }
}

public readonly record struct ScanProgress(long ScannedBytes, int ScannedFileCount);
