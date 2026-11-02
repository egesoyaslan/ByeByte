using System.Collections.ObjectModel;

namespace ByteFinder.App.Types;

public sealed record ExplorerNode(
    string Name,
    string Size,
    string OccupiedPercentage,
    string Extension,
    bool IsDirectory,
    int FileIndex,
    System.Windows.Thickness NameIndent,
    ObservableCollection<ExplorerNode> Children)
{
    public bool HasChildren => Children.Count > 0;
}
