using System.Numerics;

namespace ZoneReborn.World.Characters;

public sealed partial class CharacterPose
{
    public float AimPitch { get; set; }
    public float TorsoRoll { get; set; }

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
        float roll = TorsoRoll * weight;

        if (pitch == 0f && roll == 0f &&
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

        if (roll != 0)
        {
            // Roll around the horizontal view direction in avatar model space.
            // The pelvis and feet keep the locomotion pose; descendants inherit
            // each spine contribution before the weapon and clothing IKs run.
            var forward = new Vector3(MathF.Sin(LookYaw), 0, MathF.Cos(LookYaw));
            rotation *= Matrix4x4.CreateFromAxisAngle(forward, roll);
        }

        transform *= rotation;
        transform.Translation = pivot;
    }
}
