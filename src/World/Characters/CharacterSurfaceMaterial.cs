using System.Numerics;

namespace ZoneReborn.World.Characters;

public sealed record CharacterSurfaceMaterial
{
    public string? ColourMaskPath { get; init; }
    public string? DiffuseMaskPath { get; init; }
    public string? OpacityMaskPath { get; init; }
    public Vector4 ColourR { get; init; } = Vector4.One;
    public Vector4 ColourG { get; init; } = Vector4.One;
    public Vector4 ColourB { get; init; } = Vector4.One;
    public Vector4 ColourA { get; init; } = Vector4.One;
    public Vector4 PatternScaleShift { get; init; } = new(1, 1, 0, 0);
    public Vector2 PatternRotation { get; init; } = Vector2.UnitX;
    public bool ColourByUv2 { get; init; }
    public Vector4 OpacityScaleShift { get; init; } = new(1, 1, 0, 0);
    public bool DoubleSided { get; init; }
    public bool UsesAlpha { get; init; }
    public float AlphaCutoff { get; init; }
}
