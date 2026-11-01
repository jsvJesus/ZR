using System.Collections.ObjectModel;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Characters;

public sealed class CharacterEquipmentCatalog
{
    private static readonly int[] NudeTypes = [11010, 11020, 11025, 11030, 11035];
    private static readonly IReadOnlyDictionary<string, string> NoTints =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
    private const string TeethModel = "characters2/clothing/ManNude/ManTeeth.model";
    private readonly AssetPaths _assets;
    private readonly Dictionary<int, CharacterEquipmentItem> _byType;
    private readonly CharacterEquipmentItem[] _nudeParts;
    private readonly int[] _defaultTypes;

    public IReadOnlyList<CharacterEquipmentItem> Items { get; }
    public CharacterOutfit DefaultOutfit => new((int[])_defaultTypes.Clone());

    private CharacterEquipmentCatalog(AssetPaths assets, CharacterEquipmentItem[] items,
        CharacterEquipmentItem[] nudeParts, int[] defaultTypes)
    {
        _assets = assets;
        Items = Array.AsReadOnly(items);
        _byType = items.ToDictionary(item => item.TypeId);
        _nudeParts = nudeParts;
        _defaultTypes = defaultTypes;
    }

    public static CharacterEquipmentCatalog Read(AssetPaths assets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);
        string source = Path.GetFullPath(Path.Combine(assets.Root, "..", "..", "sdk", "scripts", "common", "data", "items_pyson"));
        var all = new Dictionary<int, CharacterEquipmentItem>();
        foreach (string file in new[] { "CLOTH.pyson", "WEAPON.pyson" })
        {
            string path = Path.Combine(source, file);
            bool isWeapon = file == "WEAPON.pyson";
            var entries = List(PysonLiteralReader.Read(path, cancellationToken), path);
            foreach (object? entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var data = Map(entry, path);
                int type = Integer(Required(data, "TypeID", path), path);
                string context = $"{path}, TypeID {type}";
                int[] slots = isWeapon
                    ? (data.TryGetValue("weaponSlot", out var weaponSlot) ? [Integer(weaponSlot, context)] : [])
                    : List(Get(data, "SlotType", new List<object?>()), context).Select(value => Integer(value, context)).Distinct().ToArray();
                if (!isWeapon && slots.Any(slot => slot < 101 || slot > 133))
                    throw new InvalidDataException($"{context}: неизвестный SlotType.");
                string[] models = isWeapon
                    ? Strings(new[] { Get(data, "ModelPath", "") }, context)
                    : Strings(Map(Get(data, "ModelNames", new Dictionary<object, object?>()), context).Values, context);
                var alternatives = new List<(object? Tag, string[] Models)>();
                foreach (object? value in List(Get(data, "AlternativeModelsList", new List<object?>()), context))
                {
                    var pair = Pair(value, context);
                    alternatives.Add((Tag(pair[0], context), Strings(List(pair[1], context), context)));
                }
                var overrides = new List<(int Slot, object? Tag)>();
                foreach (object? value in List(Get(data, "OverrideSlots", new List<object?>()), context))
                {
                    var pair = Pair(value, context);
                    int slot = Integer(pair[0], context);
                    if (slot < 101 || slot > 133) throw new InvalidDataException($"{context}: неизвестный OverrideSlots {slot}.");
                    overrides.Add((slot, Tag(pair[1], context)));
                }
                var tints = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var tint in Map(Get(data, "Tint", new Dictionary<object, object?>()), context))
                {
                    string tintMatter = Text(tint.Key, context);
                    if (!tints.TryAdd(tintMatter, Text(tint.Value, context)))
                        throw new InvalidDataException($"{context}: повторный Tint {tintMatter}.");
                }
                foreach (string model in models.Concat(alternatives.SelectMany(pair => pair.Models)).Distinct(StringComparer.OrdinalIgnoreCase))
                    ValidateModel(assets, model, context);
                string script = Text(Get(data, "_script_name_", ""), context);
                string category = isWeapon ? "Оружие" : Category(slots);
                string name = DisplayName(data, script, models, category, type, context);
                var item = new CharacterEquipmentItem(type, name, category, slots, models, tints, isWeapon,
                    overrides.ToArray(), alternatives.ToArray(), OptionalText(data, "SetEquip", context),
                    Get(data, "SetEquipMain", false) is true);
                if (!all.TryAdd(type, item)) throw new InvalidDataException($"{context}: повторный TypeID.");
            }
        }

        var nude = NudeTypes.Select(type => all.TryGetValue(type, out var item) && !item.IsWeapon && item.ModelNames.Length != 0
            ? item : throw new InvalidDataException($"В каталоге SDK отсутствует базовая часть тела {type}.")).ToArray();
        ValidateModel(assets, TeethModel, "Базовая модель зубов");
        // Empty templates and the game's diagnostic error clothing are not usable equipment.
        var visible = all.Values.Where(item => item.ModelNames.Length != 0 &&
            (item.IsWeapon || item.SlotTypes.Length != 0) && item.TypeId is not (11021 or 11031)).ToArray();
        string[] defaults =
        [
            "characters2/clothing/body/body_adidas/body_adidas.model",
            "characters2/clothing/legs/legs_adidas/legs_adidas.model",
            "characters2/clothing/boots/boots_adidas/boots_adidas.model"
        ];
        int[] defaultTypes = defaults.Select(model => visible.SingleOrDefault(item => !item.IsWeapon &&
            item.ModelNames.Contains(model, StringComparer.OrdinalIgnoreCase))?.TypeId
            ?? throw new InvalidDataException($"В каталоге SDK отсутствует исходная одежда: {model}")).ToArray();
        var catalog = new CharacterEquipmentCatalog(assets, visible, nude, defaultTypes);
        catalog.Compose(catalog.DefaultOutfit);
        cancellationToken.ThrowIfCancellationRequested();
        return catalog;
    }

    public CharacterOutfit Equip(CharacterOutfit outfit, CharacterEquipmentItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var current = Validate(outfit);
        item = Find(item.TypeId);
        if (item.IsWeapon) return new((int[])outfit.ItemTypes.Clone(), item.TypeId);
        return new(current.Where(previous => !previous.SlotTypes.Intersect(item.SlotTypes).Any())
            .Select(previous => previous.TypeId).Append(item.TypeId).ToArray(), outfit.WeaponType);
    }

    public CharacterOutfit Remove(CharacterOutfit outfit, int typeId)
    {
        Validate(outfit);
        Find(typeId);
        return new(outfit.ItemTypes.Where(type => type != typeId).ToArray(), outfit.WeaponType == typeId ? null : outfit.WeaponType);
    }

    public CharacterPartSelection[] Compose(CharacterOutfit outfit)
    {
        var equipped = Validate(outfit);
        var tags = new Dictionary<int, HashSet<object?>>();
        foreach (var item in equipped)
            foreach (var (slot, tag) in item.OverrideSlots)
            {
                if (!tags.TryGetValue(slot, out var values)) tags[slot] = values = [];
                values.Add(tag);
            }

        var parts = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        var body = _nudeParts.Where(part => !equipped.Any(item => item.SlotTypes.Intersect(part.SlotTypes).Any()));
        foreach (var item in equipped.Concat(body))
        {
            var itemTags = new HashSet<object?>();
            foreach (int slot in item.SlotTypes)
                if (tags.TryGetValue(slot, out var values)) itemTags.UnionWith(values);
            if (itemTags.Contains(null)) continue;
            string[] models = item.ModelNames;
            foreach (var alternative in item.AlternativeModels)
                if (itemTags.Contains(alternative.Tag))
                {
                    models = alternative.Models;
                    break;
                }
            foreach (string model in models)
            {
                ValidateModel(_assets, model, $"Предмет {item.TypeId}");
                if (!parts.TryGetValue(model, out var tints))
                    parts[model] = tints = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var tint in item.Tints) tints[tint.Key] = tint.Value;
            }
        }
        ValidateModel(_assets, TeethModel, "Базовая модель зубов");
        parts.TryAdd(TeethModel, new Dictionary<string, string>(StringComparer.Ordinal));
        var result = parts.Select(part => new CharacterPartSelection(part.Key,
            new ReadOnlyDictionary<string, string>(part.Value))).ToList();
        if (outfit.WeaponType is int weaponType)
        {
            var weapon = Find(weaponType);
            foreach (string model in weapon.ModelNames)
            {
                ValidateModel(_assets, model, $"Оружие {weaponType}");
                // The original M1911 exports a pistol origin only; its avatar anchor has the same name.
                string attachment = model.Equals("characters2/weapons/pistol/colt/m1911_lod1.model", StringComparison.OrdinalIgnoreCase)
                    ? "HP_Pistol" : "HP_weaponback";
                result.Add(new(model, weapon.Tints.Count == 0 ? NoTints : weapon.Tints, attachment, attachment));
            }
        }
        return result.ToArray();
    }

    private CharacterEquipmentItem[] Validate(CharacterOutfit outfit)
    {
        ArgumentNullException.ThrowIfNull(outfit);
        if (outfit.ItemTypes is null) throw new InvalidDataException("В комплекте отсутствует список предметов.");
        var occupied = new HashSet<int>();
        var types = new HashSet<int>();
        var items = new List<CharacterEquipmentItem>();
        foreach (int type in outfit.ItemTypes)
        {
            var item = Find(type);
            if (item.IsWeapon) throw new InvalidDataException($"Оружие {type} должно находиться в отдельном слоте оружия.");
            if (!types.Add(type)) throw new InvalidDataException($"Предмет {type} повторяется в комплекте.");
            foreach (int slot in item.SlotTypes)
                if (!occupied.Add(slot)) throw new InvalidDataException($"Предмет {type} конфликтует с другим предметом в слоте {slot}.");
            items.Add(item);
        }
        if (outfit.WeaponType is int weaponType && !Find(weaponType).IsWeapon)
            throw new InvalidDataException($"Предмет {weaponType} не является оружием.");
        return items.ToArray();
    }

    private CharacterEquipmentItem Find(int type) => _byType.TryGetValue(type, out var item) ? item
        : throw new InvalidDataException($"Предмет {type} отсутствует в доступном каталоге SDK. Сбросьте сохранённый комплект.");

    private static void ValidateModel(AssetPaths assets, string model, string context)
    {
        if (!model.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"{context}: ожидался путь .model: {model}");
        string path = AssetPathResolver.Resolve(assets.Root, model);
        if (!File.Exists(path)) throw new FileNotFoundException($"{context}: не найдена модель {model}", path);
    }

    private static string DisplayName(Dictionary<object, object?> data, string script, string[] models,
        string category, int type, string context)
    {
        foreach (string field in new[] { "nameRU", "nameEN" })
            if (OptionalText(data, field, context) is { Length: > 0 } name)
                return name.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? name;
        if (!string.IsNullOrWhiteSpace(script) && !script.StartsWith("ITEM_", StringComparison.Ordinal))
            return script.Replace('_', ' ');
        return models.Length == 0 ? $"{category} {type}" : Path.GetFileNameWithoutExtension(models[0]).Replace('_', ' ');
    }

    private static string Category(int[] slots) => slots.Length > 3 ? "Костюмы" : string.Join(" / ", slots.Select(slot => slot switch
    {
        101 => "Голова", 102 => "Торс", 103 => "Ноги", 104 => "Кисти рук", 105 => "Шлемы",
        106 => "Очки", 107 => "Маски", 108 => "Наушники", 109 => "Нижнее бельё", 110 => "Нательные майки",
        111 => "Кальсоны", 112 => "Нательные рубашки", 113 => "Носки", 114 => "Рубашки", 115 => "Головные уборы",
        116 => "Защитные головные уборы", 117 => "Куртки", 118 => "Защитные куртки", 119 => "Брюки", 120 => "Защитные брюки",
        121 => "Перчатки", 122 => "Защитные перчатки", 123 => "Обувь", 124 => "Защитная обувь", 125 => "Разгрузки",
        126 => "Броня", 127 => "Рюкзаки", 128 => "Ступни", 129 => "Руки", 130 => "Волосы", 131 => "Усы",
        132 => "Бороды", 133 => "Пояса", _ => $"Слот {slot}"
    }).Distinct());

    private static object? Required(Dictionary<object, object?> data, string key, string context) =>
        data.TryGetValue(key, out var value) ? value : throw new InvalidDataException($"{context}: отсутствует {key}.");
    private static object? Get(Dictionary<object, object?> data, string key, object? fallback) => data.TryGetValue(key, out var value) ? value : fallback;
    private static Dictionary<object, object?> Map(object? value, string context) => value as Dictionary<object, object?>
        ?? throw new InvalidDataException($"{context}: ожидался словарь.");
    private static List<object?> List(object? value, string context) => value as List<object?>
        ?? throw new InvalidDataException($"{context}: ожидался список или кортеж.");
    private static List<object?> Pair(object? value, string context)
    {
        var pair = List(value, context);
        return pair.Count == 2 ? pair : throw new InvalidDataException($"{context}: ожидалась пара значений.");
    }
    private static int Integer(object? value, string context) => value is long number && number is >= int.MinValue and <= int.MaxValue
        ? (int)number : throw new InvalidDataException($"{context}: ожидалось целое число.");
    private static string Text(object? value, string context) => value as string
        ?? throw new InvalidDataException($"{context}: ожидалась строка.");
    private static string? OptionalText(Dictionary<object, object?> data, string key, string context) =>
        !data.TryGetValue(key, out var value) || value is null ? null : Text(value, context);
    private static object? Tag(object? value, string context) => value is null or string or long or bool ? value
        : throw new InvalidDataException($"{context}: недопустимый тег слота.");
    private static string[] Strings(IEnumerable<object?> values, string context) => values
        .Where(value => value is not null and not false).Select(value => Text(value, context).Replace('\\', '/'))
        .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
