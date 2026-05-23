using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Views;

/// <summary>Dashboard page — workspace overview and quick actions.</summary>
public partial class DashboardPage : ContentPage
{
    /// <summary>Initialises the Dashboard page and sets its ViewModel.</summary>
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DashboardViewModel vm)
            vm.RefreshCommand.Execute(null);
    }
}
