using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Workspace Viewer page — browse and preview generated files.</summary>
public sealed partial class WorkspaceViewerViewModel : ObservableObject, IQueryAttributable
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

    [ObservableProperty]
    private string _requestedRelativePath = string.Empty;

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

    /// <summary>Applies incoming query parameters to initialize or update the view model.</summary>
    void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ticketId", out var ticketId))
            TicketId = ticketId?.ToString() ?? string.Empty;
        if (query.TryGetValue("relativePath", out var relativePath))
            RequestedRelativePath = Uri.UnescapeDataString(relativePath?.ToString() ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(TicketId))
            _ = LoadAndSelectFromQueryAsync();
    }

    partial void OnSelectedFileChanged(GeneratedFileItem? value)
    {
        // No-op: selection loading is handled by LoadAndSelectFromQueryAsync
        // to avoid re-entrant calls from SelectFileAsync setting SelectedFile.
    }

    private async Task LoadAndSelectFromQueryAsync()
    {
        await LoadFilesAsync();
        if (string.IsNullOrWhiteSpace(RequestedRelativePath))
            return;

        var matched = GeneratedFiles.FirstOrDefault(file =>
            string.Equals(file.RelativePath, RequestedRelativePath, StringComparison.OrdinalIgnoreCase));
        if (matched is not null)
            await SelectFileAsync(matched);
    }
}
