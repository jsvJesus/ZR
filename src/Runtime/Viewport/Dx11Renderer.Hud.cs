using Runtime.UI.Hud;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Stats;

namespace Runtime.Viewport
{
    public sealed partial class Dx11Renderer
    {
        private HtmlHudRenderer? _hud;

        private PlayerStatsSnapshot? _hudSnapshot;

        public void SetHudSnapshot(
            PlayerStatsSnapshot? snapshot)
        {
            _hudSnapshot = snapshot;
        }

        private void RenderHud()
        {
            if (_hudSnapshot is not { } snapshot ||
                _backBufferTarget == null)
            {
                return;
            }

            _hud ??= new HtmlHudRenderer(
                _device,
                AssetPaths.FromApplication());

            _hud.Render(
                _context,
                _backBufferTarget,
                _width,
                _height,
                snapshot);
        }

        private void DisposeHud()
        {
            _hud?.Dispose();

            _hud = null;
            _hudSnapshot = null;
        }
    }
}
