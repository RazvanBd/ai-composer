namespace AiComposer.Maui.Models;

/// <summary>A named, ordered group of artifacts for display in the Artifacts Explorer.</summary>
public sealed class ArtifactGroup : List<ArtifactItem>
{
    /// <summary>Human-readable group name (e.g., "Tickets", "Epics").</summary>
    public string GroupTitle { get; }

    /// <summary>Initialises the group with a display title and its member items.</summary>
    public ArtifactGroup(string groupTitle, IEnumerable<ArtifactItem> items) : base(items)
    {
        GroupTitle = groupTitle;
    }
}
