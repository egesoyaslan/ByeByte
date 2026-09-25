namespace ByteFinder.App.Types;

public sealed record ExplorerNode(
    string Name,
    string Size,
    string OccupiedPercentage,
    string Extension,
    bool IsDirectory,
    System.Windows.Thickness NameIndent,
    IReadOnlyList<ExplorerNode> Children)
{
    public bool HasChildren => Children.Count > 0;
}
