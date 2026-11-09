using SharpDX.D3DCompiler;
using ZoneReborn.World.Assets;

namespace Editor.Rendering.Shaders
{
    internal sealed class ShaderCompiler
    {
        private const string EnvironmentInclude =
            "#include \"Environment/Lighting.hlsli\"";

        private readonly AssetPaths _assets;

        public ShaderCompiler(AssetPaths assets)
        {
            _assets = assets;
        }

        public CompilationResult Compile(
            string relativePath,
            string entryPoint,
            string profile)
        {
            string path = AssetPathResolver.Resolve(
                _assets.Shaders,
                relativePath);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Не найден файл шейдера: {path}",
                    path);
            }

            string source = File.ReadAllText(path);

            if (!source.Contains(
                    EnvironmentInclude,
                    StringComparison.Ordinal))
            {
                return ShaderBytecode.CompileFromFile(
                    path,
                    entryPoint,
                    profile,
                    ShaderFlags.None,
                    EffectFlags.None,
                    null,
                    null);
            }

            string includePath = AssetPathResolver.Resolve(
                _assets.Shaders,
                "Environment/Lighting.hlsli");

            string commonSource = File.ReadAllText(includePath);

            source = source.Replace(
                EnvironmentInclude,
                commonSource,
                StringComparison.Ordinal);

            return ShaderBytecode.Compile(
                source,
                entryPoint,
                profile,
                ShaderFlags.None,
                EffectFlags.None);
        }
    }
}
