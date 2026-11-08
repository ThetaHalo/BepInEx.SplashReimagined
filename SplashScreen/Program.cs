using System.Diagnostics;
#if WINDOWS
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#endif
using Raylib_cs;
using Color = Raylib_cs.Color;
#if WINDOWS
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingImage = System.Drawing.Image;
using DrawingRectangle = System.Drawing.Rectangle;
#endif
using Rectangle = Raylib_cs.Rectangle;

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

        bool connectedToGame = args.Length > 0;
#if WINDOWS
        if (IsRunningUnderWine())
        {
            if (connectedToGame)
            {
                ThreadLogReader.Start();
                SetGameDir(args);
            }

            Application.Run(new WineSplashForm(connectedToGame));
            return;
        }
#endif

        Raylib.SetConfigFlags(ConfigFlags.UndecoratedWindow | ConfigFlags.TransparentWindow | ConfigFlags.TopmostWindow);
        Raylib.InitWindow(SplashWidth, SplashHeight, $"{Configuration.ProjectName} Splash Screen");
        Raylib.SetTargetFPS(60);
        Raylib.InitAudioDevice();
        
        double startedAt = Raylib.GetTime();
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
    
#if DEBUG
    private static void CheckDebugKeybinds()
    {
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
#endif

    public static void SetGameDir(string[] args)
    {
        var pid = int.Parse(args.Last());
        var gameProcess = Process.GetProcessById(pid);
        
        if (gameProcess == null)
            throw new Exception("Could not find game process. Make sure to launch the game first.");

        GameDir = Path.GetDirectoryName(gameProcess.MainModule?.FileName) ?? throw new Exception("Could not determine game directory.");
    }

#if WINDOWS
    private static bool IsRunningUnderWine()
    {
        if (!NativeLibrary.TryLoad("ntdll.dll", out nint ntdll))
            return false;

        try
        {
            return NativeLibrary.TryGetExport(ntdll, "wine_get_version", out _);
        }
        finally
        {
            NativeLibrary.Free(ntdll);
        }
    }

    private sealed class WineSplashForm : Form
    {
        private readonly bool connectedToGame;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
        private readonly Dictionary<string, DrawingImage> images = new(StringComparer.OrdinalIgnoreCase);
        private readonly DrawingFont headingFont = new("Consolas", 30, FontStyle.Bold, GraphicsUnit.Pixel);
        private readonly DrawingFont itemFont = new("Consolas", 20, FontStyle.Regular, GraphicsUnit.Pixel);
        private TimeSpan lastLogAt;
        private Point mousePosition;

        public WineSplashForm(bool connectedToGame)
        {
            this.connectedToGame = connectedToGame;
            lastLogAt = stopwatch.Elapsed;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(SplashWidth, SplashHeight);
            BackColor = DrawingColor.FromArgb(1, 0, 1);
            TransparencyKey = BackColor;
            TopMost = true;
            ShowInTaskbar = false;
            KeyPreview = true;
            DoubleBuffered = true;

            foreach (string name in new[]
                     {
                         "SplashBackground.png", "github_inactive.png", "github_active.png",
                         "discord_inactive.png", "discord_active.png", "opengamedir_inactive.png",
                         "opengamedir_active.png", "logo.png", "progressbar.png",
                         "progressbar_mask.png", "checkmark.png", "x.png"
                     })
            {
                string path = Path.Combine(AppContext.BaseDirectory, "assets", name);
                images.Add(name, DrawingImage.FromFile(path));
            }

            timer.Tick += (_, _) =>
            {
                if (connectedToGame)
                {
                    if (ThreadLogReader.Update())
                        lastLogAt = stopwatch.Elapsed;

                    if (ThreadLogReader.Closed || StateManager.ShouldClose ||
                        stopwatch.Elapsed.TotalSeconds >= 120 ||
                        (stopwatch.Elapsed - lastLogAt).TotalSeconds >= 60)
                    {
                        Close();
                        return;
                    }
                }
                else if (stopwatch.Elapsed.TotalSeconds >= 10)
                {
                    Close();
                    return;
                }

                Invalidate();
            };
            timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics graphics = e.Graphics;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            DrawImage(graphics, "SplashBackground.png", 0, 0, SplashWidth, SplashHeight);
            DrawButton(graphics, "github_inactive.png", "github_active.png", 70, 40);
            DrawButton(graphics, "discord_inactive.png", "discord_active.png", 160, 40);
            if (!string.IsNullOrEmpty(GameDir) && Directory.Exists(GameDir))
                DrawButton(graphics, "opengamedir_inactive.png", "opengamedir_active.png", 250, 40);

            using var whiteBrush = new SolidBrush(DrawingColor.White);
            graphics.DrawString($"{Configuration.GameName} is Loading.", headingFont, whiteBrush, 70, 140);
            DrawCheckpoint(graphics, SplashScreenState.ChainloaderStarted, 70, 177);
            DrawCheckpoint(graphics, SplashScreenState.LoadingPlugins, 70, 217);
            DrawCheckpoint(graphics, SplashScreenState.Done, 70, 257);

            const int logoX = 390;
            const int logoSize = 320;
            DrawImage(graphics, "logo.png", logoX, 70, logoSize, logoSize);

            const int barX = 70;
            const int barY = 390;
            DrawImage(graphics, "progressbar.png", barX, barY, 660, 60);
            int fillWidth = (int)(660 * Math.Clamp(StateManager.GetLoadingBarProgress(), 0, 1));
            if (fillWidth > 0)
            {
                GraphicsState state = graphics.Save();
                graphics.SetClip(new DrawingRectangle(barX, barY, fillWidth, 60));
                DrawImage(graphics, "progressbar_mask.png", barX, barY, 660, 60);
                graphics.Restore(state);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            mousePosition = e.Location;
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (new DrawingRectangle(70, 40, 90, 90).Contains(e.Location))
                    OpenLink(Configuration.GitHubLink);
                else if (new DrawingRectangle(160, 40, 90, 90).Contains(e.Location))
                    OpenLink(Configuration.DiscordLink);
                else if (new DrawingRectangle(250, 40, 90, 90).Contains(e.Location) && Directory.Exists(GameDir))
                    Process.Start(new ProcessStartInfo(GameDir) { UseShellExecute = true });
            }

            base.OnMouseClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
            base.OnKeyDown(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                headingFont.Dispose();
                itemFont.Dispose();
                foreach (DrawingImage image in images.Values)
                    image.Dispose();
            }

            base.Dispose(disposing);
        }

        private void DrawCheckpoint(Graphics graphics, SplashScreenState state, int x, int y)
        {
            bool complete = StateManager.IsCheckpointComplete(state);
            DrawImage(graphics, complete ? "checkmark.png" : "x.png", x, y, 27, 27);
            using var brush = new SolidBrush(DrawingColor.White);
            graphics.DrawString(StateManager.GetMessage(state), itemFont, brush, x + 40, y - 2);
        }

        private void DrawButton(Graphics graphics, string inactive, string active, int x, int y)
        {
            string image = new DrawingRectangle(x, y, 90, 90).Contains(mousePosition) ? active : inactive;
            DrawImage(graphics, image, x, y, 90, 90);
        }

        private void DrawImage(Graphics graphics, string name, int x, int y, int width, int height) =>
            graphics.DrawImage(images[name], x, y, width, height);

        private static void OpenLink(string link)
        {
            if (Uri.TryCreate(link, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
#endif
}