using System.Globalization;

namespace AiComposer.Maui.Converters;

/// <summary>Converts a ticket lifecycle state string to a background colour for the status badge.</summary>
public sealed class TicketStatusToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString()?.ToLowerInvariant() switch
        {
            "ready"                     => Color.FromArgb("#2563EB"),
            "running"                   => Color.FromArgb("#D97706"),
            "done"                      => Color.FromArgb("#16A34A"),
            "blocked"                   => Color.FromArgb("#DC2626"),
            "paused_human_intervention" => Color.FromArgb("#DC2626"),
            _                           => Color.FromArgb("#6B7280"),
        };

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
