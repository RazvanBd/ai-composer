using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Tickets page — list tickets with state and run actions.</summary>
public sealed partial class TicketsViewModel : ObservableObject
{
    private readonly ITicketService _ticketService;
    private readonly IRunService _runService;

    [ObservableProperty]
    private ObservableCollection<TicketItem> _tickets = [];

    [ObservableProperty]
    private TicketItem? _selectedTicket;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Initialises <see cref="TicketsViewModel"/>.</summary>
    public TicketsViewModel(ITicketService ticketService, IRunService runService)
    {
        _ticketService = ticketService;
        _runService = runService;
    }

    /// <summary>Loads all tickets from the current workspace.</summary>
    [RelayCommand]
    private async Task LoadTicketsAsync()
    {
        IsLoading = true;
        try
        {
            var items = await _ticketService.LoadTicketsAsync();
            Tickets = new ObservableCollection<TicketItem>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Starts a run for the currently selected ticket.</summary>
    [RelayCommand(CanExecute = nameof(CanRunSelectedTicket))]
    private async Task RunSelectedTicketAsync()
    {
        if (SelectedTicket is null) return;
        await _runService.StartRunAsync(SelectedTicket.Id);
    }

    private bool CanRunSelectedTicket() => SelectedTicket is not null && !_runService.IsRunning;
}
