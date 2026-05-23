using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;
using CommunityToolkit.Maui.Storage;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Settings page — read and write application settings.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IWorkspaceService _workspaceService;
    private readonly ISecureStorageService _secureStorageService;

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _selectedProvider = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _hasSavedConfirmation;

    [ObservableProperty]
    private string _saveConfirmationMessage = string.Empty;

    /// <summary>Supported AI providers available in Settings.</summary>
    public IReadOnlyList<string> Providers { get; } =
        ["DeepSeek", "Copilot", "Local"];

    /// <summary>Initialises <see cref="SettingsViewModel"/>.</summary>
    public SettingsViewModel(ISettingsService settingsService, IWorkspaceService workspaceService, ISecureStorageService secureStorageService)
    {
        _settingsService = settingsService;
        _workspaceService = workspaceService;
        _secureStorageService = secureStorageService;
        _ = LoadSettingsAsync();
    }

    /// <summary>Loads settings from storage into the view model properties.</summary>
    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        WorkspacePath = settings.WorkspacePath;
        OutputPath = settings.OutputPath;
        SelectedProvider = ToProviderLabel(settings.AiProvider);
        Model = settings.AiModel;
        ApiKey = await _secureStorageService.GetApiKeyAsync();
        HasSavedConfirmation = false;
    }

    /// <summary>Saves the current view model values to persistent settings.</summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            var settings = await _settingsService.LoadAsync();
            settings.WorkspacePath = WorkspacePath;
            settings.OutputPath = OutputPath;
            settings.AiProvider = ToProviderValue(SelectedProvider);
            settings.AiModel = Model;

            await _settingsService.SaveAsync(settings);
            await _secureStorageService.SetApiKeyAsync(ApiKey);
            SaveConfirmationMessage = $"Settings saved at {DateTime.Now:HH:mm:ss}.";
            HasSavedConfirmation = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Opens a folder picker and sets the workspace path without persisting.</summary>
    [RelayCommand]
    private async Task BrowseWorkspaceAsync()
    {
        var selected = await _workspaceService.OpenWorkspaceAsync();
        if (!string.IsNullOrWhiteSpace(selected))
            WorkspacePath = selected;
    }

    /// <summary>Opens a folder picker and sets the output path.</summary>
    [RelayCommand]
    private async Task BrowseOutputAsync()
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Folder?.Path))
            OutputPath = result.Folder.Path;
    }

    /// <summary>Restores form values to their defaults.</summary>
    [RelayCommand]
    private async Task ResetAsync()
    {
        var defaults = new AppSettings();
        WorkspacePath = defaults.WorkspacePath;
        OutputPath = defaults.OutputPath;
        SelectedProvider = ToProviderLabel(defaults.AiProvider);
        Model = defaults.AiModel;
        ApiKey = string.Empty;
        await _secureStorageService.RemoveApiKeyAsync();
        await _settingsService.SaveAsync(defaults);
        HasSavedConfirmation = false;
    }

    private static string ToProviderLabel(string provider) => provider.ToLowerInvariant() switch
    {
        "deepseek" => "DeepSeek",
        "copilot" => "Copilot",
        "local" or "" => "Local",
        _ => "DeepSeek",
    };

    private static string ToProviderValue(string providerLabel) => providerLabel switch
    {
        "DeepSeek" => "deepseek",
        "Copilot" => "copilot",
        "Local" => "",
        _ => "deepseek",
    };
}
