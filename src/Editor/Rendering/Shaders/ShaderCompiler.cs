using System.Text;
using System.Text.RegularExpressions;
using SharpDX.D3DCompiler;
using ZoneReborn.World.Assets;

namespace Editor.Rendering.Shaders
{
    internal sealed class ShaderCompiler
    {
        private static readonly Regex IncludeDirective = new(
            @"^\s*#\s*include\s*""([^""]+)""\s*(?://.*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

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
                relativePath
            );

            var activeIncludes = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

            string source = ExpandIncludes(
                path,
                activeIncludes,
                0
            );

            return ShaderBytecode.Compile(
                source,
                entryPoint,
                profile,
                ShaderFlags.None,
                EffectFlags.None
            );
        }

        private string ExpandIncludes(
            string path,
            HashSet<string> activeIncludes,
            int depth)
        {
            if (depth > 32)
            {
                throw new InvalidDataException(
                    "Превышена глубина вложенности HLSL include."
                );
            }

            path = Path.GetFullPath(path);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Не найден файл шейдера: {path}",
                    path
                );
            }

            if (!activeIncludes.Add(path))
            {
                throw new InvalidDataException(
                    $"Обнаружено циклическое подключение HLSL: {path}"
                );
            }

            try
            {
                var source = new StringBuilder();

                foreach (string line in File.ReadLines(path))
                {
                    Match match = IncludeDirective.Match(line);

                    if (!match.Success)
                    {
                        source.AppendLine(line);
                        continue;
                    }

                    string includeName = match.Groups[1].Value
                        .Replace('\\', '/');

                    string includePath = AssetPathResolver.Resolve(
                        _assets.Shaders,
                        includeName
                    );

                    if (!File.Exists(includePath))
                    {
                        string directory = Path.GetDirectoryName(path)!;

                        includePath = AssetPathResolver.Resolve(
                            directory,
                            includeName
                        );
                    }

                    source.AppendLine(
                        ExpandIncludes(
                            includePath,
                            activeIncludes,
                            depth + 1
                        )
                    );
                }

                return source.ToString();
            }
            finally
            {
                activeIncludes.Remove(path);
            }
        }
    }
}
