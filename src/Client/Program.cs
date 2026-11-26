using Avalonia;

namespace Client
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .StartWithClassicDesktopLifetime(args);
        }
    }
}
