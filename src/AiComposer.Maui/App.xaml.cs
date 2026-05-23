namespace AiComposer.Maui;

/// <summary>Root MAUI application entry point.</summary>
public partial class App : Application
{
    /// <summary>Initialises the application and sets the AppShell as the main page.</summary>
    public App(AppShell shell)
    {
        InitializeComponent();
        MainPage = shell;
    }
}
