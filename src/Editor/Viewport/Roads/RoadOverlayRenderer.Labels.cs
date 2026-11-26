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
            ['Z'] = [31, 1, 2, 4, 8, 16, 31]
        };

        public void RenderLootLabels(
            ActorDocument document,
            Guid? selectedActor,
            Matrix4x4 viewProjection,
            Vector3 cameraPosition,
            int width,
            int height)
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

                bool selected =
                    actor.Id == selectedActor;

                if (!selected &&
                    Vector3.DistanceSquared(
                        actor.Position,
                        cameraPosition) > 14400f)
                {
                    continue;
                }

                Vector3 position =
                    actor.Position +
                    Vector3.UnitY * 1.35f;

                Vector4 clip = Vector4.Transform(
                    new Vector4(position, 1f),
                    viewProjection);

                if (clip.W <= 0.001f)
                {
                    continue;
                }

                Vector3 ndc = new(
                    clip.X / clip.W,
                    clip.Y / clip.W,
                    clip.Z / clip.W);

                if (MathF.Abs(ndc.X) > 1f ||
                    MathF.Abs(ndc.Y) > 1f ||
                    ndc.Z < 0f ||
                    ndc.Z > 1f)
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

                DrawActorLabel(
                    actor.LootCategory,
                    ndc,
                    width,
                    height,
                    color);
            }

            Flush(Matrix4x4.Identity);
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
