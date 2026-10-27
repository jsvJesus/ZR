using System.Numerics;
using System.Text;

namespace ZoneReborn.World.Characters.Formats;

internal sealed class SourceBinary : BinaryReader
{
    public SourceBinary(byte[] bytes) : base(new MemoryStream(bytes, false)) { }
    public int Count(int stride, int maximum = 1_000_000)
    {
        int count = ReadInt32();
        if (count < 0 || count > maximum || (long)count * stride > BaseStream.Length - BaseStream.Position)
            throw new InvalidDataException("Invalid source array length.");
        return count;
    }
    public string SourceString() => Encoding.UTF8.GetString(ReadBytes(Count(1, 4096)));
    public float Scalar()
    {
        float value = ReadSingle();
        return float.IsFinite(value) ? value : throw new InvalidDataException("Non-finite source value.");
    }
    public Vector3 Vector() => new(Scalar(), Scalar(), Scalar());
    public Quaternion Rotation() => new(Scalar(), Scalar(), Scalar(), Scalar());
}
