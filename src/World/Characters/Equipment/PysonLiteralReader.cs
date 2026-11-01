using System.Globalization;
using System.Text;

namespace ZoneReborn.World.Characters;

/// <summary>Reads data literals only. PYSON dictionaries may omit commas between entries.</summary>
internal sealed class PysonLiteralReader
{
    private readonly string _text;
    private readonly string _source;
    private readonly CancellationToken _cancellationToken;
    private int _position;
    private int _valueCount;

    private PysonLiteralReader(string text, string source, CancellationToken cancellationToken)
    {
        _text = text;
        _source = source;
        _cancellationToken = cancellationToken;
    }

    internal static object? Read(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
            throw new FileNotFoundException($"Не найден исходный каталог предметов SDK: {path}", path);
        if (new FileInfo(path).Length > 32 * 1024 * 1024)
            throw new InvalidDataException($"Слишком большой каталог PYSON: {path}");
        var reader = new PysonLiteralReader(File.ReadAllText(path, Encoding.UTF8), path, cancellationToken);
        object? result = reader.Value(0);
        reader.Space();
        if (reader._position != reader._text.Length) throw reader.Error("Лишние данные после литерала");
        return result;
    }

    private object? Value(int depth)
    {
        if ((++_valueCount & 255) == 0) _cancellationToken.ThrowIfCancellationRequested();
        if (depth > 64 || _valueCount > 2_000_000) throw Error("Превышен предел сложности PYSON");
        Space();
        if (_position >= _text.Length) throw Error("Неожиданный конец литерала");
        char c = _text[_position];
        if (c == '{') return Dictionary(depth + 1);
        if (c == '[' || c == '(') return Sequence(depth + 1, c == '[' ? ']' : ')');
        if (c == '"') return String();
        if ((c == 'u' || c == 'U') && _position + 1 < _text.Length && _text[_position + 1] == '"')
        {
            _position++;
            return String();
        }
        if (c == '-' || char.IsAsciiDigit(c)) return Number();
        int start = _position;
        while (_position < _text.Length && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] == '_'))
            _position++;
        return _text[start.._position] switch
        {
            "True" => true,
            "False" => false,
            "None" => null,
            _ => throw Error("Допускаются только словари, списки, кортежи, строки, числа, True, False и None")
        };
    }

    private Dictionary<object, object?> Dictionary(int depth)
    {
        _position++;
        var result = new Dictionary<object, object?>();
        while (!Take('}'))
        {
            object? key = Value(depth);
            if (key is not (string or long or double or bool)) throw Error("Недопустимый ключ словаря");
            Require(':');
            if (!result.TryAdd(key, Value(depth))) throw Error($"Повторный ключ словаря: {key}");
            Take(',');
        }
        return result;
    }

    private List<object?> Sequence(int depth, char close)
    {
        _position++;
        var result = new List<object?>();
        while (!Take(close))
        {
            result.Add(Value(depth));
            Take(',');
        }
        return result;
    }

    private string String()
    {
        _position++;
        var result = new StringBuilder();
        while (_position < _text.Length)
        {
            char c = _text[_position++];
            if (c == '"') return result.ToString();
            if (c == '\r' || c == '\n') throw Error("Перенос строки внутри строкового литерала");
            if (c != '\\') { result.Append(c); continue; }
            if (_position >= _text.Length) throw Error("Незавершённая escape-последовательность");
            c = _text[_position++];
            switch (c)
            {
                case '\\': result.Append('\\'); break;
                case '"': result.Append('"'); break;
                case '\'': result.Append('\''); break;
                case 'n': result.Append('\n'); break;
                case 'r': result.Append('\r'); break;
                case 't': result.Append('\t'); break;
                case 'a': result.Append('\a'); break;
                case 'b': result.Append('\b'); break;
                case 'f': result.Append('\f'); break;
                case 'v': result.Append('\v'); break;
                case 'x': result.Append((char)Hex(2)); break;
                case 'u': result.Append((char)Hex(4)); break;
                case 'U':
                    uint code = Hex(8);
                    if (code > 0x10ffff || code is >= 0xd800 and <= 0xdfff) throw Error("Недопустимый Unicode-код");
                    result.Append(char.ConvertFromUtf32((int)code));
                    break;
                default:
                    // Python preserves unknown escapes; keep resource path backslashes intact.
                    result.Append('\\').Append(c);
                    break;
            }
        }
        throw Error("Незавершённая строка");
    }

    private uint Hex(int count)
    {
        if (_position + count > _text.Length || !uint.TryParse(_text.AsSpan(_position, count),
            NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint value))
            throw Error("Недопустимая escape-последовательность");
        _position += count;
        return value;
    }

    private object Number()
    {
        int start = _position;
        if (_text[_position] == '-') _position++;
        while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++;
        bool floating = false;
        if (_position < _text.Length && _text[_position] == '.')
        {
            floating = true;
            _position++;
            while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++;
        }
        if (_position < _text.Length && (_text[_position] == 'e' || _text[_position] == 'E'))
        {
            floating = true;
            _position++;
            if (_position < _text.Length && (_text[_position] == '+' || _text[_position] == '-')) _position++;
            while (_position < _text.Length && char.IsAsciiDigit(_text[_position])) _position++;
        }
        string token = _text[start.._position];
        if (!floating && long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long integer))
            return integer;
        if (floating && double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number))
            return number;
        throw Error("Недопустимое число");
    }

    private bool Take(char c)
    {
        Space();
        if (_position >= _text.Length || _text[_position] != c) return false;
        _position++;
        return true;
    }

    private void Require(char c)
    {
        if (!Take(c)) throw Error($"Ожидался символ '{c}'");
    }

    private void Space()
    {
        _cancellationToken.ThrowIfCancellationRequested();
        while (_position < _text.Length && (char.IsWhiteSpace(_text[_position]) || _text[_position] == '\ufeff')) _position++;
    }

    private InvalidDataException Error(string message)
    {
        int line = 1, column = 1;
        for (int i = 0; i < _position; i++)
        {
            if (_text[i] == '\n') { line++; column = 1; }
            else column++;
        }
        return new InvalidDataException($"{_source}:{line}:{column}: {message}.");
    }
}
