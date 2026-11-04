using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ZoneReborn.World.Scene;

// BigWorld PackedSection: type is stored on the END offset, not the start offset.
// Layout reference: BigWorld lib/resmgr/packed_section.{hpp,cpp}.
internal sealed class BigWorldSection(string name, int type, ReadOnlyMemory<byte> data,
    BigWorldSection[] children)
{
    public string Name { get; } = name;
    public BigWorldSection[] Children { get; } = children;
    public BigWorldSection? Child(string name) => Children.FirstOrDefault(child => child.Name == name);
    public IEnumerable<BigWorldSection> All(string name) => Children.Where(child => child.Name == name);
    public string Text => type switch
    {
        1 => Encoding.UTF8.GetString(data.Span),
        // The packer recognizes base64-looking strings (including "vertices") as blobs.
        5 => Convert.ToBase64String(data.Span),
        2 => Integer.ToString(CultureInfo.InvariantCulture),
        4 => Flag ? "true" : "false",
        3 => string.Join(" ", Floats().Select(value => value.ToString("R", CultureInfo.InvariantCulture))),
        _ => throw new InvalidDataException($"Unsupported packed value type {type} for {Name}.")
    };
    public int Integer
    {
        get
        {
            if (type == 2) return data.Length switch
            {
                0 => 0,
                1 => (sbyte)data.Span[0],
                2 => BinaryPrimitives.ReadInt16LittleEndian(data.Span),
                4 => BinaryPrimitives.ReadInt32LittleEndian(data.Span),
                8 => checked((int)BinaryPrimitives.ReadInt64LittleEndian(data.Span)),
                _ => throw new InvalidDataException($"Invalid packed integer {Name}.")
            };
            return int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value : throw new InvalidDataException($"Invalid integer {Name}.");
        }
    }
    public bool Flag => type == 4 ? data.Length > 0 : Text.Trim().ToLowerInvariant() switch
    {
        "true" or "1" => true, "false" or "0" or "" => false,
        _ => throw new InvalidDataException($"Invalid boolean {Name}.")
    };
    public float[] Floats()
    {
        float[] values;
        if (type == 3)
        {
            if (data.Length % 4 != 0) throw new InvalidDataException($"Invalid float data {Name}.");
            if (data.Length == 0) return [0];
            values = new float[data.Length / 4];
            for (int i = 0; i < values.Length; i++)
                values[i] = BinaryPrimitives.ReadSingleLittleEndian(data.Span[(i * 4)..]);
        }
        else if (type == 2) values = [Integer];
        else values = Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        if (values.Any(value => !float.IsFinite(value))) throw new InvalidDataException($"Non-finite value {Name}.");
        return values;
    }

    public static BigWorldSection Read(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = File.OpenRead(path);
        if (stream.Length is < 1 or > 64 * 1024 * 1024) throw new InvalidDataException("Invalid BigWorld metadata size.");
        byte[] bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return Read(bytes, cancellationToken);
    }

    public static BigWorldSection Read(byte[] bytes, CancellationToken cancellationToken)
    {
        if (bytes.Length >= 5 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x62A14E45)
        {
            if (bytes[4] != 0) throw new InvalidDataException("Unsupported BigWorld packed-section version.");
            int offset = 5;
            var strings = new List<string>();
            while (offset < bytes.Length && bytes[offset] != 0)
            {
                int end = Array.IndexOf(bytes, (byte)0, offset);
                if (end < 0 || end - offset > 65536 || strings.Count >= 32768)
                    throw new InvalidDataException("Invalid BigWorld string table.");
                strings.Add(Encoding.UTF8.GetString(bytes, offset, end - offset));
                offset = end + 1;
            }
            if (offset >= bytes.Length) throw new InvalidDataException("Truncated BigWorld string table.");
            int remainingNodes = 250_000;
            return Parse("root", bytes.AsMemory(offset + 1), 0, 0);

            BigWorldSection Parse(string name, ReadOnlyMemory<byte> memory, int type, int depth)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (depth > 128 || --remainingNodes < 0) throw new InvalidDataException("BigWorld section tree is too large.");
                if (type != 0)
                {
                    if (type is < 1 or > 5) throw new InvalidDataException($"Unsupported BigWorld section type {type}.");
                    return new(name, type, memory, []);
                }
                if (memory.Length < 6) throw new InvalidDataException("Truncated BigWorld section.");
                int count = BinaryPrimitives.ReadInt16LittleEndian(memory.Span);
                int payload = 6 + count * 6;
                if (count < 0 || payload > memory.Length) throw new InvalidDataException("Invalid BigWorld child count.");
                uint previous = BinaryPrimitives.ReadUInt32LittleEndian(memory.Span[2..]);
                int ownLength = Position(previous);
                if (ownLength > memory.Length - payload) throw new InvalidDataException("Invalid BigWorld section offset.");
                var children = new BigWorldSection[count];
                for (int i = 0; i < count; i++)
                {
                    int key = BinaryPrimitives.ReadInt16LittleEndian(memory.Span[(6 + i * 6)..]);
                    uint end = BinaryPrimitives.ReadUInt32LittleEndian(memory.Span[(8 + i * 6)..]);
                    int startPosition = Position(previous), endPosition = Position(end);
                    if ((uint)key >= (uint)strings.Count || endPosition < startPosition || endPosition > memory.Length - payload)
                        throw new InvalidDataException("Invalid BigWorld child range.");
                    children[i] = Parse(strings[key], memory.Slice(payload + startPosition, endPosition - startPosition),
                        (int)(end >> 28), depth + 1);
                    previous = end;
                }
                if (payload + Position(previous) != memory.Length) throw new InvalidDataException("Trailing BigWorld section data.");
                int ownType = (int)(BinaryPrimitives.ReadUInt32LittleEndian(memory.Span[2..]) >> 28);
                if (ownType is < 1 or > 5) throw new InvalidDataException("Unsupported BigWorld own-value type.");
                return new(name, ownType, memory.Slice(payload, ownLength), children);
            }
        }
        using var input = new MemoryStream(bytes, writable: false);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 64 * 1024 * 1024 });
        XElement root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Missing BigWorld XML root.");
        int xmlNodes = 250_000;
        return FromXml(root, 0);
        BigWorldSection FromXml(XElement element, int depth)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (depth > 128 || --xmlNodes < 0) throw new InvalidDataException("BigWorld XML tree is too large.");
            string value = string.Concat(element.Nodes().OfType<XText>().Select(text => text.Value)).Trim();
            return new(element.Name.LocalName, 1, Encoding.UTF8.GetBytes(value),
                element.Elements().Select(child => FromXml(child, depth + 1)).ToArray());
        }
    }

    private static int Position(uint value) => (int)(value & 0x0FFFFFFF);
}
