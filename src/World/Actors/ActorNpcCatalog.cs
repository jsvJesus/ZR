using System.Text.Json;
using ZoneReborn.World.Assets;

namespace ZoneReborn.World.Actors
{
    public sealed record ActorNpcDefinition(
        int NpcId,
        string TemplateId,
        string Name,
        string Profession,
        string HeadPreset,
        string BarterList,
        string SourceLocation,
        int FactionId,
        bool HasAutoQuests)
    {
        public bool IsTradeCandidate =>
            !string.IsNullOrWhiteSpace(BarterList) ||
            TemplateId.Contains(
                "trader",
                StringComparison.OrdinalIgnoreCase) ||
            Profession is "TRADER"
                or "WEAPONER"
                or "ARMORER"
                or "BARMAN";

        public override string ToString()
        {
            return $"{Name} [{NpcId}]";
        }
    }

    public sealed class ActorNpcCatalog
    {
        public IReadOnlyList<ActorNpcDefinition> Items { get; }

        private ActorNpcCatalog(
            IReadOnlyList<ActorNpcDefinition> items)
        {
            Items = items;
        }

        public static ActorNpcCatalog Read(AssetPaths assets)
        {
            string directory = Path.GetFullPath(
                Path.Combine(
                    assets.Root,
                    "..",
                    "..",
                    "sdk",
                    "scripts",
                    "common",
                    "data"));

            string path = Path.Combine(
                directory,
                "AdditionalNPCParametersData.json");

            string questsDirectory = Path.Combine(
                directory,
                "Quests",
                "QuestsAutogenerator");

            var questers = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(questsDirectory))
            {
                foreach (string file in Directory.EnumerateFiles(
                    questsDirectory,
                    "*.json"))
                {
                    questers.Add(
                        Path.GetFileNameWithoutExtension(file));
                }
            }

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);

            var items = new List<ActorNpcDefinition>();

            foreach (JsonProperty property in
                     document.RootElement.EnumerateObject())
            {
                if (property.Name == "!!default")
                {
                    continue;
                }

                JsonElement data = property.Value;

                if (data.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                int id = ReadInt(data, "npcID");

                if (id <= 0)
                {
                    continue;
                }

                string name = ReadString(data, "ru_name");

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = ReadString(data, "en_name");
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = property.Name;
                }

                items.Add(new ActorNpcDefinition(
                    id,
                    property.Name,
                    name,
                    ReadString(data, "prof_name"),
                    ReadString(data, "head_preset_name"),
                    ReadString(data, "BarterListName"),
                    ReadString(data, "space"),
                    ReadInt(data, "fractionID"),
                    questers.Contains(property.Name)));
            }

            return new ActorNpcCatalog(
                items.OrderBy(item => item.Name)
                    .ThenBy(item => item.NpcId)
                    .ToArray());
        }

        private static string ReadString(
            JsonElement data,
            string property)
        {
            return data.TryGetProperty(property, out var value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? ""
                : "";
        }

        private static int ReadInt(
            JsonElement data,
            string property)
        {
            return data.TryGetProperty(property, out var value) &&
                   value.ValueKind == JsonValueKind.Number &&
                   value.TryGetInt32(out int number)
                ? number
                : 0;
        }
    }
}
