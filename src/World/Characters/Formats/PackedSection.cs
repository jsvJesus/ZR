using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace ZoneReborn.World.Characters.Formats;

public sealed record PackedSection(string Name, object Value, PackedSection[] Children)
{
    public string Text => Convert.ToString(Value, CultureInfo.InvariantCulture) ?? "";
    public PackedSection? Find(string name) => Children.FirstOrDefault(child => child.Name == name);
    public IEnumerable<PackedSection> All(string name) => Children.Where(child => child.Name == name);
    public string String(string name) => Find(name)?.Text ?? "";
    public float[] Floats => Value as float[] ?? throw new InvalidDataException($"Expected float data: {Name}");

    public static PackedSection Read(string path) => Parse(File.ReadAllBytes(path));

    public static PackedSection Parse(byte[] bytes)
    {
        var data = bytes.AsSpan();
        if (data.Length < 12 || BinaryPrimitives.ReadUInt32LittleEndian(data) != 0x62a14e45 || data[4] != 0)
            throw new InvalidDataException("Unsupported BigWorld packed section.");
        int offset = 5;
        var names = new List<string>();
        while (offset < bytes.Length && bytes[offset] != 0)
        {
            int length = bytes.AsSpan(offset).IndexOf((byte)0);
            if (length < 0 || length > 4096 || names.Count >= 65536) throw new InvalidDataException("Invalid name table.");
            names.Add(Encoding.UTF8.GetString(bytes, offset, length));
            offset += length + 1;
        }
        return ReadNode("", offset + 1, bytes.Length, 0);

        PackedSection ReadNode(string name, int start, int end, int depth)
        {
            if (depth > 256 || start < 0 || end > bytes.Length || end - start < 6)
                throw new InvalidDataException("Invalid packed section bounds.");
            int count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(start));
            uint own = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(start + 2));
            int begin = checked(start + 6 + count * 6);
            int previous = (int)(own & 0x0fffffff);
            if (begin > end || previous > end - begin) throw new InvalidDataException("Invalid section value.");
            object value = Decode(begin, previous, own >> 28);
            var children = new PackedSection[count];
            for (int i = 0; i < count; i++)
            {
                int entry = start + 6 + i * 6;
                int key = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(entry));
                uint descriptor = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(entry + 2));
                int stop = (int)(descriptor & 0x0fffffff);
                uint type = descriptor >> 28;
                if (key >= names.Count || stop < previous || stop > end - begin)
                    throw new InvalidDataException("Invalid child section.");
                children[i] = type == 0 ? ReadNode(names[key], begin + previous, begin + stop, depth + 1)
                    : new PackedSection(names[key], Decode(begin + previous, stop - previous, type), []);
                previous = stop;
            }
            if (begin + previous != end) throw new InvalidDataException("Trailing section data.");
            return new PackedSection(name, value, children);
        }

        object Decode(int start, int length, uint type)
        {
            var raw = bytes.AsSpan(start, length);
            switch (type)
            {
                case 1: return Encoding.UTF8.GetString(raw);
                case 2:
                    return length switch { 0 => 0, 1 => (int)raw[0], 2 => (int)BinaryPrimitives.ReadInt16LittleEndian(raw),
                        4 => BinaryPrimitives.ReadInt32LittleEndian(raw), _ => throw new InvalidDataException("Invalid integer.") };
                case 3:
                    if (length % 4 != 0) throw new InvalidDataException("Invalid float section.");
                    var floats = new float[length / 4];
                    for (int i = 0; i < floats.Length; i++)
                    {
                        floats[i] = BinaryPrimitives.ReadSingleLittleEndian(raw[(i * 4)..]);
                        if (!float.IsFinite(floats[i])) throw new InvalidDataException("Non-finite float.");
                    }
                    return floats;
                case 4: return length != 0 && raw[0] != 0;
                case 5: return Convert.ToBase64String(raw);
                default: throw new InvalidDataException($"Unsupported packed value type {type}.");
            }
        }
    }
}
