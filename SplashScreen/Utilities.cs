using System.Diagnostics;
using System.Numerics;
using Raylib_cs;

namespace Lotus.SplashScreen;

public static class Utilities
{
    private static float displayedBarProgress;
    
    private static readonly SplashScreenState[] Checkpoints =
    {
        SplashScreenState.ChainloaderStarted,
        SplashScreenState.LoadingPlugins,
        SplashScreenState.Done
    };
    
    public static void DrawImage(Texture2D texture, float x, float y, float width, float height, bool flip = false)
    {
        Rectangle source = new(0, 0, flip ? -texture.Width : texture.Width, texture.Height);
        Rectangle destination = new(x, y, width, height);
        Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0, Color.White);
    }

    public static void ScaleImage(Texture2D texture, float x, float y, float maxWidth, float maxHeight, bool flip = false)
    {
        float scale = MathF.Min(
            1f,
            MathF.Min(
                maxWidth / texture.Width,
                maxHeight / texture.Height));

        DrawImage(texture, x, y, texture.Width * scale, texture.Height * scale, flip);
    }
    
    public static void DrawButton(Texture2D inactive, Texture2D active, Rectangle bounds, string url, ref bool wasHovered)
    {
        bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), bounds);
        Texture2D texture = hovered ? active : inactive;
        Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, texture.Height), bounds, Vector2.Zero, 0, Color.White);
        
        if (hovered && !wasHovered)
            Raylib.PlaySound(Textures.HoverSound);

        wasHovered = hovered;

        if (!hovered || !Raylib.IsMouseButtonPressed(MouseButton.Left)) return;
        Raylib.PlaySound(Textures.ClickSound);
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    
    public static void DrawLoadingBar(float progress)
    {
        const int barWidth = 800;
        const int barHeight = 500;

        int x = (LotusSplashScreen.SplashWidth - barWidth) / 2;
        int y = LotusSplashScreen.SplashHeight - barHeight;

        progress = Math.Clamp(progress, 0f, 1f);
        
        // lerping looks more cool than just cutting
        float blend = 1f - MathF.Exp(-8f * Raylib.GetFrameTime());
        displayedBarProgress += (progress - displayedBarProgress) * blend;

        if (MathF.Abs(progress - displayedBarProgress) < 0.001f)
            displayedBarProgress = progress;

        DrawImage(Textures.Progressbar, x, y, barWidth, barHeight);

        int width = (int)(barWidth * displayedBarProgress);

        if (width > 0)
        {
            Raylib.BeginScissorMode(x, y, width, barHeight);
            DrawImage(Textures.ProgressbarMask, x, y, barWidth, barHeight);
            Raylib.EndScissorMode();
        }
    }
    
    public static void DrawCheckpoints()
    {
        for (int i = 0; i < Checkpoints.Length + 1; i++)
        {
            if (i == 0)
            {
                int y = 140;
                Raylib.DrawText($"{Configuration.GameName} is Loading.", 70, y, 30, Color.White);
            }
            else
            {
                SplashScreenState state = Checkpoints[i - 1];
                int y = 140 + i * 40;
            
                Texture2D icon = StateManager.IsCheckpointComplete(state) ? Textures.Checkmark : Textures.X;
                DrawImage(icon, 70, y - 3, 27, 27);
                Raylib.DrawText(StateManager.GetMessage(state), 110, y, 20, Color.White);
            }
        }
    }
}