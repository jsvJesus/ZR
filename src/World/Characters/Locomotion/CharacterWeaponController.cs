using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public enum CharacterWeaponState { Holstered, EquippingIn, EquippingOut, Held, HolsteringIn, HolsteringOut }

/// <summary>The original track-12 in/out sequence. Attachment transfer occurs at IN completion.</summary>
public sealed class CharacterWeaponController
{
    private readonly LocomotionLibrary _library;
    private const float CombatStanceDuration = 10f;
    private float _combatStanceRemaining;
    private float _time;
    private float _transitionTime;
    private bool _requestedHeld;
    private float _leftHandWeight = 1f;
    public CharacterWeaponDefinition? Definition { get; }
    public CharacterWeaponState State { get; private set; }
    public CharacterWeaponProfile? Profile { get; private set; }
    public bool IsChanging => State is not (CharacterWeaponState.Holstered or CharacterWeaponState.Held);
    public bool RequestedHeld => _requestedHeld;
    public bool InHands => State is CharacterWeaponState.Held or CharacterWeaponState.EquippingOut or CharacterWeaponState.HolsteringIn;
    public bool Armed => State is CharacterWeaponState.Held or CharacterWeaponState.EquippingIn or CharacterWeaponState.EquippingOut;
    public bool Relaxed { get; private set; } = true;
    public float RightCarryWeight { get; private set; }
    public float LeftGripWeight => TrackingWeight * (1 - RightCarryWeight) * _leftHandWeight;
    public string TrackerState { get; private set; } = "idle";
    public string TrackerStatus { get; private set; } = "bored";
    public CharacterAction? Overlay { get; private set; }
    public float Frame => Overlay is { } action ? action.Clip.FirstFrame
        + Math.Min(_time, action.Clip.Duration) * action.Clip.FrameRate * action.Clip.Direction : 0;
    public float OverlayWeight => Overlay == null ? 0 : Math.Clamp(Math.Min(
        _transitionTime / Math.Max(0.001f, Overlay.BlendIn),
        State is CharacterWeaponState.EquippingOut or CharacterWeaponState.HolsteringOut
            ? (Overlay.Clip.Duration - _time) / Math.Max(0.001f, Overlay.BlendOut) : 1), 0, 1);
    // During OUT, the right hand first carries the gun, then blends onto the tracker.
    public float TrackingWeight => State switch
    {
        CharacterWeaponState.Held => 1,
        CharacterWeaponState.EquippingOut when Overlay != null => Math.Clamp(
            1 - (Overlay.Clip.Duration - _time) / 0.3f, 0, 1),
        CharacterWeaponState.HolsteringIn => 1 - Math.Clamp(_time / 0.2f, 0, 1),
        _ => 0
    };

    public CharacterWeaponController(CharacterWeaponDefinition? definition, LocomotionLibrary library)
    {
        Definition = definition;
        _library = library;
        Profile = definition?.ResolveProfile("bored", "idle");
        RightCarryWeight = Profile?.ToAttachWeapon == "Right" ? 1f : 0f;
    }

    public void Request(bool held) => _requestedHeld = Definition != null && held;
    public void Toggle() => Request(!_requestedHeld);

    public void RestoreHeld()
    {
        if (Definition == null)
        {
            return;
        }

        _requestedHeld = true;
        State = CharacterWeaponState.Held;
        Overlay = null;

        _combatStanceRemaining = 0f;
        Relaxed = true;
    }

