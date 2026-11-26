using System.Numerics;
using ZoneReborn.World.Actors;

namespace Runtime.Viewport.Roads
{
    internal sealed partial class RoadOverlayRenderer
    {
        private static readonly Vector4 LootLocationColor =
            new(0.82f, 0.87f, 0.95f, 1f);

        private static readonly Vector4 SelectedLootLocationColor =
            new(1f, 0.08f, 0.08f, 1f);

        private void DrawLootLocations(
            LevelActor actor,
            Guid? selectedLocation)
        {
            LootSpawnLocation? selected = null;

            foreach (LootSpawnLocation location in actor.LootLocations)
            {
                if (location.Id == selectedLocation)
                {
                    selected = location;
                    continue;
                }

                DrawLootCube(
                    location.GetWorldPosition(actor),
                    0.3f,
                    LootLocationColor);
            }

            if (selected != null)
            {
                DrawLootCube(
                    selected.GetWorldPosition(actor),
                    0.3f,
                    SelectedLootLocationColor);
            }
        }

        private void DrawLootCube(
            Vector3 position,
            float size,
            Vector4 color)
        {
            float half = size * 0.5f;

            Span<Vector3> corners =
                stackalloc Vector3[8];

            for (int index = 0; index < 8; index++)
            {
                corners[index] = position + new Vector3(
                    (index & 1) == 0 ? -half : half,
                    (index & 2) == 0 ? 0f : size,
                    (index & 4) == 0 ? -half : half);
            }

            for (int index = 0; index < 8; index++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    int other = index ^ (1 << axis);

                    if (other > index)
                    {
                        AddLine(
                            corners[index],
                            corners[other],
                            color);
                    }
                }
            }
        }
    }
}
