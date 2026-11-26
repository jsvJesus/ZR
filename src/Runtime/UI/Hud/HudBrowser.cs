using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using CefSharp;
using CefSharp.OffScreen;
using ZoneReborn.World.Characters.Stats;

namespace Runtime.UI.Hud
{
    internal sealed class HudBrowser : IDisposable
    {
        public const int Width = 400;
        public const int Height = 220;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly ChromiumWebBrowser _browser;

        private byte[]? _pendingFrame;

        private long _lastUpdate;

        private bool _disposed;

        public HudBrowser(string htmlPath)
        {
            if (!File.Exists(htmlPath))
            {
                throw new FileNotFoundException(
                    "HUD HTML was not found.",
                    htmlPath);
            }

            string address = new Uri(
                Path.GetFullPath(htmlPath)).AbsoluteUri;

            _browser = new ChromiumWebBrowser(
                address,
                automaticallyCreateBrowser: false);

            try
            {
                _browser.Size = new System.Drawing.Size(
                    Width,
                    Height);

                _browser.DeviceScaleFactor = 1f;

                _browser.Paint += OnPaint;

                _browser.CreateBrowser(
                    browserSettings: new BrowserSettings
                    {
                        BackgroundColor = 0x00000000,
                        WindowlessFrameRate = 30
                    });
            }
            catch
            {
                _browser.Paint -= OnPaint;
                _browser.Dispose();

                throw;
            }
        }

        public void Publish(PlayerStatsSnapshot snapshot)
        {
            if (_disposed ||
                !_browser.IsBrowserInitialized ||
                !_browser.CanExecuteJavascriptInMainFrame)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();

            if (_lastUpdate != 0 &&
                Stopwatch.GetElapsedTime(
                    _lastUpdate,
                    now).TotalMilliseconds < 100)
            {
                return;
            }

            _lastUpdate = now;

            string json = JsonSerializer.Serialize(
                snapshot,
                JsonOptions);

            _browser.ExecuteScriptAsync(
                $"window.updateHud?.({json});");
        }

        private void OnPaint(
            object? sender,
            OnPaintEventArgs e)
        {
            if (e.IsPopup ||
                e.Width != Width ||
                e.Height != Height)
            {
                return;
            }

            byte[] pixels = new byte[
                e.Width * e.Height * 4];

            Marshal.Copy(
                e.BufferHandle,
                pixels,
                0,
                pixels.Length);

            Interlocked.Exchange(
                ref _pendingFrame,
                pixels);

            e.Handled = true;
        }

        public byte[]? TakeFrame()
        {
            return Interlocked.Exchange(
                ref _pendingFrame,
                null);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _browser.Paint -= OnPaint;

            _browser.Dispose();

            Interlocked.Exchange(
                ref _pendingFrame,
                null);
        }
    }
}
