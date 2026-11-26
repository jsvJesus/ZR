using System.Numerics;
using ZoneReborn.World.Actors;

namespace Editor.Viewport.Roads
{
    internal sealed partial class RoadOverlayRenderer
    {
        private static readonly Dictionary<char, int[]> ActorGlyphs = new()
        {
            ['A'] = [14, 17, 17, 31, 17, 17, 17],
            ['B'] = [30, 17, 17, 30, 17, 17, 30],
            ['C'] = [14, 17, 16, 16, 16, 17, 14],
            ['D'] = [30, 17, 17, 17, 17, 17, 30],
            ['E'] = [31, 16, 16, 30, 16, 16, 31],
            ['F'] = [31, 16, 16, 30, 16, 16, 16],
            ['G'] = [14, 17, 16, 23, 17, 17, 15],
            ['H'] = [17, 17, 17, 31, 17, 17, 17],
            ['I'] = [31, 4, 4, 4, 4, 4, 31],
            ['J'] = [7, 2, 2, 2, 18, 18, 12],
            ['K'] = [17, 18, 20, 24, 20, 18, 17],
            ['L'] = [16, 16, 16, 16, 16, 16, 31],
            ['M'] = [17, 27, 21, 21, 17, 17, 17],
            ['N'] = [17, 25, 21, 19, 17, 17, 17],
            ['O'] = [14, 17, 17, 17, 17, 17, 14],
            ['P'] = [30, 17, 17, 30, 16, 16, 16],
            ['Q'] = [14, 17, 17, 17, 21, 18, 13],
            ['R'] = [30, 17, 17, 30, 20, 18, 17],
            ['S'] = [15, 16, 16, 14, 1, 1, 30],
            ['T'] = [31, 4, 4, 4, 4, 4, 4],
            ['U'] = [17, 17, 17, 17, 17, 17, 14],
            ['V'] = [17, 17, 17, 17, 17, 10, 4],
            ['W'] = [17, 17, 17, 21, 21, 21, 10],
            ['X'] = [17, 17, 10, 4, 10, 17, 17],
            ['Y'] = [17, 17, 10, 4, 4, 4, 4],
            ['Z'] = [31, 1, 2, 4, 8, 16, 31],
            ['0'] = [14, 17, 19, 21, 25, 17, 14],
            ['1'] = [4, 12, 4, 4, 4, 4, 14],
            ['2'] = [14, 17, 1, 2, 4, 8, 31],
            ['3'] = [30, 1, 1, 14, 1, 1, 30],
            ['4'] = [2, 6, 10, 18, 31, 2, 2],
            ['5'] = [31, 16, 16, 30, 1, 1, 30],
            ['6'] = [14, 16, 16, 30, 17, 17, 14],
            ['7'] = [31, 1, 2, 4, 8, 8, 8],
            ['8'] = [14, 17, 17, 14, 17, 17, 14],
            ['9'] = [14, 17, 17, 15, 1, 1, 14]
        };

        public void RenderLootLabels(
            ActorDocument document,
            Guid? selectedActor,
            Matrix4x4 viewProjection,
            Vector3 cameraPosition,
            int width,
            int height,
            Guid? selectedLocation)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _lines.Clear();

            foreach (LevelActor actor in document.Actors)
            {
                if (actor.Kind != ActorKind.LootSpawn)
                {
                    continue;
                }

                bool selected = actor.Id == selectedActor;

                if (!selected &&
                    Vector3.DistanceSquared(
                        actor.Position,
                        cameraPosition) > 14400f)
                {
                    continue;
                }

                Vector4 color = selected
                    ? new Vector4(1f, 0.9f, 0.2f, 1f)
                    : new Vector4(0.85f, 0.85f, 0.65f, 1f);

                if (!actor.Enabled)
                {
                    color = new Vector4(
                        0.45f, 0.45f, 0.45f, 1f);
                }

                DrawWorldLootLabel(
                    actor.LootCategory,
                    actor.Position + Vector3.UnitY * 1.35f,
                    viewProjection,
                    width,
                    height,
                    color);

                if (!selected)
                {
                    continue;
                }

                for (int index = 0;
                     index < actor.LootLocations.Count;
                     index++)
                {
                    LootSpawnLocation location =
                        actor.LootLocations[index];

                    Vector3 position =
                        location.GetWorldPosition(actor) +
                        Vector3.UnitY * 0.5f;

                    Vector4 locationColor =
                        location.Id == selectedLocation
                            ? SelectedLootLocationColor
                            : LootLocationColor;

                    DrawWorldLootLabel(
                        $"L{index + 1}",
                        position,
                        viewProjection,
                        width,
                        height,
                        locationColor);
                }
            }

            Flush(Matrix4x4.Identity);
        }

        private void DrawWorldLootLabel(
            string text,
            Vector3 position,
            Matrix4x4 viewProjection,
            int width,
            int height,
            Vector4 color)
        {
            Vector4 clip = Vector4.Transform(
                new Vector4(position, 1f),
                viewProjection);

            if (clip.W <= 0.001f)
            {
                return;
            }

            Vector3 ndc = new(
                clip.X / clip.W,
                clip.Y / clip.W,
                clip.Z / clip.W);

            if (!float.IsFinite(ndc.X) ||
                !float.IsFinite(ndc.Y) ||
                !float.IsFinite(ndc.Z) ||
                MathF.Abs(ndc.X) > 1f ||
                MathF.Abs(ndc.Y) > 1f ||
                ndc.Z < 0f ||
                ndc.Z > 1f)
            {
                return;
            }

            DrawActorLabel(
                text,
                ndc,
                width,
                height,
                color);
        }

        private void DrawActorLabel(
            string text,
            Vector3 center,
            int width,
            int height,
            Vector4 color)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "LOOT";
            }

            text = text.ToUpperInvariant();

            const float pixelSize = 1.8f;

            float stepX = pixelSize * 2f / width;
            float stepY = pixelSize * 2f / height;

            float totalWidth =
                (text.Length * 6 - 1) * stepX;

            float startX =
                center.X - totalWidth * 0.5f;

            for (int index = 0; index < text.Length; index++)
            {
                if (!ActorGlyphs.TryGetValue(
                        text[index],
                        out int[]? glyph))
                {
                    continue;
                }

                for (int row = 0; row < 7; row++)
                {
                    for (int column = 0; column < 5; column++)
                    {
                        if ((glyph[row] &
                             (1 << (4 - column))) == 0)
                        {
                            continue;
                        }

                        float x = startX +
                                  (index * 6 + column) * stepX;

                        float y = center.Y - row * stepY;

                        AddLine(
                            new Vector3(x, y, 0.5f),
                            new Vector3(
                                x + stepX * 0.9f,
                                y,
                                0.5f),
                            color);
                    }
                }
            }
        }
    }
}
