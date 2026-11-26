using Avalonia;
using Runtime.UI.Hud;

namespace Client
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            HudEngine.Initialize();

            try
            {
                AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace()
                    .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                HudEngine.Shutdown();
            }
        }
    }
}
