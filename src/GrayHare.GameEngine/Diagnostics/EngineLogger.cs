namespace GrayHare.GameEngine.Diagnostics;

/// <summary>
/// Minimal static logger for engine diagnostics. By default writes to
/// <see cref="System.Diagnostics.Debug.WriteLine(string)"/>; games can
/// replace the handler via <see cref="SetHandler"/>.
/// </summary>
public static class EngineLogger
{
    private static Action<string>? _handler = message => System.Diagnostics.Debug.WriteLine(message);

    /// <summary>Replaces the log handler. Pass <see langword="null"/> to disable logging.</summary>
    public static void SetHandler(Action<string>? handler) => _handler = handler;

    /// <summary>Writes a diagnostic message if a handler is registered.</summary>
    public static void Log(string message) => _handler?.Invoke(message);
}
