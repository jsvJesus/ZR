using System.Collections.ObjectModel;
using System.Numerics;

namespace ZoneReborn.World.Characters;

public sealed record CharacterWeaponProfile(
    Vector3 GunPosition,
    Vector3 GunRootPosition,
    float GunRootRoll,
    string ToAttachWeapon,
    string LeftGrip,
    string RightGrip,
    Vector3 LeftHandPosition,
    Vector3 LeftHandRotation,
    Vector3 RightHandPosition,
    Vector3 RightHandRotation)
{
    internal static CharacterWeaponProfile Default { get; } = new(
        Vector3.Zero, Vector3.Zero, 0, "Track", "", "",
        Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);
}

public sealed class CharacterWeaponDefinition
{
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, CharacterWeaponProfile>> _profiles;

    public int TypeId { get; }
    public int CategoryId { get; }
    public int GunType { get; }
    public int WeaponSlot { get; }
    // The game chooses this from the inventory slot, including secondary SMGs and short rifles.
    public string HolsterNode => WeaponSlot == 2 ? "Pistol" : "weaponback";
    public float EquipSpeed { get; }
    public bool IsPistol => GunType is 1 or 2;
    public string TypeName => GunType switch
    {
        1 => "Пистолет", 2 => "Револьвер", 3 => "Пистолет-пулемёт", 4 => "Штурмовая винтовка",
        5 => "Снайперская винтовка", 6 => "Винтовка", 7 => "Дробовик", 8 => "Пулемёт",
        9 => "Гранатомёт", 10 => "Реактивный гранатомёт", 11 => "Граната", 12 => "Распылитель",
        _ => "Оружие"
    };

    private CharacterWeaponDefinition(int typeId, int categoryId, int gunType, int weaponSlot, float equipSpeed,
        Dictionary<string, IReadOnlyDictionary<string, CharacterWeaponProfile>> profiles)
    {
        TypeId = typeId;
        CategoryId = categoryId;
        GunType = gunType;
        WeaponSlot = weaponSlot;
        EquipSpeed = equipSpeed;
        _profiles = new ReadOnlyDictionary<string, IReadOnlyDictionary<string, CharacterWeaponProfile>>(profiles);
    }

    public CharacterWeaponProfile ResolveProfile(bool crouch, bool moving, bool sprint) =>
        ResolveProfile(crouch, moving, sprint, true);

    public CharacterWeaponProfile ResolveProfile(bool crouch, bool moving, bool sprint, bool inCombat, bool running = false)
    {
        // The original tracker maps combat sprinting to run; only bored has a sprint entry.
        string state = crouch ? (moving || sprint ? running ? "crouch_run" : "crouch_walk" : "crouch_idle")
            : sprint ? (inCombat ? "run" : "sprint") : moving ? (running ? "run" : "walk") : "idle";
        if (!_profiles.TryGetValue(inCombat ? "incombat" : "bored", out var profiles)
            && !_profiles.TryGetValue("incombat", out profiles)
            && !_profiles.TryGetValue("bored", out profiles))
            return CharacterWeaponProfile.Default;
        if (profiles.TryGetValue(state, out var profile)) return profile;
        if (crouch && moving && profiles.TryGetValue("crouch_walk", out profile)) return profile;
        if (crouch && profiles.TryGetValue("crouch_idle", out profile)) return profile;
        if (sprint && profiles.TryGetValue("run", out profile)) return profile;
        return profiles.TryGetValue("idle", out profile) ? profile : CharacterWeaponProfile.Default;
    }

    internal static CharacterWeaponDefinition Read(Dictionary<object, object?> data, int typeId, string context)
    {
        int category = Integer(data, "Category", 42, context);
        int gunType = Integer(data, "GunType", 0, context);
        int weaponSlot = Integer(data, "weaponSlot", gunType is 1 or 2 ? 2 : 1, context);
        if (weaponSlot is not (1 or 2)) weaponSlot = gunType is 1 or 2 ? 2 : 1;
        // Original data includes M249's -2 modifier. Keep playback forward and bounded.
        float authoredSpeed = 1 + Number(data, "equip_speed_mod", 0, context);
        float equipSpeed = authoredSpeed > 0 ? Math.Clamp(authoredSpeed, 0.1f, 4) : 1;
        var profiles = new Dictionary<string, IReadOnlyDictionary<string, CharacterWeaponProfile>>(StringComparer.Ordinal);
        if (data.TryGetValue("tracker", out var trackerValue) && trackerValue is not null)
        {
            var tracker = Map(trackerValue, context);
            foreach (string status in new[] { "incombat", "bored" })
            {
                if (!tracker.TryGetValue(status, out var statusValue) || statusValue is null) continue;
                var states = new Dictionary<string, CharacterWeaponProfile>(StringComparer.Ordinal);
                foreach (var state in Map(statusValue, context))
                {
                    if (state.Key is not string name) throw new InvalidDataException($"{context}: invalid tracker state.");
                    var values = Map(state.Value, context);
                    string left = Text(values, "hand_actionHvatL", "", context);
                    string right = Text(values, "hand_actionHvatR", "", context);
                    // AvatarTrackers disables both clips when a source profile assigns one to both hands.
                    if (left.Length != 0 && left == right) left = right = "";
                    states.Add(name, new(
                        Vector(values, "GUNPOS", context), Vector(values, "GUNRWPOS", context),
                        Number(values, "GUNRWROLL", 0, context), Text(values, "to_attach_weapon", "Track", context),
                        left, right,
                        Vector(values, "hand_L_weapon_add_pos", context), Vector(values, "hand_L_weapon_add_rotate", context),
                        Vector(values, "hand_R_weapon_add_pos", context), Vector(values, "hand_R_weapon_add_rotate", context)));
                }
                profiles.Add(status, new ReadOnlyDictionary<string, CharacterWeaponProfile>(states));
            }
        }
        return new(typeId, category, gunType, weaponSlot, equipSpeed, profiles);
    }

    private static Dictionary<object, object?> Map(object? value, string context) =>
        value as Dictionary<object, object?> ?? throw new InvalidDataException($"{context}: invalid weapon tracker dictionary.");

    private static int Integer(Dictionary<object, object?> values, string key, int fallback, string context) =>
        !values.TryGetValue(key, out var value) || value is null ? fallback
        : value is long integer && integer is >= int.MinValue and <= int.MaxValue ? (int)integer
        : throw new InvalidDataException($"{context}: invalid integer {key}.");

    private static string Text(Dictionary<object, object?> values, string key, string fallback, string context) =>
        !values.TryGetValue(key, out var value) || value is null ? fallback
        : value as string ?? throw new InvalidDataException($"{context}: invalid string {key}.");

    private static float Number(Dictionary<object, object?> values, string key, float fallback, string context) =>
        !values.TryGetValue(key, out var value) || value is null ? fallback : Scalar(value, key, context);

    private static float Scalar(object? value, string key, string context)
    {
        float number = value switch { long integer => integer, double real => (float)real, _ => float.NaN };
        return float.IsFinite(number) ? number : throw new InvalidDataException($"{context}: invalid number {key}.");
    }

    private static Vector3 Vector(Dictionary<object, object?> values, string key, string context)
    {
        if (!values.TryGetValue(key, out var value) || value is null) return Vector3.Zero;
        if (value is not List<object?> components || components.Count != 3)
            throw new InvalidDataException($"{context}: invalid vector {key}.");
        return new(Scalar(components[0], key, context), Scalar(components[1], key, context), Scalar(components[2], key, context));
    }
}
