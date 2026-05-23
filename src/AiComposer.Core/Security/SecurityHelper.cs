using AiComposer.Core.Models;

namespace AiComposer.Core.Security;

/// <summary>
/// Security helpers: ephemeral Docker sandbox command builder and
/// injectable mock environment for external service integrations.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Builds the docker CLI arguments for an ephemeral, restricted container.
    /// Network access is blocked and the workspace is the only writable mount.
    /// </summary>
    public static IReadOnlyList<string> BuildSandboxCommand(SandboxPolicy policy, IEnumerable<string> innerCommand)
    {
        var workspace = Path.GetFullPath(policy.Workspace);
        var args = new List<string>
        {
            "docker", "run", "--rm",
            "--network", policy.NetworkMode,
            "--cpus", policy.CpuLimit,
            "--memory", policy.MemoryLimit,
        };

        if (policy.ReadOnlyRoot)
            args.Add("--read-only");

        args.AddRange(["-v", $"{workspace}:/workspace:rw", "-w", "/workspace", policy.Image]);
        args.AddRange(innerCommand);
        return args.AsReadOnly();
    }

    /// <summary>
    /// Returns environment variable overrides that redirect external dependencies
    /// to local mock services (e.g. WireMock), ensuring no real secrets are required.
    /// </summary>
    public static Dictionary<string, string> BuildMockEnvironment(Dictionary<string, string>? overrides = null)
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PAYMENTS_API_URL"] = "http://wiremock:8080/payments",
            ["S3_ENDPOINT"] = "http://wiremock:8080/s3",
            ["S3_ACCESS_KEY"] = "mock-access-key",
            ["S3_SECRET_KEY"] = "mock-secret-key",
            ["USE_MOCK_SERVICES"] = "true",
        };

        if (overrides is not null)
            foreach (var kv in overrides)
                env[kv.Key] = kv.Value;

        return env;
    }

    /// <summary>
    /// Returns true when <paramref name="targetPath"/> is inside <paramref name="workspace"/>,
    /// preventing path traversal during code generation.
    /// </summary>
    public static bool IsPathInsideWorkspace(string workspace, string targetPath)
    {
        var root = Path.GetFullPath(workspace);
        var target = Path.GetFullPath(targetPath);
        return target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || string.Equals(root, target, StringComparison.OrdinalIgnoreCase);
    }
}
