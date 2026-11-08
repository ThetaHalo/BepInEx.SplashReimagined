using System.Diagnostics;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace BepInEx.SplashReimagined;

public static unsafe class WineSplash
{
    private const int Width = SplashScreenProgram.SplashWidth;
    private const int Height = SplashScreenProgram.SplashHeight;
    private const int ButtonSize = 90;
    private const int ButtonY = 40;
    private const int BarX = 70;
    private const int BarY = 390;
    private const int BarWidth = 660;
    private const int BarHeight = 60;
    private const string ClassName = "BepInExSplashReimagined";

    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    private static readonly Sprite Background = Load("SplashBackground.png", Width, Height);
    private static readonly Sprite[] GitHub = LoadPair("github_inactive.png", "github_active.png");
    private static readonly Sprite[] Discord = LoadPair("discord_inactive.png", "discord_active.png");
    private static readonly Sprite[] GameDirectory = LoadPair("opengamedir_inactive.png", "opengamedir_active.png");
    private static readonly Sprite Logo = Load("logo.png", 270, 270);
    private static readonly Sprite Bar = Load("progressbar.png", BarWidth, BarHeight);
    private static readonly Sprite BarMask = Load("progressbar_mask.png", BarWidth, BarHeight);
    private static readonly Sprite Checkmark = Load("checkmark.png", 27, 27);
    private static readonly Sprite Cross = Load("x.png", 27, 27);

    private static TimeSpan lastLogAt;
    private static bool connectedToGame;
    private static nint window;
    private static nint frameDc;
    private static nint textDc;
    private static nint headingFont;
    private static nint itemFont;
    private static uint* frameBits;
    private static uint* textBits;
    private static NativePoint position;

    public static void Run(bool connected)
    {
        connectedToGame = connected;

        frameDc = CreateSurface(out frameBits);
        textDc = CreateSurface(out textBits);
        SetBkMode(textDc, 1);
        SetTextColor(textDc, 0xFFFFFF);
        headingFont = CreateFontW(-25, 0, 0, 0, 700, 0, 0, 0, 1, 0, 0, 4, 0, "Arial");
        itemFont = CreateFontW(-18, 0, 0, 0, 400, 0, 0, 0, 1, 0, 0, 4, 0, "Arial");

        nint instance = GetModuleHandleW(null);
        fixed (char* className = ClassName)
        {
            var windowClass = new WindowClass
            {
                Size = (uint)sizeof(WindowClass),
                WndProc = &WindowProc,
                Instance = instance,
                Cursor = LoadCursorW(0, 32512),
                ClassName = (nint)className
            };
            RegisterClassExW(ref windowClass);
        }

        position = new NativePoint { X = (GetSystemMetrics(0) - Width) / 2, Y = (GetSystemMetrics(1) - Height) / 2 };
        window = CreateWindowExW(0x80088, ClassName, ClassName, 0x80000000, position.X, position.Y, Width, Height, 0, 0, instance, 0);

        Render();
        ShowWindow(window, 5);
        SetTimer(window, 1, 33, 0);

        while (GetMessageW(out Msg message, 0, 0, 0) > 0)
            DispatchMessageW(ref message);
    }

    [UnmanagedCallersOnly]
    private static nint WindowProc(nint hwnd, uint message, nint wParam, nint lParam)
    {
        switch (message)
        {
            case 0x113:
                Tick();
                return 0;
            case 0x201:
                Click((short)lParam, (short)(lParam >> 16));
                return 0;
            case 0x100 when wParam == 0x1B:
                DestroyWindow(hwnd);
                return 0;
            case 0x2:
                PostQuitMessage(0);
                return 0;
            default:
                return DefWindowProcW(hwnd, message, wParam, lParam);
        }
    }

    private static void Tick()
    {
        TimeSpan elapsed = Stopwatch.Elapsed;

        if (connectedToGame)
        {
            if (ThreadLogReader.Update())
                lastLogAt = elapsed;

            if (ThreadLogReader.Closed || StateManager.ShouldClose ||
                elapsed.TotalSeconds >= 120 || (elapsed - lastLogAt).TotalSeconds >= 60)
            {
                DestroyWindow(window);
                return;
            }
        }
        else if (elapsed.TotalSeconds >= 10)
        {
            DestroyWindow(window);
            return;
        }

        Render();
    }

