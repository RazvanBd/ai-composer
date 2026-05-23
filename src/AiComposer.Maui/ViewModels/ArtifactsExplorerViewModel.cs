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
    private ObservableCollection<ArtifactGroup> _groupedArtifacts = [];

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

    /// <summary>Loads and validates all artifacts from the current workspace, grouped by type.</summary>
    [RelayCommand]
    private async Task LoadArtifactsAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _artifactsService.ValidateAsync();
            GroupedArtifacts = BuildGroups(items);
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

    /// <summary>Called when <see cref="SelectedArtifact"/> changes — loads preview content.</summary>
    partial void OnSelectedArtifactChanged(ArtifactItem? value)
    {
        if (value is not null)
            _ = SelectArtifactAsync(value);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static ObservableCollection<ArtifactGroup> BuildGroups(IReadOnlyList<ArtifactItem> items)
    {
        var ordered = items
            .GroupBy(a => GetGroupTitle(a.Type))
            .OrderBy(g => GroupOrder(g.Key))
            .Select(g => new ArtifactGroup(g.Key, g));

        return new ObservableCollection<ArtifactGroup>(ordered);
    }

    private static string GetGroupTitle(string type) => type.ToLowerInvariant() switch
    {
        "project_summary" => "Project Summary",
        "epic"            => "Epics",
        "rule"            => "Business Rules",
        "ticket"          => "Tickets",
        "adr"             => "ADRs",
        _                 => "Other",
    };

    private static int GroupOrder(string groupTitle) => groupTitle switch
    {
        "Project Summary" => 0,
        "Epics"           => 1,
        "Business Rules"  => 2,
        "Tickets"         => 3,
        "ADRs"            => 4,
        _                 => 99,
    };
}
