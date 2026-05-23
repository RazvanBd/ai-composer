using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Workspace Viewer page — browse and preview generated files.</summary>
public sealed partial class WorkspaceViewerViewModel : ObservableObject
{
    private readonly IOutputService _outputService;

    [ObservableProperty]
    private ObservableCollection<GeneratedFileItem> _generatedFiles = [];

    [ObservableProperty]
    private GeneratedFileItem? _selectedFile;

    [ObservableProperty]
    private string _fileContent = string.Empty;

    [ObservableProperty]
    private string _ticketId = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Initialises <see cref="WorkspaceViewerViewModel"/>.</summary>
    public WorkspaceViewerViewModel(IOutputService outputService)
    {
        _outputService = outputService;
    }

    /// <summary>Lists generated files for the configured ticket ID.</summary>
    [RelayCommand]
    private async Task LoadFilesAsync()
    {
        IsLoading = true;
        try
        {
            var files = await _outputService.ListGeneratedFilesAsync(TicketId);
            GeneratedFiles = new ObservableCollection<GeneratedFileItem>(files);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Reads and displays the content of the selected file.</summary>
    [RelayCommand]
    private async Task SelectFileAsync(GeneratedFileItem file)
    {
        SelectedFile = file;
        FileContent = await _outputService.ReadFileContentAsync(file.FullPath);
    }
}
