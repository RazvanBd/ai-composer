using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Views;

/// <summary>Settings page — configure workspace paths, AI provider, model and CLI integration.</summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>Initialises the Settings page and sets its ViewModel.</summary>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsViewModel vm)
            vm.LoadSettingsCommand.Execute(null);
    }
}
