using System.Collections.Specialized;
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

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is RunConsoleViewModel vm)
            vm.OutputLines.CollectionChanged += OnOutputLinesChanged;
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is RunConsoleViewModel vm)
            vm.OutputLines.CollectionChanged -= OnOutputLinesChanged;
    }

    private void OnOutputLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        if (BindingContext is not RunConsoleViewModel vm) return;

        var last = vm.OutputLines.LastOrDefault();
        if (last is not null)
            LogsCollectionView.ScrollTo(last, animate: false);
    }
}
