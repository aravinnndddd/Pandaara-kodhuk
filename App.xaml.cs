using System;
using System.IO;
using System.Windows;

namespace DigitalMosquito;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            File.WriteAllText(LogPath, $"[{DateTime.UtcNow:O}] App.OnStartup called\n");
        }
        catch { }

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.UtcNow:O}] UnhandledException: {args.ExceptionObject}\n");
            }
            catch { }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.UtcNow:O}] DispatcherUnhandledException: {args.Exception}\n");
            }
            catch { }
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.UtcNow:O}] App.OnExit called with code: {e.ApplicationExitCode}\n");
        }
        catch { }
        base.OnExit(e);
    }
}
