using Avalonia;
using Runtime.UI.Hud;

namespace Editor
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            HudEngine.Initialize();

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                HudEngine.Shutdown();
            }
        }

        private static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}
