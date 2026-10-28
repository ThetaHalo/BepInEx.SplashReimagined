namespace BepInEx.SplashReimagined;

public static class Configuration
{
    /// <summary>
    /// Set this to your project's name. ex: "MyMod"
    /// </summary>
    public static string ProjectName = "BepInEx.SplashReimagined";
    
    /// <summary>
    /// The name of the game which this splash screen is for. ex: "Among Us"
    /// This is used in the loading text on the splash screen.
    /// </summary>
    public static string GameName = "Among Us";
    
    /// <summary>
    /// Set this to your project's discord link. ex: "https://discord.gg/example""
    /// </summary>
    public static string DiscordLink = "https://discord.gg/example";
    
    /// <summary>
    /// Set this to your project's Website/GitHub link. ex: "https://github.com/ThetaHalo/bepinex.splashreimagined"
    /// </summary>
    public static string GitHubLink = "https://github.com/ThetaHalo/BepInEx.SplashReimagined";

    /// <summary>
    /// set this to your project's icon relative path. ex: "Assets/icon.png"
    /// this is used by the logo on the splash screen.
    /// if not set, it will use the default icon.
    /// </summary>
    public static string LogoPath = DefaultLogoPath; 
    public const string DefaultLogoPath = "default/logo.png";
    
    /// <summary>
    /// this is the icon which will appear in the taskbar
    /// if not set, it will use the Among Us icon
    /// </summary>
    public static string IconPath = DefaultIconPath;
    public const string DefaultIconPath = "default/icon.png";
    
    public static string GetAssetsPath()
    {
        var basePath = GetBasePath();
        return Path.Combine(basePath, "assets");
    }
    
    public static string GetBasePath()
    {
        return Directory.GetCurrentDirectory();
    }
    
    public static void ReadConfig()
    {
        string basePath = GetBasePath();
        string assetsPath = GetAssetsPath();
        FileInfo cfg = new FileInfo(Path.Combine(basePath, "splash.cfg"));
        
        if (!Directory.Exists(assetsPath))
        {
            Console.WriteLine($"[SplashScreen] No assets folder found at {assetsPath}. Creating it now.");
            Directory.CreateDirectory(assetsPath);
        }
        
        if (!cfg.Exists)
        {
            Console.WriteLine($"[SplashScreen] No config file found at {cfg.FullName}. Writing default cfg and using default values.");
            WriteDefaultConfig();
            return;
        }
        
        string[] lines = File.ReadAllLines(cfg.FullName);
        foreach (string line in lines)
        {
            if (line.StartsWith("ProjectName=")) ProjectName = line.Substring("ProjectName=".Length);
            else if (line.StartsWith("GameName=")) GameName = line.Substring("GameName=".Length);
            else if (line.StartsWith("DiscordLink=")) DiscordLink = line.Substring("DiscordLink=".Length);
            else if (line.StartsWith("GitHubLink=")) GitHubLink = line.Substring("GitHubLink=".Length);
            else if (line.StartsWith("LogoPath=")) LogoPath = line.Substring("LogoPath=".Length);
            else if (line.StartsWith("IconPath=")) IconPath = line.Substring("IconPath=".Length);
        }
    }
    
    // w scuff
    public static void WriteDefaultConfig()
    {
        string basePath = Directory.GetCurrentDirectory();
        FileInfo cfg = new FileInfo(Path.Combine(basePath, "splash.cfg"));
        
        if (cfg.Exists)
            return;
        
        using StreamWriter writer = new StreamWriter(cfg.FullName);
        writer.WriteLine("# This is the configuration file for the splash screen.");
        writer.WriteLine("# You can change the values below to customize the splash screen.");
        writer.WriteLine("# For custom image paths, use the `assets` folder in the splash screen's directory.");
        writer.WriteLine("# --------\n");
        writer.WriteLine("# Set this to your project's name. ex: \"MyMod\"");
        writer.WriteLine($"ProjectName={ProjectName}");
        writer.WriteLine("# Set this to the name of the game which this splash screen is for. ex: \"Among Us\"");
        writer.WriteLine("# This is used in the loading text on the splash screen.");
        writer.WriteLine($"GameName={GameName}");
        writer.WriteLine("# Set this to your project's discord link. ex: \"https://discord.gg/example\"");
        writer.WriteLine($"DiscordLink={DiscordLink}");
        writer.WriteLine("# Set this to your project's Website/GitHub link. ex: \"https://github.com/example\"");
        writer.WriteLine($"GitHubLink={GitHubLink}");
        writer.WriteLine("# Set this to the path of your project's logo. ex: \"logo.png\"\nHighly recommended to put this in the assets folder.");
        writer.WriteLine("# This is the logo which will appear on the splash screen. Recommended to use a transparent image or else it'll look weird.");
        writer.WriteLine($"LogoPath={LogoPath}");
        writer.WriteLine("# Set this to the path of your project's icon. ex: \"icon.png\"\n# Highly recommended to put this in the assets folder.");
        writer.WriteLine("# This is the icon which will appear in the taskbar.");
        writer.WriteLine($"IconPath={IconPath}");
    }
}