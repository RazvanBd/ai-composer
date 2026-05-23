namespace AiComposer.Core.Models;

/// <summary>Container isolation policy for ephemeral test/runtime execution.</summary>
public sealed class SandboxPolicy
{
    /// <summary>Host workspace path to mount inside the container.</summary>
    public string Workspace { get; set; } = string.Empty;

    /// <summary>Container image to use for execution.</summary>
    public string Image { get; set; } = "mcr.microsoft.com/dotnet/runtime:9.0";

    /// <summary>Docker network mode (use "none" to block all egress).</summary>
    public string NetworkMode { get; set; } = "none";

    /// <summary>When true, mounts the root filesystem read-only.</summary>
    public bool ReadOnlyRoot { get; set; } = true;

    /// <summary>CPU quota (fractional cores, e.g. "1.0").</summary>
    public string CpuLimit { get; set; } = "1.0";

    /// <summary>Maximum memory (e.g. "512m").</summary>
    public string MemoryLimit { get; set; } = "512m";
}
