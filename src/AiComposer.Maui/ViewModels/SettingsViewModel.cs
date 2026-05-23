using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Settings page — read and write application settings.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private string _outputPath = string.Empty;

    [ObservableProperty]
    private string _aiProvider = string.Empty;

    [ObservableProperty]
    private string _aiModel = string.Empty;

    [ObservableProperty]
    private string _cliExecutablePath = string.Empty;

    [ObservableProperty]
    private bool _liveOutput;

    [ObservableProperty]
    private bool _isSaving;

    /// <summary>Initialises <see cref="SettingsViewModel"/>.</summary>
    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>Loads settings from storage into the view model properties.</summary>
    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.LoadAsync();
        WorkspacePath = settings.WorkspacePath;
        OutputPath = settings.OutputPath;
        AiProvider = settings.AiProvider;
        AiModel = settings.AiModel;
        CliExecutablePath = settings.CliExecutablePath;
        LiveOutput = settings.LiveOutput;
    }

    /// <summary>Saves the current view model values to persistent settings.</summary>
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        IsSaving = true;
        try
        {
            var settings = new AppSettings
            {
                WorkspacePath = WorkspacePath,
                OutputPath = OutputPath,
                AiProvider = AiProvider,
                AiModel = AiModel,
                CliExecutablePath = CliExecutablePath,
                LiveOutput = LiveOutput,
            };

            await _settingsService.SaveAsync(settings);
        }
        finally
        {
            IsSaving = false;
        }
    }
}
