using Client.Configuration;
using Editor.Environment;
using Editor.Levels;
using Editor.Viewport;
using ZoneReborn.World.Actors;
using ZoneReborn.World.Assets;

namespace Client.Gameplay
{
    public sealed class LocalGameSession
    {
        private readonly LevelWorkspace _workspace = new();

        public async Task StartAsync(
            DxViewportHost viewport,
            ClientSettings settings,
            CancellationToken cancellationToken = default)
        {
            AssetPaths assets = _workspace.Assets;

            string directory = AssetPathResolver.Resolve(
                assets.Levels,
                settings.Level);

            string path = Path.Combine(
                directory,
                "level.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Игровой уровень не найден: {settings.Level}",
                    path);
            }

            var level = await _workspace.OpenAsync(
                path,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            LevelActor spawn = ResolveSpawn(
                level.Actors,
                settings.PlayerSpawn);

            var environment = await Task.Run(
                () => EnvironmentSettings.Load(
                    level.Directory,
                    assets),
                cancellationToken);

            ActorItemCatalog? itemCatalog = null;

            if (level.Actors.Actors.Any(actor =>
                    actor.Enabled &&
                    actor.Kind == ActorKind.LootSpawn))
            {
                itemCatalog = await Task.Run(
                    () => ActorItemCatalog.Read(assets),
                    cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            viewport.ShowGrid = false;
            viewport.ShowSelectedCollision = false;
            viewport.ActorEditingEnabled = false;

            viewport.LootItemCatalog = itemCatalog;

            viewport.SetEnvironment(environment);

            viewport.SetLevel(
                level,
                assets);

            viewport.SetCamera(new CameraPose(
                spawn.X,
                spawn.Y + 1.75f,
                spawn.Z,
                spawn.Yaw,
                0f,
                8f));

            await viewport.StartPlayAsync(spawn);

            cancellationToken.ThrowIfCancellationRequested();

            if (!viewport.IsPlaying)
            {
                throw new InvalidOperationException(
                    "Не удалось запустить игровую сессию.");
            }
        }

        private static LevelActor ResolveSpawn(
            ActorDocument document,
            string name)
        {
            LevelActor[] spawns = document.Actors
                .Where(actor =>
                    actor.Enabled &&
                    actor.Kind == ActorKind.PlayerSpawn)
                .ToArray();

            if (spawns.Length == 0)
            {
                throw new InvalidOperationException(
                    "На уровне отсутствуют активные PlayerSpawn.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                if (spawns.Length == 1)
                {
                    return spawns[0];
                }

                throw new InvalidOperationException(
                    "Необходимо назначить PlayerSpawn в client.json.");
            }

            LevelActor[] matches = spawns
                .Where(actor => string.Equals(
                    actor.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length == 0)
            {
                throw new InvalidOperationException(
                    $"PlayerSpawn '{name}' не найден.");
            }

            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Обнаружено несколько PlayerSpawn с именем '{name}'.");
            }

            return matches[0];
        }
    }
}
