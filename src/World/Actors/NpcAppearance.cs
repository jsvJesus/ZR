using System.Text.Json.Serialization;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters;

namespace ZoneReborn.World.Actors
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NpcAppearancePreset
    {
        Civilian,
        Trader,
        Medic,
        Technician,
        Guard
    }

    public static class NpcAppearance
    {
        public static NpcAppearancePreset Resolve(
            ActorNpcDefinition npc)
        {
            if (npc.Profession == "DOCTOR")
            {
                return NpcAppearancePreset.Medic;
            }

            if (npc.Profession is "ENGINEER" or "ARMORER" ||
                npc.TemplateId.Contains(
                    "repair",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NpcAppearancePreset.Technician;
            }

            if (npc.TemplateId.Contains(
                    "guard",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NpcAppearancePreset.Guard;
            }

            if (npc.IsTradeCandidate)
            {
                return NpcAppearancePreset.Trader;
            }

            return NpcAppearancePreset.Civilian;
        }

        public static CharacterAsset Load(
            AssetPaths assets,
            NpcAppearancePreset preset,
            CancellationToken cancellationToken)
        {
            if (preset == NpcAppearancePreset.Civilian)
            {
                return CharacterLoader.LoadIdle(
                    assets,
                    null,
                    cancellationToken);
            }

            var catalog = CharacterEquipmentCatalog.Read(
                assets,
                cancellationToken);

            int[] itemTypes = preset switch
            {
                NpcAppearancePreset.Trader =>
                    [35010, 36015, 52020],

                NpcAppearancePreset.Medic =>
                    [31030, 36015, 52020],

                NpcAppearancePreset.Technician =>
                    [30020, 30070, 56020],

                NpcAppearancePreset.Guard =>
                    [36020, 36025, 56010],

                _ => catalog.DefaultOutfit.ItemTypes
            };

            var outfit = new CharacterOutfit(itemTypes);

            CharacterPartSelection[] parts =
                catalog.Compose(outfit);

            return CharacterLoader.LoadIdle(
                assets,
                parts,
                cancellationToken);
        }
    }
}
