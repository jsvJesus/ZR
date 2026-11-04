using System.Numerics;

namespace ZoneReborn.World.Scene;

/// <summary>World-space editor fields. Rotation components are yaw, pitch and roll in degrees.</summary>
public readonly record struct SceneObjectTransform(Vector3 Position, Vector3 Rotation, Vector3 Scale)
{
    public Matrix4x4 ToWorld()
    {
        if (!Finite(Position) || !Finite(Rotation) || !Finite(Scale) ||
            MaxAbs(Position) > 1e7f || MaxAbs(Rotation) > 1e6f || MaxAbs(Scale) > 1000 ||
            MathF.Abs(Scale.X) < 0.001f || MathF.Abs(Scale.Y) < 0.001f || MathF.Abs(Scale.Z) < 0.001f)
            throw new InvalidDataException("Некорректная трансформация. Масштаб по каждой оси: от 0.001 до 1000 по модулю.");
        var radians = Rotation * (MathF.PI / 180);
        var world = Matrix4x4.CreateScale(Scale) *
            Matrix4x4.CreateFromYawPitchRoll(radians.X, radians.Y, radians.Z) * Matrix4x4.CreateTranslation(Position);
        ValidateWorld(world);
        return world;
    }

    public static bool TryFromWorld(Matrix4x4 world, out SceneObjectTransform transform)
    {
        transform = default;
        if (!Matrix4x4.Decompose(world, out var scale, out var rotation, out var position) ||
            !Finite(scale) || !Finite(position) || !float.IsFinite(rotation.LengthSquared()) || rotation.LengthSquared() < 1e-10f)
            return false;
        rotation = Quaternion.Normalize(rotation);
        var basis = Matrix4x4.CreateFromQuaternion(rotation);
        float pitch = MathF.Asin(Math.Clamp(-basis.M32, -1, 1));
        float yaw, roll;
        if (MathF.Abs(MathF.Cos(pitch)) > 0.0001f)
        {
            yaw = MathF.Atan2(basis.M31, basis.M33);
            roll = MathF.Atan2(basis.M12, basis.M22);
        }
        else
        {
            // At a vertical pitch only yaw +/- roll is determined. Keep roll zero.
            yaw = MathF.Atan2(-basis.M13, basis.M11);
            roll = 0;
        }
        transform = new(position, new Vector3(yaw, pitch, roll) * (180 / MathF.PI), scale);
        var rebuilt = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, roll)
            * Matrix4x4.CreateTranslation(position);
        // Decompose can succeed on a sheared matrix. Never silently replace that
        // authored affine transform with an approximation in the properties panel.
        float tolerance = Math.Max(1, MaxAbs(scale)) * 0.0002f;
        return Difference(world, rebuilt) <= tolerance;
    }

    internal static void ValidateWorld(Matrix4x4 world)
    {
        var x = new Vector3(world.M11, world.M12, world.M13);
        var y = new Vector3(world.M21, world.M22, world.M23);
        var z = new Vector3(world.M31, world.M32, world.M33);
        if (!Finite(x) || !Finite(y) || !Finite(z) || !Finite(world.Translation) ||
            world.M14 != 0 || world.M24 != 0 || world.M34 != 0 || world.M44 != 1 ||
            MaxAbs(world.Translation) > 1e7f || MaxAbs(x) > 1e4f || MaxAbs(y) > 1e4f || MaxAbs(z) > 1e4f ||
            !Matrix4x4.Invert(world, out var inverse) || !Finite(inverse.Translation) ||
            !Finite(new(inverse.M11, inverse.M12, inverse.M13)) ||
            !Finite(new(inverse.M21, inverse.M22, inverse.M23)) ||
            !Finite(new(inverse.M31, inverse.M32, inverse.M33)))
            throw new InvalidDataException("Матрица объекта должна быть конечной, аффинной и обратимой.");
    }

    private static float Difference(Matrix4x4 a, Matrix4x4 b) => Math.Max(
        Math.Max(MaxAbs(new(a.M11 - b.M11, a.M12 - b.M12, a.M13 - b.M13)),
            MaxAbs(new(a.M21 - b.M21, a.M22 - b.M22, a.M23 - b.M23))),
        MaxAbs(new(a.M31 - b.M31, a.M32 - b.M32, a.M33 - b.M33)));
    private static float MaxAbs(Vector3 value) => Math.Max(MathF.Abs(value.X), Math.Max(MathF.Abs(value.Y), MathF.Abs(value.Z)));
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
