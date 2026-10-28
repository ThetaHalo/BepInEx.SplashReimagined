using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Logging;

namespace BepInEx.SplashReimagined;

public class SplashManager
{
    private static readonly ManualLogSource console = Logger.CreateLogSource("SplashScreen"); // we back in javascript now 🔥🔥
    
    private static readonly ConcurrentQueue<string> MessageQueue = new();
    private static Process splashProcess;
    private static bool isFinished = false;
    
    public static void Setup()
    {
        var assemblyLocation = typeof(SplashManager).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? Path.Combine(Paths.PatcherPluginPath, "BepInEx.SplashReimagined");

        var exePath = Path.Combine(assemblyDirectory, "BepInEx.SplashReimagined.exe");

        if (!File.Exists(exePath))
        {
            exePath = Path.Combine(assemblyDirectory, "SplashScreen.exe");
            if (!File.Exists(exePath)) throw new FileNotFoundException($"BepInEx.SplashReimagined.exe AND SplashScreen.exe not found at {exePath} D:");
        }
        
        console.LogDebug("Starting main splash exe at path " + exePath);
        
        var proc = new ProcessStartInfo(exePath, Environment.ProcessId.ToString())
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        
        splashProcess = Process.Start(proc);
        
        new Thread(CommunicationThread) { IsBackground = true }.Start(splashProcess);
    }
    
    public static void SendMessage(string message)
    {
        if (isFinished) return;
        MessageQueue.Enqueue(message);
    }

    private static void CommunicationThread(object processArg)
    {
        var process = (Process)processArg;

        try
        {
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data != null)
                    console.LogError($"[Splash] {args.Data}");
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.StandardInput.AutoFlush = false;

            while (!process.HasExited)
            {
                bool wroteMessages = false;
                while (MessageQueue.TryDequeue(out string? message))
                {
                    if (message != null) 
                        process.StandardInput.WriteLine(message.Replace("\r", "").Replace("\n", " "));
                    wroteMessages = true;
                }

                if (wroteMessages) process.StandardInput.Flush();
                Thread.Sleep(50);
            }
        }
        catch (Exception exception)
        {
            console.LogWarning($"Splash ended: {exception.Message}");
        }
        finally
        {
            splashProcess = null;
            isFinished = true;
            process.Dispose();
        }
    }
}