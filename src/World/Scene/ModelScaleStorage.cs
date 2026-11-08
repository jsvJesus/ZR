using System.Globalization;
using System.Text;

namespace ZoneReborn.World.Scene;

public static class ModelScaleStorage
{
    public const string Extension = ".zrscale";
    public const float DefaultScale = 1.0f;

    public static float Read(string modelPath)
    {
        string path = Path.ChangeExtension(
            modelPath,
            Extension
        );

        if (!File.Exists(path))
        {
            return 1f;
        }

        string text = File.ReadAllText(path).Trim();

        if (!float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float scale) ||
            !float.IsFinite(scale) ||
            scale < 0.01f ||
            scale > 100f)
        {
            throw new InvalidDataException(
                $"Некорректный масштаб модели: {path}"
            );
        }

        return scale;
    }

    public static void CreateIfMissing(
        string modelPath,
        float scale)
    {
        string path = Path.ChangeExtension(
            modelPath,
            Extension
        );

        if (File.Exists(path))
        {
            return;
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(path)!
        );

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None
            );

            using var writer = new StreamWriter(
                stream,
                new UTF8Encoding(false)
            );

            writer.WriteLine(
                scale.ToString(
                    "R",
                    CultureInfo.InvariantCulture
                )
            );
        }
        catch (IOException) when (File.Exists(path))
        {
        }
    }
}
