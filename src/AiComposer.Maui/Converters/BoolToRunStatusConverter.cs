using System.Globalization;

namespace AiComposer.Maui.Converters;

/// <summary>Converts a boolean IsRunning flag to a human-readable run status string.</summary>
public sealed class BoolToRunStatusConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Running…" : "Idle";

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
