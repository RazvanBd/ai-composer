using AiComposer.Core.Models;

namespace AiComposer.Core.Markdown;

/// <summary>
/// Parses markdown files with YAML frontmatter into <see cref="ArtifactNode"/> objects
/// and assembles them into a knowledge graph by resolving inter-node links.
/// </summary>
public static class MarkdownLoader
{
    /// <summary>
    /// Recursively discovers all *.md files under <paramref name="rootDir"/>,
    /// parses their frontmatter and body, then links related nodes.
    /// </summary>
    public static Dictionary<string, ArtifactNode> LoadArtifacts(string rootDir)
    {
        var nodes = new Dictionary<string, ArtifactNode>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(rootDir, "*.md", SearchOption.AllDirectories)
                                      .OrderBy(f => f))
        {
            var node = ParseFile(file);
            nodes[node.Id] = node;
        }

        LinkNodes(nodes);
        return nodes;
    }

    // ── private helpers ────────────────────────────────────────────────────────

    private static ArtifactNode ParseFile(string path)
    {
        var raw = File.ReadAllText(path);
        var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var body = raw;

        if (raw.StartsWith("---\n", StringComparison.Ordinal))
        {
            var parts = raw.Split(["---\n"], 3, StringSplitOptions.None);
            if (parts.Length >= 3)
            {
                metadata = ParseFrontmatter(parts[1]);
                body = parts[2].Trim();
            }
        }

        var stem = System.IO.Path.GetFileNameWithoutExtension(path);
        var id = metadata.TryGetValue("id", out var rawId) ? rawId?.ToString() ?? stem : stem;
        var type = metadata.TryGetValue("type", out var rawType)
            ? (rawType?.ToString() ?? "document").ToLowerInvariant()
            : "document";
        var title = metadata.TryGetValue("title", out var rawTitle)
            ? rawTitle?.ToString() ?? ToTitle(stem)
            : ToTitle(stem);

        return new ArtifactNode
        {
            Id = id,
            Type = type,
            Title = title,
            Body = body,
            Metadata = metadata,
            Path = path,
        };
    }

    private static Dictionary<string, object> ParseFrontmatter(string frontmatter)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        string? currentListKey = null;

        foreach (var rawLine in frontmatter.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var stripped = line.TrimStart();

            // List item under the previous key
            if (stripped.StartsWith("- ", StringComparison.Ordinal) && currentListKey is not null)
            {
                if (!result.ContainsKey(currentListKey))
                    result[currentListKey] = new List<string>();
                ((List<string>)result[currentListKey]).Add(stripped[2..].Trim().Trim('\'', '"'));
                continue;
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0) continue;

            var key = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();

            if (string.IsNullOrEmpty(value))
            {
                result[key] = new List<string>();
                currentListKey = key;
                continue;
            }

            result[key] = ParseScalar(value);
            currentListKey = null;
        }

        return result;
    }

    private static object ParseScalar(string value)
    {
        value = value.Trim().Trim('\'', '"');
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        if (int.TryParse(value, out var i)) return i;

        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            var inner = value[1..^1].Trim();
            if (string.IsNullOrEmpty(inner)) return new List<string>();
            return inner.Split(',').Select(s => s.Trim().Trim('\'', '"')).ToList();
        }

        return value;
    }

    private static void LinkNodes(Dictionary<string, ArtifactNode> nodes)
    {
        foreach (var node in nodes.Values)
        {
            var links = new List<string>();

            if (node.Type == "ticket")
            {
                if (node.Metadata.TryGetValue("epic", out var epic) && epic is string epicId && nodes.ContainsKey(epicId))
                    links.Add(epicId);

                if (node.Metadata.TryGetValue("rules", out var rules) && rules is List<string> ruleIds)
                    links.AddRange(ruleIds.Where(r => nodes.ContainsKey(r)));
            }

            if (node.Type == "adr")
            {
                if (node.Metadata.TryGetValue("ticket", out var ticket) && ticket is string tId && nodes.ContainsKey(tId))
                    links.Add(tId);
            }

            node.Links = links.Distinct().ToList();
        }
    }

    private static string ToTitle(string stem) =>
        System.Text.RegularExpressions.Regex.Replace(
            stem.Replace("-", " "),
            @"\b\w",
            m => m.Value.ToUpperInvariant());
}
