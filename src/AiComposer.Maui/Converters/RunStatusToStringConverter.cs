using System.Globalization;
using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Converters;

/// <summary>Converts a <see cref="RunStatus"/> enum value to a human-readable display string.</summary>
public sealed class RunStatusToStringConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is RunStatus status
            ? status switch
            {
                RunStatus.Idle      => "Idle",
                RunStatus.Running   => "Running…",
                RunStatus.Completed => "Completed",
                RunStatus.Failed    => "Failed",
                RunStatus.Stopped   => "Stopped",
                _                   => "Unknown",
            }
            : "Unknown";

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
