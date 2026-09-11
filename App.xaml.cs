using System.Configuration;
using System.Data;
using System.Windows;

namespace DigitalMosquito;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        System.IO.File.WriteAllText("debug.log", "App.OnStartup called\n");

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            System.IO.File.AppendAllText("debug.log", "UnhandledException: " + args.ExceptionObject?.ToString() + "\n");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            System.IO.File.AppendAllText("debug.log", "DispatcherUnhandledException: " + args.Exception?.ToString() + "\n");
        };

        base.OnStartup(e);
    }
}

