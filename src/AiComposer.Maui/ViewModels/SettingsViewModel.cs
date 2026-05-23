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
    private bool _autoApprove;

    [ObservableProperty]
    private int _timeoutMinutes = 30;

    [ObservableProperty]
    private int _maxRetries = 3;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _hasSavedConfirmation;

    [ObservableProperty]
    private string _saveConfirmationMessage = string.Empty;

    /// <summary>Supported AI providers available in Settings.</summary>
    public IReadOnlyList<string> Providers { get; } =
        ["OpenAI", "Azure OpenAI", "Anthropic", "Local"];

    /// <summary>Initialises <see cref="SettingsViewModel"/>.</summary>
    public SettingsViewModel(ISettingsService settingsService, IWorkspaceService workspaceService)
    {
        _settingsService = settingsService;
        _workspaceService = workspaceService;
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
        ApiKey = settings.ApiKey;
        AutoApprove = settings.AutoApprove;
        TimeoutMinutes = Math.Max(1, settings.TimeoutMinutes);
        MaxRetries = Math.Max(0, settings.MaxRetries);
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
            settings.ApiKey = ApiKey;
            settings.AutoApprove = AutoApprove;
            settings.TimeoutMinutes = Math.Max(1, TimeoutMinutes);
            settings.MaxRetries = Math.Max(0, MaxRetries);

            await _settingsService.SaveAsync(settings);
            SaveConfirmationMessage = $"Settings saved at {DateTime.Now:HH:mm:ss}.";
            HasSavedConfirmation = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>Opens a folder picker and sets the workspace path.</summary>
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

    /// <summary>Restores form values from the currently persisted settings.</summary>
    [RelayCommand]
    private Task ResetAsync() => LoadSettingsAsync();

    private static string ToProviderLabel(string provider) => provider.ToLowerInvariant() switch
    {
        "openai" => "OpenAI",
        "azure-openai" => "Azure OpenAI",
        "anthropic" => "Anthropic",
        "local" => "Local",
        _ => "OpenAI",
    };

    private static string ToProviderValue(string providerLabel) => providerLabel switch
    {
        "OpenAI" => "openai",
        "Azure OpenAI" => "azure-openai",
        "Anthropic" => "anthropic",
        "Local" => "local",
        _ => "openai",
    };
}
