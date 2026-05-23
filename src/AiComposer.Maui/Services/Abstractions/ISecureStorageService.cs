namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Provides secure storage for sensitive values such as API keys.</summary>
public interface ISecureStorageService
{
    /// <summary>Retrieves the stored API key, or an empty string if none is stored.</summary>
    Task<string> GetApiKeyAsync();

    /// <summary>Stores the API key securely.</summary>
    Task SetApiKeyAsync(string apiKey);

    /// <summary>Removes the stored API key.</summary>
    Task RemoveApiKeyAsync();
}
