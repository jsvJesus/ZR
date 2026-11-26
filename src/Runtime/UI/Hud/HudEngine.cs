using CefSharp;
using CefSharp.OffScreen;

namespace Runtime.UI.Hud
{
    public static class HudEngine
    {
        private static bool _initialized;
        private static string? _cacheDirectory;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _cacheDirectory = Path.Combine(
                Path.GetTempPath(),
                "GameHud",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_cacheDirectory);

            var settings = new CefSettings
            {
                WindowlessRenderingEnabled = true,
                BackgroundColor = 0x00000000,
                RootCachePath = _cacheDirectory
            };

            if (!Cef.Initialize(settings))
            {
                throw new InvalidOperationException(
                    "Failed to initialize HTML UI engine.");
            }

            _initialized = true;
        }

        public static void Shutdown()
        {
            if (!_initialized)
            {
                return;
            }

            Cef.Shutdown();

            _initialized = false;

            if (_cacheDirectory != null)
            {
                try
                {
                    Directory.Delete(
                        _cacheDirectory,
                        true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                _cacheDirectory = null;
            }
        }
    }
}