    private static void Render()
    {
        GetCursorPos(out NativePoint cursor);
        ScreenToClient(window, ref cursor);

        Span<uint> frame = new(frameBits, Width * Height);
        Background.Pixels.CopyTo(frame);

        DrawButton(frame, GitHub, 70, cursor);
        DrawButton(frame, Discord, 160, cursor);
        if (Directory.Exists(SplashScreenProgram.GameDir))
            DrawButton(frame, GameDirectory, 250, cursor);

        new Span<uint>(textBits, Width * Height).Clear();
        DrawText($"{Configuration.GameName} is Loading.", headingFont, 70, 145, 350, 32);

        int y = 190;
        foreach (SplashScreenState state in Enum.GetValues<SplashScreenState>())
        {
            Blend(frame, StateManager.IsCheckpointComplete(state) ? Checkmark : Cross, 70, y);
            DrawText(StateManager.GetMessage(state), itemFont, 110, y - 1, 310, 29);
            y += 42;
        }

        GdiFlush();
        Overlay(frame, new ReadOnlySpan<uint>(textBits, Width * Height));

        Blend(frame, Logo, 440, 85);
        Blend(frame, Bar, BarX, BarY);
        Blend(frame, BarMask, BarX, BarY, (int)(BarWidth * Math.Clamp(StateManager.GetLoadingBarProgress(), 0f, 1f)));

        var size = new NativeSize { Cx = Width, Cy = Height };
        var origin = new NativePoint();
        var blend = new BlendFunction { Alpha = 255, Format = 1 };
        UpdateLayeredWindow(window, 0, ref position, ref size, frameDc, ref origin, 0, ref blend, 2);
    }

    private static void DrawButton(Span<uint> frame, Sprite[] sprites, int x, NativePoint cursor) =>
        Blend(frame, sprites[Hit(x, cursor.X, cursor.Y) ? 1 : 0], x, ButtonY);

    private static bool Hit(int x, int pointX, int pointY) =>
        pointX >= x && pointX < x + ButtonSize && pointY >= ButtonY && pointY < ButtonY + ButtonSize;

    private static void Click(int x, int y)
    {
        if (Hit(70, x, y))
            OpenLink(Configuration.GitHubLink);
        else if (Hit(160, x, y))
            OpenLink(Configuration.DiscordLink);
        else if (Hit(250, x, y) && Directory.Exists(SplashScreenProgram.GameDir))
            Process.Start(new ProcessStartInfo(SplashScreenProgram.GameDir) { UseShellExecute = true });
    }

