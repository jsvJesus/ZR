using SharpDX.D3DCompiler;
using ZoneReborn.World.Assets;

namespace Editor.Rendering.Shaders;

internal sealed class ShaderCompiler(AssetPaths assets)
{
    public CompilationResult Compile(string relativePath, string entryPoint, string profile)
    {
        string path = AssetPathResolver.Resolve(assets.Shaders, relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Не найден файл шейдера: {path}", path);

        return ShaderBytecode.CompileFromFile(path, entryPoint, profile,
            ShaderFlags.None, EffectFlags.None, null, null);
    }
}
