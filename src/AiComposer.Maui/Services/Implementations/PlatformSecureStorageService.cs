using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="ISecureStorageService"/> using the platform-provided
/// <see cref="SecureStorage"/> API for credential protection at rest.
/// </summary>
public sealed class PlatformSecureStorageService : ISecureStorageService
{
    private const string ApiKeyKey = "ai_composer_api_key";

    /// <inheritdoc/>
    public async Task<string> GetApiKeyAsync()
    {
        return await SecureStorage.Default.GetAsync(ApiKeyKey) ?? string.Empty;
    }

    /// <inheritdoc/>
    public async Task SetApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            SecureStorage.Default.Remove(ApiKeyKey);
            return;
        }

        await SecureStorage.Default.SetAsync(ApiKeyKey, apiKey);
    }

    /// <inheritdoc/>
    public Task RemoveApiKeyAsync()
    {
        SecureStorage.Default.Remove(ApiKeyKey);
        return Task.CompletedTask;
    }
}
