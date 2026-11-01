namespace ZoneReborn.World.Characters.Locomotion;

public sealed partial class CharacterLocomotion
{
    private float _turnStartYaw;
    private float _turnSign;
    private float _turnProgress;

    private bool IsTurning =>
        State is AnimState.TurningLeft or AnimState.TurningRight;

    private bool TryStartTurn(float cameraYaw)
    {
        if (!Grounded || IsLanding || IsPostureTransition || Weapon.IsChanging)
        {
            return false;
        }

        float angle = Wrap(cameraYaw - Yaw);

        float threshold = _crouched
            ? LocomotionSettings.CrouchTurnThreshold
            : LocomotionSettings.StandingTurnThreshold;

        if (MathF.Abs(angle) + 0.00001f < threshold)
        {
            return false;
        }

        _turnStartYaw = Yaw;
        _turnSign = MathF.Sign(angle);
        _turnProgress = 0;

        string posture = _crouched ? "crouch_unarmed" : "stay_incombat";
        string direction = angle > 0 ? "right" : "left";

        string actionName =
            $"turn_{direction}45_{posture}";

        if (!_library.Actions.TryGetValue(actionName, out var action))
        {
            return false;
        }

        State = angle > 0
            ? AnimState.TurningRight
            : AnimState.TurningLeft;

        Animator.Play(action, false);

        return true;
    }

    private void AdvanceTurn(float dt, float cameraYaw)
    {
        Animator.Advance(dt, 0);

        var clip = Animator.Current.Clip;

        float startYaw = clip.RootYaw(clip.FirstFrame);
        float currentYaw = clip.RootYaw(Animator.Frame);
        float endYaw = clip.RootYaw(clip.LastFrame);

        float total = Wrap(endYaw - startYaw);
        float traveled = Wrap(currentYaw - startYaw);

        float progress;

        if (MathF.Abs(total) > 0.05f)
        {
            progress = Math.Clamp(
                traveled / total,
                0f,
                1f
            );
        }
        else
        {
            float phase = Math.Clamp(Animator.Phase, 0f, 1f);

            progress = phase * phase * (3f - 2f * phase);
        }

        _turnProgress = MathF.Max(
            _turnProgress,
            progress
        );

        Yaw = Wrap(
            _turnStartYaw +
            _turnSign * LocomotionSettings.TurnAngle * _turnProgress
        );

        if (!Animator.Complete)
        {
            return;
        }

        Yaw = Wrap(
            _turnStartYaw +
            _turnSign * LocomotionSettings.TurnAngle
        );

        State = _crouched
            ? AnimState.CrouchIdle
            : AnimState.Idle;

        // Continue a large turn without inserting an idle clip between steps.
        if (TryStartTurn(cameraYaw)) return;

        Animator.Play(
            _matcher.Idle(_caps),
            true
        );
    }
}
