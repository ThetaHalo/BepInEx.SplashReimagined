namespace Lotus.SplashScreen;

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
    public static string LogoPath = "assets/logo.png"; 
    
    /// <summary>
    /// this is the icon which will appear in the taskbar
    /// if not set, it will use the Among Us icon
    /// </summary>
    public static string IconPath = "assets/icon.png";
}