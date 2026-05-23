using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Views;

/// <summary>Artifacts Explorer page — browse and preview workspace artifacts.</summary>
public partial class ArtifactsExplorerPage : ContentPage
{
    /// <summary>Initialises the Artifacts Explorer page and sets its ViewModel.</summary>
    public ArtifactsExplorerPage(ArtifactsExplorerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ArtifactsExplorerViewModel vm)
            vm.LoadArtifactsCommand.Execute(null);
    }
}
