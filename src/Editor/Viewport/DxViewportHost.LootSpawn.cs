using System.Numerics;
using ZoneReborn.World.Actors;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        private Guid? _selectedLootLocationId;

        public Guid? SelectedLootLocationId =>
            _selectedLootLocationId;

        public bool LootLocationPlacementEnabled { get; set; }

        public bool LootPreviewEnabled { get; set; } = true;

        public ActorItemCatalog? LootItemCatalog { get; set; }

        private LootSpawnLocation? LootLocationSelection()
        {
            if (ActorSelection() is not { } actor ||
                actor.Kind != ActorKind.LootSpawn ||
                _selectedLootLocationId is not Guid id)
            {
                return null;
            }

            return actor.LootLocations.FirstOrDefault(
                location => location.Id == id);
        }

        public void SelectLootLocation(Guid? id)
        {
            if (_selectedLootLocationId == id)
            {
                return;
            }

            CancelActorDrag();

            _selectedLootLocationId =
                ActorSelection() is { } actor &&
                actor.Kind == ActorKind.LootSpawn &&
                actor.LootLocations.Any(location => location.Id == id)
                    ? id
                    : null;

            if (_selectedLootLocationId.HasValue)
            {
                _actorTransformMode = ObjectGizmoMode.Move;
            }

            _hoveredActorAxis = ObjectGizmoAxis.None;
        }

        private bool PlaceLootLocation(Vector3 position)
        {
            if (!LootLocationPlacementEnabled)
            {
                return false;
            }

            if (ActorSelection() is not { } actor ||
                actor.Kind != ActorKind.LootSpawn ||
                _level == null)
            {
                LootLocationPlacementEnabled = false;

                return true;
            }

            if (actor.LootLocations.Count >= 256)
            {
                RendererError?.Invoke(
                    "Достигнут лимит Locations.");

                return true;
            }

            var location = new LootSpawnLocation
            {
                Loot = []
            };

            location.SetWorldPosition(actor, position);

            actor.LootLocations.Add(location);

            SelectLootLocation(location.Id);

            _level.Actors.Touch();

            ActorChanged?.Invoke(actor.Id);

            return true;
        }

        private bool TryPickLootLocation(
            Vector3 origin,
            Vector3 direction)
        {
            if (ActorSelection() is not { } actor ||
                actor.Kind != ActorKind.LootSpawn)
            {
                return false;
            }

            LootSpawnLocation? selected = null;

            float nearest = 1500f;

            foreach (LootSpawnLocation location in actor.LootLocations)
            {
                Vector3 center =
                    location.GetWorldPosition(actor) +
                    Vector3.UnitY * 0.15f;

                Vector3 offset = center - origin;

                float distance = Vector3.Dot(
                    offset,
                    direction);

                if (distance <= 0f || distance >= nearest)
                {
                    continue;
                }

                Vector3 closest =
                    origin + direction * distance;

                float radius = Math.Clamp(
                    distance * 0.004f,
                    0.22f,
                    0.75f);

                if (Vector3.DistanceSquared(
                        center,
                        closest) > radius * radius)
                {
                    continue;
                }

                selected = location;
                nearest = distance;
            }

            if (selected == null)
            {
                return false;
            }

            SelectLootLocation(selected.Id);

            ActorPicked?.Invoke(actor.Id);

            return true;
        }
    }
}
