using System.Diagnostics;
using Raylib_cs;

namespace BepInEx.SplashReimagined;

public static class SplashScreenProgram
{
    public const int SplashWidth = 800;
    public const int SplashHeight = 500;

    public static string GameDir = string.Empty;    
    
    [STAThread]
    public static void Main(params string[] args)
    {
        Configuration.ReadConfig();
        
        Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.TransparentWindow | ConfigFlags.TopmostWindow);
        Raylib.InitWindow(SplashWidth, SplashHeight, $"{Configuration.ProjectName} Splash Screen");
        Raylib.SetTargetFPS(60);
        Raylib.InitAudioDevice();
        
        double startedAt = Raylib.GetTime();
        bool connectedToGame = args.Length > 0;
        int timeoutTime = 120;
        int threadTimeout = 60;
        
        double lastLogAt = startedAt;

        try
        {
            Textures.Load();
            bool wasWebsiteHovered = false;
            bool wasDiscordHovered = false;
            bool wasOpenGameDirHovered = false;

            if (connectedToGame)
            {
                ThreadLogReader.Start();
                SetGameDir(args);
            }

            while (!Raylib.WindowShouldClose())
            {
                bool updated = ThreadLogReader.Update();
                double now = Raylib.GetTime();

                if (updated)
                    lastLogAt = now;

                if (connectedToGame)
                {
                    bool openTooLong = now - startedAt >= timeoutTime;
                    bool logsTimedOut = now - lastLogAt >= threadTimeout;

                    if (ThreadLogReader.Closed || StateManager.ShouldClose || openTooLong || logsTimedOut)
                    {
                        break;
                    }
                }
                
                if (connectedToGame && (ThreadLogReader.Closed || StateManager.ShouldClose)) break;
                
                #if DEBUG
                if (!connectedToGame)
                    CheckDebugKeybinds();
                #endif
                
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Blank);

                Utilities.DrawImage(Textures.SplashBackground, 0, 0, SplashWidth, SplashHeight);

                #if !DEBUG
                if (!connectedToGame)
                {
                    Raylib.DrawText($"This is a splash screen for {Configuration.ProjectName}.\n It is not meant to be run directly.\nThe process will exit in 10s, please open {Configuration.GameName}.", 80, 225, 20, Color.Red);
                    Raylib.EndDrawing();
                    if (Raylib.GetTime() - startedAt >= 10)
                        break;

                    continue;
                }
                #endif
                
                // this sucks ass
                float logoX = Math.Max(0, SplashWidth - Textures.AppIcon.Width - 70);
                const float loadingBarTop = 400;
                float logoScale = MathF.Min(
                    (SplashWidth - logoX) / Textures.Logo.Width,
                    loadingBarTop / Textures.Logo.Height) * 0.8f;
                float logoHeight = Textures.Logo.Height * logoScale;

                Utilities.DrawImage(
                    Textures.Logo,
                    logoX,
                    (loadingBarTop - logoHeight) / 2 + 30,
                    Textures.Logo.Width * logoScale,
                    logoHeight);
                // ---

                Utilities.DrawButton(Textures.WebsiteInactive, Textures.WebsiteActive, new Rectangle(70, 40, 90, 90), Configuration.GitHubLink, ref wasWebsiteHovered);
                Utilities.DrawButton(Textures.DiscordInactive, Textures.DiscordActive, new Rectangle(160, 40, 90, 90), Configuration.DiscordLink, ref wasDiscordHovered);
                if (GameDir != string.Empty && Directory.Exists(GameDir)) Utilities.DrawButton(Textures.OpenGameDirInactive, Textures.OpenGameDirActive, new Rectangle(250, 40, 90, 90), GameDir, ref wasOpenGameDirHovered);
                
                Utilities.DrawCheckpoints();
                Utilities.DrawLoadingBar(StateManager.GetLoadingBarProgress());

                Raylib.EndDrawing();
            }
        }
        finally
        {
            Textures.Unload();
            Raylib.CloseAudioDevice();
            Raylib.CloseWindow();
        }
    }
    
    private static void CheckDebugKeybinds()
    {
        #if !DEBUG
        return;
        #endif
        float change = 0.5f * Raylib.GetFrameTime();

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            StateManager.SetLoadingBarProgress(
                StateManager.GetLoadingBarProgress() + change);

        if (Raylib.IsKeyDown(KeyboardKey.Down))
            StateManager.SetLoadingBarProgress(
                StateManager.GetLoadingBarProgress() - change);

        int step = 0;

        if (Raylib.IsKeyPressed(KeyboardKey.Right)) step = 1;
        if (Raylib.IsKeyPressed(KeyboardKey.Left)) step = -1;
        if (step == 0) return;

        int index = Math.Clamp((int)StateManager.CurrentState + step, (int)SplashScreenState.ChainloaderStarted, (int)SplashScreenState.Done);
        var state = (SplashScreenState)index;
        StateManager.SetDebugState(state);
        
        int processed = state switch
        {
            SplashScreenState.ChainloaderStarted => 0,
            SplashScreenState.LoadingPlugins => 1,
            SplashScreenState.Done => 2,
            _ => throw new ArgumentOutOfRangeException()
        };

        StateManager.SetPluginProgress(processed, 2);
    }

    public static void SetGameDir(string[] args)
    {
        var pid = int.Parse(args.Last());
        var gameProcess = Process.GetProcessById(pid);
        
        if (gameProcess == null)
            throw new Exception("Could not find game process. Make sure to launch the game first.");

        GameDir = Path.GetDirectoryName(gameProcess.MainModule?.FileName) ?? throw new Exception("Could not determine game directory.");
    }
    
}