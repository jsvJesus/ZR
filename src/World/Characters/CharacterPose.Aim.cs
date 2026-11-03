using System.Numerics;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    public float AimPitch { get; set; }

    private void ApplyUpperBodyAim(
        ref Matrix4x4 transform,
        string nodeName,
        float torsoYaw)
    {
        float weight = nodeName switch
        {
            "Spine" => 0.20f,
            "Spine1" => 0.35f,
            "Spine2" => 0.45f,
            _ => 0f
        };

        float pitch = AimPitch * weight;

        if (pitch == 0f &&
            (nodeName != "Spine2" || torsoYaw == 0f))
        {
            return;
        }

        var pivot = transform.Translation;

        var rotation = Matrix4x4.CreateRotationX(-pitch);

        if (nodeName == "Spine2")
        {
            rotation *= Matrix4x4.CreateRotationY(torsoYaw);
        }

        transform *= rotation;
        transform.Translation = pivot;
    }
}
