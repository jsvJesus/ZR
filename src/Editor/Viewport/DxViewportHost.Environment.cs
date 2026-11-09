using Editor.Environment;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private EnvironmentSettings _environment = new();

        public void SetEnvironment(EnvironmentSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _environment = settings;
            _renderer?.SetEnvironment(settings);
        }
    }
}
