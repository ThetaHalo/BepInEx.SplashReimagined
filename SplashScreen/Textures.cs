using Raylib_cs;

namespace BepInEx.SplashReimagined;

// insane lie for class name, this has sounds asw, not just textures
public static class Textures
{
    public static Texture2D SplashBackground { get; private set; }
    public static Texture2D Checkmark { get; private set; }
    public static Texture2D DiscordActive { get; private set; }
    public static Texture2D DiscordInactive { get; private set; }
    public static Texture2D Progressbar { get; private set; }
    public static Texture2D ProgressbarMask { get; private set; }
    public static Texture2D WebsiteActive { get; private set; }
    public static Texture2D WebsiteInactive { get; private set; }
    public static Texture2D OpenGameDirActive { get; private set; }
    public static Texture2D OpenGameDirInactive { get; private set; }
    public static Texture2D X { get; private set; }
    public static Texture2D AppIcon { get; private set; }
    public static Texture2D Logo { get; private set; }
    
    public static Sound ClickSound { get; private set; }
    
    public static Sound HoverSound { get; private set; }

    public static void Load()
    {
        SplashBackground = LoadTexture("SplashBackground.png");
        Checkmark = LoadTexture("checkmark.png");
        DiscordActive = LoadTexture("discord_active.png");
        DiscordInactive = LoadTexture("discord_inactive.png");
        Progressbar = LoadTexture("progressbar.png");
        ProgressbarMask = LoadTexture("progressbar_mask.png");
        WebsiteActive = LoadTexture("github_active.png");
        WebsiteInactive = LoadTexture("github_inactive.png");
        OpenGameDirActive = LoadTexture("opengamedir_active.png");
        OpenGameDirInactive = LoadTexture("opengamedir_inactive.png");
        X = LoadTexture("x.png");
        AppIcon = GetIcon(Configuration.IconPath);
        Logo = GetLogo(Configuration.LogoPath);
        
        ClickSound = LoadSoundAsset("click.wav");
        HoverSound = LoadSoundAsset("hover.wav");
    }

    private static Texture2D LoadTexture(string filename, bool isIcon = false, bool isEmbedded = true)
    {
        Stream stream;
        if (isEmbedded)
        {
            stream = typeof(Textures).Assembly.GetManifestResourceStream($"Assets/{filename}")
                ?? throw new FileNotFoundException($"Embedded texture not found: {filename}");
        }
        else
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException($"Texture file not found: {filename}");

            stream = File.OpenRead(filename);
        }

        using (stream)
        {
            using var memory = new MemoryStream();
            stream.CopyTo(memory);

            byte[] bytes = memory.ToArray();
            Image image = Raylib.LoadImageFromMemory(".png", bytes);

            if (isIcon)
            {
                Raylib.ImageFormat(ref image, PixelFormat.UncompressedR8G8B8A8);
                Raylib.SetWindowIcon(image);
            }

            try
            {
                return Raylib.LoadTextureFromImage(image);
            }
            finally
            {
                Raylib.UnloadImage(image);
            }
        }
    }
    
    public static Sound LoadSoundAsset(string filename)
    {
        var assembly = typeof(Textures).Assembly;
        using var stream = assembly.GetManifestResourceStream($"Assets/{filename}") ?? throw new FileNotFoundException($"Embedded sound not found: {filename}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        byte[] bytes = memory.ToArray();
        Wave wave = Raylib.LoadWaveFromMemory(Path.GetExtension(filename), bytes);

        try {return Raylib.LoadSoundFromWave(wave);}
        finally { Raylib.UnloadWave(wave); }
    }

    public static void Unload()
    {
        foreach (Texture2D texture in 
            new[] { 
                SplashBackground, Checkmark, DiscordActive, DiscordInactive, 
                    Progressbar, ProgressbarMask, WebsiteActive, WebsiteInactive, X,
                    OpenGameDirActive, OpenGameDirInactive, AppIcon
            })
        {
            Raylib.UnloadTexture(texture);
        }
        
        Raylib.UnloadSound(ClickSound);
        Raylib.UnloadSound(HoverSound);
    }

    private static Texture2D GetLogo(string path)
    {
        if (path.Contains("default/logo.png"))
        {
            return LoadTexture("logo.png");
        }
        
        path = path.Replace("/", Path.DirectorySeparatorChar.ToString()).Replace("\\", Path.DirectorySeparatorChar.ToString());
        
        path = Configuration.GetBasePath() + Path.DirectorySeparatorChar + path;
        
        if (!File.Exists(path))
        {
            Console.WriteLine($"[SplashScreen] Logo file not found at {path}. Using default logo.");
            return LoadTexture("logo.png");
        }
        
        return LoadTexture(path, isEmbedded: false);
    }
    
    private static Texture2D GetIcon(string path)
    {
        if (path.Contains("default/icon.png"))
        {
            return LoadTexture("icon.png", isIcon: true);
        }
        
        path = path.Replace("/", Path.DirectorySeparatorChar.ToString()).Replace("\\", Path.DirectorySeparatorChar.ToString());
        
        path = Configuration.GetBasePath() + Path.DirectorySeparatorChar + path;
        
        if (!File.Exists(path))
        {
            Console.WriteLine($"[SplashScreen] Icon file not found at {path}. Using default icon.");
            return LoadTexture("icon.png", isIcon: true);
        }

        return LoadTexture(path, isIcon: true, isEmbedded: false);
    }
}