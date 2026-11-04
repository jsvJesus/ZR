using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private const float StandingHeight = 1.75f;
    private const float CrouchingHeight = 1.1f;
    private const float StepHeight = 0.3f;
    private float CollisionHeight => _crouched ? CrouchingHeight : StandingHeight;

    private bool GroundBelow(float x, float z, float maximumY, out float height) =>
        _surface.TryGetGround(x, z, maximumY, out height);

    private bool MoveAmongObjects(Vector2 displacement)
    {
        var target = Position + new Vector3(displacement.X, 0, displacement.Y);
        if (!_surface.TryGetHeight(target.X, target.Z, out float terrain)) return false;
        float slope = MathF.Tan(LocomotionSettings.MaximumSlopeDegrees * MathF.PI / 180);
        // Model stairs may be stepped onto; the existing terrain slope limit still applies.
        if (displacement.LengthSquared() > 1e-10f && terrain - Position.Y > displacement.Length() * slope + 0.001f)
        {
            if (!_surface.TryGetHeight(Position.X, Position.Z, out float here) ||
                !_surface.TryGetHeight(Position.X + 0.1f, Position.Z, out float hx) ||
                !_surface.TryGetHeight(Position.X, Position.Z + 0.1f, out float hz)) return false;
            var gradient = new Vector2(hx - here, hz - here);
            if (gradient.LengthSquared() < 1e-6f) return false;
            gradient = Vector2.Normalize(gradient);
            displacement -= gradient * Math.Max(0, Vector2.Dot(displacement, gradient));
            target = Position + new Vector3(displacement.X, 0, displacement.Y);
            if (!_surface.TryGetHeight(target.X, target.Z, out terrain) ||
                terrain - Position.Y > displacement.Length() * slope + 0.001f) return false;
        }

        var moved = _surface.MoveCharacter(Position, target, CollisionHeight);
        float maximumTerrainRise = displacement.Length() * slope + 0.001f;
        bool hasSupport = TryGroundSupport(moved, StepHeight, maximumTerrainRise, out var grounded, StepHeight);
        var remaining = new Vector2(target.X - moved.X, target.Z - moved.Z);
        bool blocked = displacement.LengthSquared() > 1e-10f &&
            remaining.LengthSquared() > Math.Max(1e-10f, displacement.LengthSquared() * 0.01f);
        if (blocked)
        {
            // Use the clearance actually available. Requiring the full 0.3 m raise
            // would reject a low step beneath a ceiling even when the capsule fits.
            var raised = _surface.MoveCharacter(Position, Position + Vector3.UnitY * StepHeight, CollisionHeight);
            float rise = raised.Y - Position.Y;
            if (rise > 0.002f && rise <= StepHeight + 0.01f)
            {
                var stepped = _surface.MoveCharacter(raised, target with { Y = raised.Y }, CollisionHeight);
                var stepRemaining = new Vector2(target.X - stepped.X, target.Z - stepped.Z);
                if (stepRemaining.LengthSquared() < remaining.LengthSquared() &&
                    TryGroundSupport(stepped, rise + StepHeight, maximumTerrainRise, out var supported, StepHeight) &&
                    supported.Y >= Position.Y - StepHeight && supported.Y <= Position.Y + StepHeight + 0.01f)
                {
                    // A rounded capsule reaches a stair edge before its center reaches the tread.
                    // Keep that swept support instead of snapping to the lower floor at the center.
                    Position = supported;
                    Grounded = true;
                    return true;
                }
            }
        }

        // Recover only an unsupported upward push beyond a legitimate ramp rise.
        // A horizontal wall slide can also leave a real ledge and must be free to fall.
        if (blocked && !hasSupport && moved.Y > Position.Y + maximumTerrainRise + 0.01f &&
            TryGroundSupport(Position, 0.02f, 0.001f, out var previousSupport, StepHeight))
        {
            Position = previousSupport;
            Grounded = true;
            return true;
        }

        Position = hasSupport ? grounded : moved;
        Grounded = hasSupport;
        return true;
    }

    private bool TryGroundSupport(Vector3 candidate, float maximumDrop, float maximumTerrainRise, out Vector3 supported,
        float maximumObjectRise = float.PositiveInfinity)
    {
        supported = candidate;
        bool hasTerrain = _surface.TryGetHeight(candidate.X, candidate.Z, out float terrain);
        bool found = _surface.TrySnapToObject(candidate, CollisionHeight, maximumDrop, out var objectSupport,
                Position.Y + maximumObjectRise) &&
            (!hasTerrain || objectSupport.Y >= terrain - 0.001f) &&
            _surface.CanOccupy(objectSupport, CollisionHeight);
        if (found) supported = objectSupport;

        // Object support must come from the entire capsule, including contacts outside
        // its center ray. Terrain has no scene collider, so retain its heightfield floor.
        if (hasTerrain && terrain >= candidate.Y - maximumDrop &&
            terrain <= Position.Y + maximumTerrainRise && (!found || terrain > supported.Y))
        {
            var terrainSupport = candidate with { Y = terrain };
            if (_surface.CanOccupy(terrainSupport, CollisionHeight))
            {
                supported = terrainSupport;
                found = true;
            }
        }
        return found;
    }
}
