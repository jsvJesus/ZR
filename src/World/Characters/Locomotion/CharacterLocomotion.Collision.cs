using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private const float StandingHeight = 1.75f;
    private const float CrouchingHeight = 1.1f;
    private const float StepHeight = 0.3f;
    private float _supportSurfaceY = float.NaN;
    private float CollisionHeight => _crouched ? CrouchingHeight : StandingHeight;

    private bool GroundBelow(float x, float z, float maximumY, out float height)
    {
        if (_surface.TryGetGround(x, z, maximumY, out height)) return true;
        height = float.NegativeInfinity; // An open terrain hole has no heightfield floor.
        return _surface.Contains(x, z);
    }

    private bool MoveAmongObjects(Vector2 displacement)
    {
        var target = Position + new Vector3(displacement.X, 0, displacement.Y);
        if (!_surface.Contains(target.X, target.Z)) return false;
        if (!_surface.TryGetHeight(target.X, target.Z, out float terrain)) terrain = Position.Y;
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
        // At a tread edge the rounded capsule's feet can be below the actual support.
        // Measure the next step from the last accepted surface, never a speculative probe.
        float currentSurfaceY = float.IsFinite(_supportSurfaceY) ? Math.Max(Position.Y, _supportSurfaceY) : Position.Y;
        float maximumSurfaceY = currentSurfaceY + StepHeight;
        bool hasSupport = TryGroundSupport(moved, StepHeight, maximumTerrainRise,
            out var grounded, out float surfaceY, maximumSurfaceY);
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
                    TryGroundSupport(stepped, rise + StepHeight, maximumTerrainRise,
                        out var supported, out float stepSurfaceY, maximumSurfaceY) &&
                    supported.Y >= Position.Y - StepHeight && supported.Y <= Position.Y + StepHeight + 0.01f)
                {
                    // A rounded capsule reaches a stair edge before its center reaches the tread.
                    // Keep that swept support instead of snapping to the lower floor at the center.
                    Position = supported;
                    _supportSurfaceY = stepSurfaceY;
                    Grounded = true;
                    return true;
                }
            }
        }

        // Recover only an unsupported upward push beyond a legitimate ramp rise.
        // A horizontal wall slide can also leave a real ledge and must be free to fall.
        if (blocked && !hasSupport && moved.Y > Position.Y + maximumTerrainRise + 0.01f &&
            TryGroundSupport(Position, 0.02f, 0.001f,
                out var previousSupport, out float previousSurfaceY, maximumSurfaceY))
        {
            Position = previousSupport;
            _supportSurfaceY = previousSurfaceY;
            Grounded = true;
            return true;
        }

        Position = hasSupport ? grounded : moved;
        if (hasSupport) _supportSurfaceY = surfaceY;
        Grounded = hasSupport;
        return true;
    }

    private bool TryGroundSupport(Vector3 candidate, float maximumDrop, float maximumTerrainRise, out Vector3 supported,
        out float surfaceY, float maximumSurfaceY = float.PositiveInfinity)
    {
        supported = candidate;
        surfaceY = candidate.Y;
        bool hasTerrain = _surface.TryGetHeight(candidate.X, candidate.Z, out float terrain);
        bool found = _surface.TrySnapToObject(candidate, CollisionHeight, maximumDrop, out var objectSupport,
                out float objectSurfaceY, maximumSurfaceY) &&
            (!hasTerrain || objectSupport.Y >= terrain - 0.001f) &&
            _surface.CanOccupy(objectSupport, CollisionHeight);
        if (found)
        {
            supported = objectSupport;
            surfaceY = objectSurfaceY;
        }

        // Object support must come from the entire capsule, including contacts outside
        // its center ray. Terrain has no scene collider, so retain its heightfield floor.
        if (hasTerrain && terrain >= candidate.Y - maximumDrop &&
            terrain <= Position.Y + maximumTerrainRise && (!found || terrain > supported.Y))
        {
            var terrainSupport = candidate with { Y = terrain };
            if (_surface.CanOccupy(terrainSupport, CollisionHeight))
            {
                supported = terrainSupport;
                surfaceY = terrain;
                found = true;
            }
        }
        return found;
    }
}
