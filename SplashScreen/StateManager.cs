namespace Lotus.SplashScreen;

public static class StateManager
{
    private static float loadingBarProgress = 0f;
    public static bool ShouldClose = false;
    
    public static int PluginsProcessed { get; private set; }
    public static int PluginCount { get; private set; }
    
    public static Dictionary<SplashScreenState, string> StateToMessage { get; } = new()
    {
        { SplashScreenState.ChainloaderStarted, "Chainloader Initialized" },
        { SplashScreenState.LoadingPlugins, "Loading plugins ({0}/{1})" },
        { SplashScreenState.Done, "Launching the game!" }
    };

    public static Dictionary<SplashScreenState, string> StateToLog { get; } = new()
    {
        { SplashScreenState.ChainloaderStarted, "Chainloader initialized" },
        { SplashScreenState.LoadingPlugins, "plugins to load" },
        { SplashScreenState.Done, "Chainloader startup complete" }
    };
    
    public static SplashScreenState CurrentState { get; private set; } = SplashScreenState.ChainloaderStarted;
    
    
    public static void ProcessLogMessage(string message)
    {
        message = message.Trim();
        
        if (message.Contains("Begin async loading MainMenu"))
        {
            ShouldClose = true;
            return;
        }

        foreach (var entry in StateToLog)
        {
            if (!message.Contains(entry.Value, StringComparison.Ordinal))
                continue;
            
            SetState(entry.Key);

            if (entry.Key == SplashScreenState.LoadingPlugins)
            {
                // "x plugins to load"
                string count = message.Split(' ')[0];

                if (int.TryParse(count, out int total))
                    SetPluginProgress(PluginsProcessed, total);
            }
            else if (entry.Key == SplashScreenState.Done)
            {
                SetPluginProgress(PluginCount, PluginCount);
            }
            break;
        }

        if (message.StartsWith("Loading [", StringComparison.Ordinal) || message.StartsWith("Skipping [", StringComparison.Ordinal))
        {
            SetPluginProgress(PluginsProcessed + 1, PluginCount);
        }
    }
    
    public static float GetLoadingBarProgress() => loadingBarProgress;
    public static void SetLoadingBarProgress(float progress) => loadingBarProgress = Math.Clamp(progress, 0f, 1f);
    private static void UpdateLoadingBarProgress()
    {
        int completed = 0;
        int total = 0;

        foreach (SplashScreenState state in Enum.GetValues<SplashScreenState>())
        {
            if (state == SplashScreenState.Done) continue;
            total++;
            if (IsCheckpointComplete(state)) completed++;
        }
        SetLoadingBarProgress(completed / (float)total);
    }
    
    public static void SetState(SplashScreenState state) 
    {
        if ((int)state > (int)CurrentState) CurrentState = state;
        UpdateLoadingBarProgress();
    }

    public static void SetPluginProgress(int processed, int total)
    {
        PluginCount = Math.Max(0, total);
        PluginsProcessed = Math.Clamp(processed, 0, PluginCount);
        UpdateLoadingBarProgress();
    }

    public static string GetMessage(SplashScreenState state)
    {
        return state == SplashScreenState.LoadingPlugins
            ? string.Format(StateToMessage[state], PluginsProcessed, PluginCount)
            : StateToMessage[state];
    }

    public static bool IsCheckpointComplete(SplashScreenState state)
    {
        int current = (int)CurrentState;
        return state switch
        {
            SplashScreenState.ChainloaderStarted => current >= (int)SplashScreenState.LoadingPlugins,
            SplashScreenState.LoadingPlugins => CurrentState == SplashScreenState.Done,
            SplashScreenState.Done => current >= (int)SplashScreenState.Done,
            _ => false
        };
    }
    
    public static void SetDebugState(SplashScreenState state)
    {
        CurrentState = state;
        UpdateLoadingBarProgress();
    }
}

public enum SplashScreenState
{
    ChainloaderStarted,
    LoadingPlugins,
    Done
}