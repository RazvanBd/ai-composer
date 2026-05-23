using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Views;

/// <summary>Workspace Viewer page — browse and preview generated output files.</summary>
public partial class WorkspaceViewerPage : ContentPage
{
    /// <summary>Initialises the Workspace Viewer page and sets its ViewModel.</summary>
    public WorkspaceViewerPage(WorkspaceViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
