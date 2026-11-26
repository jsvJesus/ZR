using System.Text.Json;

namespace Client.Configuration
{
    public sealed class ClientSettings
    {
        public string Level { get; init; } = "lvl_01_start";

        public string PlayerSpawn { get; init; } = "ClientSpawn";

        public string PlayerName { get; init; } = "TestPlayer";

        public int WindowWidth { get; init; } = 1600;

        public int WindowHeight { get; init; } = 900;

        public static ClientSettings Load()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "client.json");

            if (!File.Exists(path))
            {
                return new ClientSettings();
            }

            var settings = JsonSerializer.Deserialize<ClientSettings>(
                File.ReadAllText(path),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (settings == null)
            {
                throw new InvalidDataException(
                    "Не удалось прочитать настройки клиента.");
            }

            if (string.IsNullOrWhiteSpace(settings.Level) ||
                string.IsNullOrWhiteSpace(settings.PlayerName) ||
                settings.WindowWidth < 640 ||
                settings.WindowHeight < 480)
            {
                throw new InvalidDataException(
                    "Некорректные настройки клиента.");
            }

            return settings;
        }
    }
}
