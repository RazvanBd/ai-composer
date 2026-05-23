using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Artifacts Explorer page — list and preview artifacts.</summary>
public sealed partial class ArtifactsExplorerViewModel : ObservableObject
{
    private readonly IArtifactsService _artifactsService;

    [ObservableProperty]
    private ObservableCollection<ArtifactItem> _artifacts = [];

    [ObservableProperty]
    private ArtifactItem? _selectedArtifact;

    [ObservableProperty]
    private string _selectedContent = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Initialises <see cref="ArtifactsExplorerViewModel"/>.</summary>
    public ArtifactsExplorerViewModel(IArtifactsService artifactsService)
    {
        _artifactsService = artifactsService;
    }

    /// <summary>Loads all artifacts from the current workspace.</summary>
    [RelayCommand]
    private async Task LoadArtifactsAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _artifactsService.LoadArtifactsAsync();
            Artifacts = new ObservableCollection<ArtifactItem>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Loads the content of the selected artifact for preview.</summary>
    [RelayCommand]
    private async Task SelectArtifactAsync(ArtifactItem artifact)
    {
        SelectedArtifact = artifact;
        SelectedContent = await _artifactsService.ReadArtifactContentAsync(artifact.FilePath);
    }
}
