using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Views;

/// <summary>Tickets page — list tickets with state badges and run actions.</summary>
public partial class TicketsPage : ContentPage
{
    /// <summary>Initialises the Tickets page and sets its ViewModel.</summary>
    public TicketsPage(TicketsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TicketsViewModel vm)
            vm.LoadTicketsCommand.Execute(null);
    }
}
