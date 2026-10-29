using System.Numerics;
using ZoneReborn.World.Roads;

namespace Editor.Viewport
{
    public sealed partial class DxViewportHost
    {
        public bool RoadEditingEnabled { get; set; }
        public event Action<SourceRoad, RoadSegment>? RoadSegmentPicked;

        public void SelectRoadSegment(SourceRoad? road, RoadSegment? segment) =>
            _renderer?.SelectRoadSegment(road, segment);

        public void ReloadRoadTextures() => _renderer?.ReloadRoadTextures();

        private void PickRoadSegment(int x, int y)
        {
            if (!RoadEditingEnabled || IsPlaying || IsStarting || _renderer == null ||
                !GetClientRect(_windowHandle, out NativeRect rect)) return;
            int width = rect.Right - rect.Left, height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) return;
            var forward = Vector3.Normalize(_camera.LookDirection);
            var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
            var up = Vector3.Cross(forward, right);
            float scale = MathF.Tan(1.05f * 0.5f);
            var direction = Vector3.Normalize(forward + right * ((2f * x / width - 1) * width / height * scale)
                + up * ((1 - 2f * y / height) * scale));
            try
            {
                if (_renderer.PickRoadSegment(_camera.Position, direction) is { } selection)
                    RoadSegmentPicked?.Invoke(selection.Road, selection.Segment);
            }
            catch (Exception exception) { RendererError?.Invoke(exception.Message); }
        }

        public void FocusRoadSegment(SourceRoad road, RoadSegment segment)
        {
            if (IsPlaying || IsStarting) return;
            var mesh = RoadMesh.Build(road, segment);
            var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
            foreach (var vertex in mesh.Vertices)
            {
                min = Vector3.Min(min, vertex.Position); max = Vector3.Max(max, vertex.Position);
            }
            var center = (min + max) * 0.5f;
            if (segment.FollowTerrain && _level != null)
            {
                var surface = new ZoneReborn.World.Terrain.TerrainSurface(_level);
                if (surface.TryGetHeight(center.X, center.Z, out float ground))
                {
                    int a = segment.Index, b = segment.Index + 1;
                    float offset = (road.Nodes[a].Y - road.OriginalNodes[a].Y + road.Nodes[b].Y - road.OriginalNodes[b].Y) * 0.5f;
                    int row = (segment.Index * SourceRoad.SamplesPerSegment + SourceRoad.SamplesPerSegment / 2) * 4;
                    float sourceOffset = road.TerrainOffsets.Length == road.Vertices.Length
                        ? Enumerable.Range(0, 4).Average(c => road.TerrainOffsets[row + c]) : 0;
                    center.Y = ground + sourceOffset + offset + 0.02f;
                }
            }
            float distance = Math.Clamp(Vector3.Distance(min, max) * 0.9f, 20, 1000);
            SetCamera(new(center.X, center.Y + distance * 0.7f, center.Z - distance,
                0, -MathF.Atan(0.7f), Math.Clamp(distance, 8, 250)));
        }
    }
}
