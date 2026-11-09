using System.Numerics;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Formats;

namespace Editor.Environment
{
    internal sealed class SkyCycle
    {
        private readonly Key[] _light;
        private readonly Key[] _ambient;

        private SkyCycle(Key[] light, Key[] ambient)
        {
            _light = light;
            _ambient = ambient;
        }

        public static SkyCycle Load(AssetPaths assets)
        {
            string path = Path.Combine(
                assets.Root,
                "system",
                "data",
                "sky.xml");

            if (!File.Exists(path))
            {
                return new SkyCycle([], []);
            }

            var root = PackedSection.Read(path);
            var cycle = root.Find("day_night_cycle");

            if (cycle == null)
            {
                return new SkyCycle([], []);
            }

            return new SkyCycle(
                ReadKeys(cycle, "lightkey"),
                ReadKeys(cycle, "ambientkey"));
        }

        public Vector3 SunColor(float time)
        {
            return Sample(
                _light,
                time,
                new Vector3(1f, 0.96f, 0.87f));
        }

        public Vector3 AmbientColor(float time)
        {
            return Sample(
                _ambient,
                time,
                new Vector3(0.48f, 0.52f, 0.56f));
        }

        private static Key[] ReadKeys(
            PackedSection cycle,
            string name)
        {
            var keys = new List<Key>();

            foreach (var section in cycle.All(name))
            {
                var time = section.Find("time")?.Value as float[];
                var color = section.Find("colour")?.Value as float[];

                if (time is not { Length: > 0 } ||
                    color is not { Length: >= 3 })
                {
                    continue;
                }

                if (!float.IsFinite(time[0]) ||
                    !float.IsFinite(color[0]) ||
                    !float.IsFinite(color[1]) ||
                    !float.IsFinite(color[2]))
                {
                    continue;
                }

                keys.Add(new Key(
                    NormalizeHour(time[0]),
                    Vector3.Clamp(
                        new Vector3(color[0], color[1], color[2]) / 255f,
                        Vector3.Zero,
                        Vector3.One)));
            }

            return keys
                .OrderBy(key => key.Time)
                .ToArray();
        }

        private static Vector3 Sample(
            Key[] keys,
            float time,
            Vector3 fallback)
        {
            if (keys.Length == 0)
            {
                return fallback;
            }

            if (keys.Length == 1)
            {
                return keys[0].Color;
            }

            time = NormalizeHour(time);

            for (int i = 0; i < keys.Length; i++)
            {
                Key start = keys[i];
                Key end = keys[(i + 1) % keys.Length];

                float startTime = start.Time;
                float endTime = i == keys.Length - 1
                    ? end.Time + 24f
                    : end.Time;

                float sampleTime = time;

                if (i == keys.Length - 1 &&
                    sampleTime < startTime)
                {
                    sampleTime += 24f;
                }

                if (sampleTime < startTime ||
                    sampleTime > endTime)
                {
                    continue;
                }

                float duration = MathF.Max(
                    endTime - startTime,
                    0.0001f);

                float blend = Math.Clamp(
                    (sampleTime - startTime) / duration,
                    0f,
                    1f);

                return Vector3.Lerp(
                    start.Color,
                    end.Color,
                    blend);
            }

            return keys[0].Color;
        }

        private static float NormalizeHour(float hour)
        {
            return (hour % 24f + 24f) % 24f;
        }

        private readonly record struct Key(
            float Time,
            Vector3 Color);
    }
}
