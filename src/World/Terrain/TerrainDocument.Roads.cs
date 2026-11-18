using ZoneReborn.World.Roads;

namespace ZoneReborn.World.Terrain;

public sealed partial class TerrainDocument
{
    /// <summary>Enables a derived corridor; raw height arrays and terrain undo records are never changed.</summary>
    public int ConformRoad(RoadDefinition road, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(road);
        cancellationToken.ThrowIfCancellationRequested();
        RoadStorage.Validate(road);
        if (!road.FollowTerrain)
            throw new InvalidOperationException("Модификатор Terrain доступен для дорог с привязкой к рельефу.");
        road.TerrainModifierEnabled = true;
        road.PreserveSourceElevation = false;
        road.TerrainConformed = false;
        InvalidateRoadModifiers(road);
        EnsureRoadModifiers(cancellationToken);
        return _roadProfiles.TryGetValue(road, out var profile) ? profile.Segments.Length : 0;
    }
}
