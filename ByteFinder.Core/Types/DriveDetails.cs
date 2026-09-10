namespace ByteFinder.Core.Types;

public readonly record struct DriveDetails(
    string Name,
    string RootPath,
    long TotalSize,
    long AvailableFreeSpace);
