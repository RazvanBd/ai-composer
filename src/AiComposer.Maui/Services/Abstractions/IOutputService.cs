using AiComposer.Maui.Models;

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Lists and reads files generated in the output folder.</summary>
public interface IOutputService
{
    /// <summary>Lists all generated files under the output folder for the given ticket.</summary>
    Task<IReadOnlyList<GeneratedFileItem>> ListGeneratedFilesAsync(string ticketId, CancellationToken ct = default);

    /// <summary>Reads the text content of a generated file.</summary>
    Task<string> ReadFileContentAsync(string fullPath, CancellationToken ct = default);
}
