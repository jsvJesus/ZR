using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client
{
    public sealed class GameHost : Game
    {
        private readonly GraphicsDeviceManager _graphics;

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

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(18, 21, 27));

            base.Draw(gameTime);
        }
    }
}
