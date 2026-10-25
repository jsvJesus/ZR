using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.UI;

namespace Client
{
    public sealed class GameHost : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private Desktop? _desktop;

        public GameHost()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
                SynchronizeWithVerticalRetrace = true
            };

            Window.Title = "ZoneReborn";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            MyraEnvironment.Game = this;
            _desktop = new Desktop();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(18, 21, 27));
            _desktop?.Render();

            base.Draw(gameTime);
        }
    }
}
