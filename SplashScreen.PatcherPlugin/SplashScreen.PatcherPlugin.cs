using System.Threading;
using BepInEx.Preloader.Core.Patching;

namespace BepInEx.SplashReimagined;

[PatcherPluginInfo("ThetaHalo.BepInExSplash", "ThetaHalo.BepInExSplash", "1.1.0")]
public class LotusSplashPatcherPlugin : BasePatcher
{
    private static int isInit;
    private BepLogListener listener;

    public override void Initialize()
    {
        if (Interlocked.Exchange(ref isInit, 1) == 1) return;
        SplashManager.Setup();
        listener = BepLogListener.StartListening();
    }
}

