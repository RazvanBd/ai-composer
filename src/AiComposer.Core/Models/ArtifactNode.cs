namespace AiComposer.Core.Models;

/// <summary>Knowledge-graph node loaded from a markdown artifact file.</summary>
public sealed class ArtifactNode
{
    /// <summary>Unique identifier for this node (from frontmatter or file stem).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Node type: ticket, epic, business_rule, adr, project_summary, etc.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Human-readable title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown body content (everything after the frontmatter block).</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Raw frontmatter key/value pairs parsed from the YAML block.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>Absolute path to the source markdown file.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>IDs of other nodes this node directly references.</summary>
    public List<string> Links { get; set; } = new();
}
