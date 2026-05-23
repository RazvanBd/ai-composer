using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Dashboard page — overview and quick actions.</summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IArtifactsService _artifactsService;
    private readonly ITicketService _ticketService;

    [ObservableProperty]
    private string _workspacePath = "No workspace open";

    [ObservableProperty]
    private int _artifactCount;

    [ObservableProperty]
    private int _ticketCount;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Initialises <see cref="DashboardViewModel"/>.</summary>
    public DashboardViewModel(
        IWorkspaceService workspaceService,
        IArtifactsService artifactsService,
        ITicketService ticketService)
    {
        _workspaceService = workspaceService;
        _artifactsService = artifactsService;
        _ticketService = ticketService;
    }

    /// <summary>Opens a workspace folder and refreshes the dashboard summary.</summary>
    [RelayCommand]
    private async Task OpenWorkspaceAsync()
    {
        var path = await _workspaceService.OpenWorkspaceAsync();
        if (path is not null)
        {
            WorkspacePath = path;
            await RefreshAsync();
        }
    }

    /// <summary>Refreshes artifact and ticket counts from the current workspace.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var artifacts = await _artifactsService.LoadArtifactsAsync();
            var tickets = await _ticketService.LoadTicketsAsync();
            ArtifactCount = artifacts.Count;
            TicketCount = tickets.Count;
            WorkspacePath = _workspaceService.CurrentWorkspacePath ?? "No workspace open";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
