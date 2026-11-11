using System.Text;
using System.Text.Json;

namespace ZoneReborn.World.Scene.Import;

public sealed record CollisionGenerationSettings(
    float TargetRatio,
    float MaximumError,
    bool PreserveBorders,
    string Profile = "custom"
);

public static class CollisionGenerationSettingsStorage
{
    public static readonly CollisionGenerationSettings Defaults =
        new(0.12f, 0.015f, true, "general");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string GetPath(string modelPath)
    {
        return Path.ChangeExtension(
            modelPath,
            ".zrcollision.settings.json"
        );
    }

    public static CollisionGenerationSettings Read(
        string modelPath)
    {
        string path = GetPath(modelPath);

        if (!File.Exists(path))
        {
            return CollisionProfiles.Resolve(
                "auto",
                modelPath
            );
        }

        string json = File.ReadAllText(path);

        var settings =
            JsonSerializer.Deserialize<CollisionGenerationSettings>(
                json,
                Options
            ) ?? throw new InvalidDataException(
                $"Не удалось прочитать настройки коллизии: {path}"
            );

        if (string.IsNullOrWhiteSpace(settings.Profile))
        {
            settings = settings with
            {
                Profile = "custom"
            };
        }

        Validate(settings);
        return settings;
    }

    public static void Write(
        string modelPath,
        CollisionGenerationSettings settings)
    {
        Validate(settings);

        string path = GetPath(modelPath);

        Directory.CreateDirectory(
            Path.GetDirectoryName(path)!
        );

        string temporary =
            path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            string json = JsonSerializer.Serialize(
                settings,
                Options
            );

            File.WriteAllText(
                temporary,
                json,
                new UTF8Encoding(false)
            );

            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static void Validate(
        CollisionGenerationSettings settings)
    {
        if (!float.IsFinite(settings.TargetRatio) ||
            settings.TargetRatio < 0.05f ||
            settings.TargetRatio > 1f)
        {
            throw new InvalidDataException(
                "Количество треугольников должно быть от 5% до 100%."
            );
        }

        if (!float.IsFinite(settings.MaximumError) ||
            settings.MaximumError < 0.001f ||
            settings.MaximumError > 0.1f)
        {
            throw new InvalidDataException(
                "Максимальная ошибка должна быть от 1 до 100 мм."
            );
        }

        if (!CollisionProfiles.IsKnown(settings.Profile))
        {
            throw new InvalidDataException(
                "Недопустимый профиль коллизии."
            );
        }
    }
}
