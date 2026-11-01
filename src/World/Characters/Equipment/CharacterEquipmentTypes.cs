using System.Collections.ObjectModel;

namespace ZoneReborn.World.Characters;

public sealed record CharacterPartSelection(
    string Model,
    IReadOnlyDictionary<string, string> Tints,
    string? AttachmentNode = null,
    string? OriginNode = null)
{
    public CharacterWeaponDefinition? Weapon { get; init; }
}

public sealed record CharacterOutfit(int[] ItemTypes, int? WeaponType = null);

public sealed class CharacterEquipmentItem
{
    public int TypeId { get; }
    public string Name { get; }
    public string Category { get; }
    public int CategoryId { get; }
    public int[] Slots => (int[])SlotTypes.Clone();
    public string[] Models => (string[])ModelNames.Clone();
    public IReadOnlyDictionary<string, string> Tints { get; }
    public bool IsWeapon { get; }
    public CharacterWeaponDefinition? Weapon { get; }

    internal int[] SlotTypes { get; }
    internal string[] ModelNames { get; }
    internal (int Slot, object? Tag)[] OverrideSlots { get; }
    internal (object? Tag, string[] Models)[] AlternativeModels { get; }
    // These identify ability sets in the original game, not automatic visual sets.
    internal string? SetEquip { get; }
    internal bool SetEquipMain { get; }

    internal CharacterEquipmentItem(int typeId, string name, string category, int[] slots,
        string[] models, Dictionary<string, string> tints, bool isWeapon,
        (int Slot, object? Tag)[] overrides, (object? Tag, string[] Models)[] alternatives,
        string? setEquip, bool setEquipMain, int categoryId = 0, CharacterWeaponDefinition? weapon = null)
    {
        TypeId = typeId;
        Name = name;
        Category = category;
        CategoryId = categoryId;
        SlotTypes = slots;
        ModelNames = models;
        Tints = new ReadOnlyDictionary<string, string>(tints);
        IsWeapon = isWeapon;
        Weapon = weapon;
        OverrideSlots = overrides;
        AlternativeModels = alternatives;
        SetEquip = setEquip;
        SetEquipMain = setEquipMain;
    }

    public override string ToString() => Name;
}
