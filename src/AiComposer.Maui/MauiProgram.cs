using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using AiComposer.Maui.Services.Abstractions;
using AiComposer.Maui.Services.Implementations;
using AiComposer.Maui.ViewModels;
using AiComposer.Maui.Views;

namespace AiComposer.Maui;

/// <summary>Application bootstrap — configures and builds the MAUI host.</summary>
public static class MauiProgram
{
    /// <summary>Creates and configures the MAUI application.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // -----------------------------------------------------------------------
        // Services — infrastructure
        // -----------------------------------------------------------------------
        builder.Services.AddSingleton<ISettingsService, LocalSettingsService>();
        builder.Services.AddSingleton<ISecureStorageService, PlatformSecureStorageService>();
        builder.Services.AddSingleton<IWorkspaceService, FileWorkspaceService>();
        builder.Services.AddSingleton<IArtifactsService, FileArtifactsService>();
        builder.Services.AddSingleton<ITicketService, FileTicketService>();
        builder.Services.AddSingleton<IRunService, CliRunService>();
        builder.Services.AddSingleton<IOutputService, FileOutputService>();

        // -----------------------------------------------------------------------
        // ViewModels
        // -----------------------------------------------------------------------
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ArtifactsExplorerViewModel>();
        builder.Services.AddTransient<TicketsViewModel>();
        builder.Services.AddTransient<RunConsoleViewModel>();
        builder.Services.AddTransient<WorkspaceViewerViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // -----------------------------------------------------------------------
        // Views / Pages
        // -----------------------------------------------------------------------
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ArtifactsExplorerPage>();
        builder.Services.AddTransient<TicketsPage>();
        builder.Services.AddTransient<RunConsolePage>();
        builder.Services.AddTransient<WorkspaceViewerPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Shell
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
