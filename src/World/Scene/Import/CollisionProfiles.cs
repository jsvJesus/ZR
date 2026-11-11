using ZoneReborn.World.Scene.Import;

public static class CollisionProfiles
{
    public static bool IsKnown(string? profile)
    {
        return profile is
            "auto" or
            "vehicle" or
            "building" or
            "small" or
            "complex" or
            "general" or
            "custom";
    }

    public static CollisionGenerationSettings Resolve(
        string profile,
        string modelPath)
    {
        if (!IsKnown(profile))
        {
            throw new InvalidDataException(
                $"Неизвестный профиль коллизии: {profile}"
            );
        }

        string resolved = profile == "auto"
            ? Detect(modelPath)
            : profile;

        return resolved switch
        {
            "vehicle" => new(
                0.07f,
                0.015f,
                true,
                profile
            ),

            "building" => new(
                0.25f,
                0.005f,
                true,
                profile
            ),

            "small" => new(
                0.10f,
                0.008f,
                true,
                profile
            ),

            "complex" => new(
                0.40f,
                0.003f,
                true,
                profile
            ),

            _ => new(
                0.12f,
                0.015f,
                true,
                profile
            )
        };
    }

    public static string Detect(string modelPath)
    {
        string[] words = modelPath
            .ToLowerInvariant()
            .Split(
                ['/', '\\', '_', '-', '.', ' '],
                StringSplitOptions.RemoveEmptyEntries
            );

        if (Contains(
                "vehicle", "vehicles", "transport",
                "car", "cars", "vaz", "lada",
                "raf", "uaz", "gaz", "kamaz",
                "truck", "bus", "jeep", "van",
                "sedan", "ambulance"))
        {
            return "vehicle";
        }

        if (Contains(
                "complex", "bridge", "tunnel",
                "bunker", "tower", "stairs",
                "staircase"))
        {
            return "complex";
        }

        if (Contains(
                "building", "buildings", "house",
                "barracks", "garage", "factory",
                "warehouse", "office", "hut",
                "architecture", "structures",
                "industrial", "military",
                "checkpoint", "blockpost"))
        {
            return "building";
        }

        if (Contains(
                "prop", "props", "furniture",
                "chair", "table", "crate",
                "barrel", "box", "debris",
                "small", "trash", "tool"))
        {
            return "small";
        }

        return "general";

        bool Contains(params string[] candidates)
        {
            return words.Any(word =>
                candidates.Contains(
                    word,
                    StringComparer.Ordinal
                )
            );
        }
    }

    public static string DisplayName(string profile)
    {
        return profile switch
        {
            "vehicle" => "Транспорт",
            "building" => "Здания",
            "small" => "Малые объекты",
            "complex" => "Сложная геометрия",
            "general" => "Универсальный",
            "auto" => "Автоматический",
            "custom" => "Ручные настройки",
            _ => "Неизвестный"
        };
    }

    public static int RecommendedTriangles(string modelPath)
    {
        return Detect(modelPath) switch
        {
            "vehicle" => 20_000,
            "building" => 60_000,
            "small" => 8_000,
            "complex" => 100_000,
            _ => 40_000
        };
    }
}