    private static void OpenLink(string link)
    {
        if (Uri.TryCreate(link, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private static void DrawText(string text, nint font, int x, int y, int width, int height)
    {
        var rect = new NativeRect { Left = x, Top = y, Right = x + width, Bottom = y + height };
        SelectObject(textDc, font);
        DrawTextW(textDc, text, -1, ref rect, 0x8820);
    }

    private static void Blend(Span<uint> frame, Sprite sprite, int x, int y, int visibleWidth = int.MaxValue)
    {
        int columns = Math.Min(sprite.Width, visibleWidth);

        for (int row = 0; row < sprite.Height; row++)
        {
            int source = row * sprite.Width;
            int destination = (y + row) * Width + x;

            for (int column = 0; column < columns; column++)
            {
                uint pixel = sprite.Pixels[source + column];
                if (pixel >> 24 == 0) continue;
                frame[destination + column] = Over(pixel, frame[destination + column]);
            }
        }
    }

    private static void Overlay(Span<uint> frame, ReadOnlySpan<uint> text)
    {
        for (int i = 0; i < frame.Length; i++)
        {
            uint coverage = (text[i] >> 8) & 255;
            if (coverage == 0) continue;
            frame[i] = Over(coverage * 0x01010101, frame[i]);
        }
    }

    private static uint Over(uint source, uint destination)
    {
        uint inverse = 255 - (source >> 24);
        uint alpha = (source >> 24) + (destination >> 24) * inverse / 255;
        uint red = ((source >> 16) & 255) + ((destination >> 16) & 255) * inverse / 255;
        uint green = ((source >> 8) & 255) + ((destination >> 8) & 255) * inverse / 255;
        uint blue = (source & 255) + (destination & 255) * inverse / 255;
        return alpha << 24 | red << 16 | green << 8 | blue;
    }

    private static nint CreateSurface(out uint* bits)
    {
        var info = new BitmapInfo { Size = 40, BiWidth = Width, BiHeight = -Height, Planes = 1, BitCount = 32 };
        nint dc = CreateCompatibleDC(0);
        nint bitmap = CreateDIBSection(dc, ref info, 0, out void* pixels, 0, 0);
        SelectObject(dc, bitmap);
        bits = (uint*)pixels;
        return dc;
    }

    private static Sprite[] LoadPair(string inactive, string active) =>
        [Load(inactive, ButtonSize, ButtonSize), Load(active, ButtonSize, ButtonSize)];

    private static Sprite Load(string filename, int width, int height)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "assets", filename);
        using Stream stream = File.Exists(path)
            ? File.OpenRead(path)
            : typeof(WineSplash).Assembly.GetManifestResourceStream($"Assets/{filename}")
                ?? throw new FileNotFoundException($"Embedded texture not found: {filename}");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        Raylib_cs.Image image = Raylib.LoadImageFromMemory(".png", memory.ToArray());
        Raylib.ImageFormat(ref image, PixelFormat.UncompressedR8G8B8A8);
        if (image.Width != width || image.Height != height)
            Raylib.ImageResize(ref image, width, height);

        var pixels = new uint[width * height];
        var data = (byte*)image.Data;
        for (int i = 0; i < pixels.Length; i++)
        {
            uint red = data[i * 4];
            uint green = data[i * 4 + 1];
            uint blue = data[i * 4 + 2];
            uint alpha = data[i * 4 + 3];
            pixels[i] = alpha << 24 | (red * alpha / 255) << 16 | (green * alpha / 255) << 8 | (blue * alpha / 255);
        }

        Raylib.UnloadImage(image);
        return new Sprite(pixels, width, height);
    }

    private sealed record Sprite(uint[] Pixels, int Width, int Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int Cx;
        public int Cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlendFunction
    {
        public byte Op;
        public byte Flags;
        public byte Alpha;
        public byte Format;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public uint Size;
        public int BiWidth;
        public int BiHeight;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ColorsUsed;
        public uint ColorsImportant;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public nint Hwnd;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowClass
    {
        public uint Size;
        public uint Style;
        public delegate* unmanaged<nint, uint, nint, nint, nint> WndProc;
        public int ClassExtra;
        public int WindowExtra;
        public nint Instance;
        public nint Icon;
        public nint Cursor;
        public nint Brush;
        public nint MenuName;
        public nint ClassName;
        public nint SmallIcon;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? name);

    [DllImport("user32.dll")]
    private static extern ushort RegisterClassExW(ref WindowClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll")]
    private static extern nint LoadCursorW(nint instance, nint name);

    [DllImport("user32.dll")]
    private static extern int ShowWindow(nint hwnd, int command);

    [DllImport("user32.dll")]
    private static extern nint SetTimer(nint hwnd, nint id, uint elapse, nint callback);

    [DllImport("user32.dll")]
    private static extern int GetMessageW(out Msg message, nint hwnd, uint min, uint max);

    [DllImport("user32.dll")]
    private static extern nint DispatchMessageW(ref Msg message);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProcW(nint hwnd, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern int DestroyWindow(nint hwnd);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern int GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    private static extern int ScreenToClient(nint hwnd, ref NativePoint point);

    [DllImport("user32.dll")]
    private static extern int UpdateLayeredWindow(nint hwnd, nint destinationDc, ref NativePoint destination, ref NativeSize size, nint sourceDc, ref NativePoint source, uint colorKey, ref BlendFunction blend, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int DrawTextW(nint dc, string text, int length, ref NativeRect rect, uint format);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint dc);

    [DllImport("gdi32.dll")]
    private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out void* bits, nint section, uint offset);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint dc, nint obj);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(nint dc, int mode);

    [DllImport("gdi32.dll")]
    private static extern uint SetTextColor(nint dc, uint color);

    [DllImport("gdi32.dll")]
    private static extern int GdiFlush();

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);
}
