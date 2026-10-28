using BepInEx.Logging;

namespace BepInEx.SplashReimagined;

public class BepLogListener: ILogListener
{
    private bool isDisposed = false;
    public LogLevel LogLevelFilter => LogLevel.All;

    public static BepLogListener StartListening()
    {
        var logListener = new BepLogListener();
        Logger.Listeners.Add(logListener);
        return logListener;
    }

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        if (isDisposed) return;
        if (eventArgs.Source.SourceName.Contains("BepInEx") && eventArgs.Data != null)
            SplashManager.SendMessage(eventArgs.Data.ToString()!);
    }
    
    public void Dispose() => isDisposed = true;
}