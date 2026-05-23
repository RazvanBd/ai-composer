using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Views;

/// <summary>Run Console page — live CLI output streaming and run control.</summary>
public partial class RunConsolePage : ContentPage
{
    /// <summary>Initialises the Run Console page and sets its ViewModel.</summary>
    public RunConsolePage(RunConsoleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
