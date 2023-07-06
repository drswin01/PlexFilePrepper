using Serilog;
using System;
using System.Windows;

namespace Prepper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void AppStartup(object sender, StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/Prepper.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Information($"Prepper openning");
            MainWindow = new MainWindow();
            try
            {
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error during application runtime");
            }
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            Log.Information($"Prepper closing");
            Log.CloseAndFlush();
        }
    }
}
