using System.Numerics;

namespace ZoneReborn.World.Characters.Formats;

public readonly record struct VectorKey(float Time, Vector3 Value);
public readonly record struct RotationKey(float Time, Quaternion Value);
public sealed record AnimationChannel(VectorKey[] Scale, VectorKey[] Position, RotationKey[] Rotation)
{
    public Vector3 SamplePosition(float frame) => VectorAt(Position, frame);
    public Matrix4x4 Sample(float frame)
    {
        var s = VectorAt(Scale, frame);
        var p = VectorAt(Position, frame);
        int i = Index(Rotation.Length, x => Rotation[x].Time, frame);
        var a = Rotation[i]; var b = Rotation[Math.Min(i + 1, Rotation.Length - 1)];
        var q = Quaternion.Normalize(Quaternion.Slerp(a.Value, b.Value, Factor(a.Time, b.Time, frame)));
        return Matrix4x4.CreateScale(s) * Matrix4x4.CreateFromQuaternion(q) * Matrix4x4.CreateTranslation(p);
    }
    private static Vector3 VectorAt(VectorKey[] keys, float frame)
    {
        int i = Index(keys.Length, x => keys[x].Time, frame);
        var a = keys[i]; var b = keys[Math.Min(i + 1, keys.Length - 1)];
        return Vector3.Lerp(a.Value, b.Value, Factor(a.Time, b.Time, frame));
    }
    private static float Factor(float a, float b, float t) => b > a ? Math.Clamp((t - a) / (b - a), 0, 1) : 0;
    private static int Index(int length, Func<int, float> time, float frame)
    {
        int low = 0, high = length - 1;
        while (low < high) { int middle = (low + high + 1) / 2; if (time(middle) <= frame) low = middle; else high = middle - 1; }
        return low;
    }
}
public sealed record SourceAnimation(float Frames, Dictionary<string, AnimationChannel> Channels);

public static class AnimationReader
{
    public static SourceAnimation Read(string path)
    {
        using var reader = new SourceBinary(File.ReadAllBytes(path));
        float frames = reader.Scalar();
        if (frames <= 0) throw new InvalidDataException("Invalid animation duration.");
        reader.SourceString(); reader.SourceString();
        int count = reader.Count(8, 4096);
        var channels = new Dictionary<string, AnimationChannel>(StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            int type = reader.ReadInt32();
            if (type is not (1 or 4)) throw new InvalidDataException($"Unsupported animation channel {type}.");
            string name = reader.SourceString();
            if (type == 4) { reader.Scalar(); reader.Scalar(); reader.Scalar(); }
            var scale = Vectors(); var position = Vectors();
            int rotations = reader.Count(20);
            if (rotations == 0) throw new InvalidDataException("Empty rotation keys.");
            var rotation = new RotationKey[rotations];
            float previous = -1;
            for (int j = 0; j < rotations; j++)
            {
                float time = Time(ref previous);
                var q = reader.Rotation();
                if (q.LengthSquared() < 0.00001f) throw new InvalidDataException("Invalid rotation.");
                rotation[j] = new(time, Quaternion.Normalize(q));
            }
            for (int j = 0; j < 3; j++) { int indices = reader.Count(4); reader.BaseStream.Position += indices * 4L; }
            if (!channels.TryAdd(name, new(scale, position, rotation))) throw new InvalidDataException("Duplicate animation channel.");
        }
        if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Trailing animation data.");
        return new(frames, channels);

        float Time(ref float previous)
        {
            float time = reader.Scalar();
            if (time < previous || time < 0 || time > frames + 1) throw new InvalidDataException("Invalid keyframe time.");
            previous = time; return time;
        }
        VectorKey[] Vectors()
        {
            int n = reader.Count(16);
            if (n == 0) throw new InvalidDataException("Empty vector keys.");
            var keys = new VectorKey[n]; float previous = -1;
            for (int j = 0; j < n; j++) keys[j] = new(Time(ref previous), reader.Vector());
            return keys;
        }
    }
}