    public void Advance(
        float dt,
        bool canStart,
        bool crouch,
        bool sprint,
        bool aiming = false,
        bool firing = false)
    {
        if (Definition == null)
        {
            return;
        }

        // Activity can arrive during a jump or an equip sequence. Remember it
        // without replacing the action that is still playing.
        bool weaponReady = InHands || _requestedHeld;

        if (weaponReady && (aiming || firing))
        {
            _combatStanceRemaining = CombatStanceDuration;
        }
        else
        {
            _combatStanceRemaining = MathF.Max(
                0f,
                _combatStanceRemaining - MathF.Max(0f, dt)
            );
        }

        if (State == CharacterWeaponState.Holstered &&
            !_requestedHeld)
        {
            _combatStanceRemaining = 0f;
        }

        bool combatActive = _combatStanceRemaining > 0f;
        bool sprintRelaxed = sprint && !crouch;

        Relaxed = sprintRelaxed ||
                  (!combatActive && !(aiming && weaponReady));

        if (!IsChanging &&
            canStart &&
            _requestedHeld != (State == CharacterWeaponState.Held))
        {
            State = _requestedHeld
                ? CharacterWeaponState.EquippingIn
                : CharacterWeaponState.HolsteringIn;

            _transitionTime = 0f;
            StartPhase();
        }

        if (Overlay == null)
        {
            return;
        }

        float remaining = dt * Definition.EquipSpeed;
        _transitionTime += remaining;

        while (Overlay is { } action)
        {
            float step = Math.Min(
                remaining,
                Math.Max(0f, action.Clip.Duration - _time)
            );

            _time += step;
            remaining -= step;

            if (_time < action.Clip.Duration)
            {
                break;
            }

            State = State switch
            {
                CharacterWeaponState.EquippingIn =>
                    CharacterWeaponState.EquippingOut,

                CharacterWeaponState.EquippingOut =>
                    CharacterWeaponState.Held,

                CharacterWeaponState.HolsteringIn =>
                    CharacterWeaponState.HolsteringOut,

                _ => CharacterWeaponState.Holstered
            };

            StartPhase();

            if (remaining <= 0f)
            {
                break;
            }
        }
    }

    public void UpdatePose(float dt, CharacterAction action, bool airborne, bool aiming)
    {
        // AvatarTrackers.modelMatchNotifier uses the matched action, not requested
        // input/speed. Jump and posture clips retain the preceding movement key.
        string[] parts = action.Name.Split('_');
        if (!action.Stop && parts.Length == 4 && (parts[2] is "stay" or "crouch")
            && (parts[0] is "idle" or "walk" or "run" or "sprint" or "turn"))
        {
            TrackerState = parts[2] == "crouch"
                ? parts[0] == "idle" ? "crouch_idle" : parts[0] == "run" ? "crouch_run" : "crouch_walk"
                : parts[0] == "turn" ? "idle" : parts[0];
        }

        TrackerStatus = aiming ? "aim" : Relaxed ? "bored" : "incombat";
        if (Definition == null) return;
        string state = TrackerState == "sprint" && TrackerStatus != "bored" ? "run" : TrackerState;
        var target = Definition.ResolveProfile(TrackerStatus, state);

        float blend = 1 - MathF.Exp(-12 * dt);
        if (Profile is { } previous)
            Profile = target with
            {
                GunPosition = Vector3.Lerp(previous.GunPosition, target.GunPosition, blend),
                GunRootPosition = Vector3.Lerp(previous.GunRootPosition, target.GunRootPosition, blend),
                GunRootRoll = previous.GunRootRoll + MathF.IEEERemainder(target.GunRootRoll - previous.GunRootRoll, MathF.Tau) * blend,
                LeftHandPosition = Vector3.Lerp(previous.LeftHandPosition, target.LeftHandPosition, blend),
                LeftHandRotation = Vector3.Lerp(previous.LeftHandRotation, target.LeftHandRotation, blend),
                RightHandPosition = Vector3.Lerp(previous.RightHandPosition, target.RightHandPosition, blend),
                RightHandRotation = Vector3.Lerp(previous.RightHandRotation, target.RightHandRotation, blend),
                GunPitchLimits = Vector2.Lerp(previous.GunPitchLimits, target.GunPitchLimits, blend),
                GunYawLimits = Vector2.Lerp(previous.GunYawLimits, target.GunYawLimits, blend)
            };
        else Profile = target;

        float carry = airborne || target.ToAttachWeapon == "Right"
            ? 1f
            : 0f;

        RightCarryWeight += (carry - RightCarryWeight) * blend;
        float leftWeight = target.LeftHandFree ? 0f : 1f;
        _leftHandWeight += (leftWeight - _leftHandWeight) * blend;
        if (MathF.Abs(leftWeight - _leftHandWeight) < 0.001f) _leftHandWeight = leftWeight;

        if (MathF.Abs(carry - RightCarryWeight) < 0.001f)
        {
            RightCarryWeight = carry;
        }
    }

    private void StartPhase()
    {
        _time = 0;
        if (!IsChanging) { Overlay = null; return; }
        string verb = State is CharacterWeaponState.EquippingIn or CharacterWeaponState.EquippingOut ? "equipweapon" : "unEquipweapon";
        string phase = State is CharacterWeaponState.EquippingIn or CharacterWeaponState.HolsteringIn ? "in" : "out";
        Overlay = _library.Actions[$"{verb}_{Definition!.HolsterNode}_{phase}"];
    }
}
