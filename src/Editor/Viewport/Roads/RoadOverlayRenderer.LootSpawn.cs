using System.Numerics;
using ZoneReborn.World.Actors;

namespace Editor.Viewport.Roads
{
    internal sealed partial class RoadOverlayRenderer
    {
        private void DrawLootLocations(
            LevelActor actor,
            Guid? selectedLocation)
        {
            foreach (LootSpawnLocation location in actor.LootLocations)
            {
                bool selected =
                    location.Id == selectedLocation;

                Vector4 color = selected
                    ? new Vector4(1f, 0.05f, 0.05f, 1f)
                    : new Vector4(0.45f, 0.45f, 0.45f, 1f);

                Vector3 position =
                    location.GetWorldPosition(actor);

                DrawLootCube(
                    position,
                    0.3f,
                    color);
            }
        }

        private void DrawLootCube(
            Vector3 position,
            float size,
            Vector4 color)
        {
            float half = size * 0.5f;

            Span<Vector3> corners = stackalloc Vector3[8];

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
