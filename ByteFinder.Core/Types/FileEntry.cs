namespace ByteFinder.Core.Types;

public readonly record struct FileEntry(
    string Name,
    int ParentIndex,
    long Size,
    FileAttributes Attributes)
{
    public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;
    public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;
    public bool IsSystem => (Attributes & FileAttributes.System) != 0;
}