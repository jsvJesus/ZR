using System.Numerics;

namespace ZoneReborn.World.Characters.Locomotion;

public enum CharacterWeaponState { Holstered, EquippingIn, EquippingOut, Held, HolsteringIn, HolsteringOut }

/// <summary>The original track-12 in/out sequence. Attachment transfer occurs at IN completion.</summary>
public sealed class CharacterWeaponController
{
    private readonly LocomotionLibrary _library;
    private float _time;
    private float _transitionTime;
    private bool _requestedHeld;
    private bool _profileCrouch;
    private bool _profileMoving;
    private bool _profileSprint;
    private bool _profileRunning;
    public CharacterWeaponDefinition? Definition { get; }
    public CharacterWeaponState State { get; private set; }
    public CharacterWeaponProfile? Profile { get; private set; }
    public bool IsChanging => State is not (CharacterWeaponState.Holstered or CharacterWeaponState.Held);
    public bool RequestedHeld => _requestedHeld;
    public bool InHands => State is CharacterWeaponState.Held or CharacterWeaponState.EquippingOut or CharacterWeaponState.HolsteringIn;
    public bool Armed => State is CharacterWeaponState.Held or CharacterWeaponState.EquippingIn or CharacterWeaponState.EquippingOut;
    public bool Relaxed { get; private set; }
    public float RightCarryWeight { get; private set; }
    public float LeftGripWeight => TrackingWeight * (1 - RightCarryWeight);
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
        Profile = definition?.ResolveProfile(false, false, false);
    }

    public void Request(bool held) => _requestedHeld = Definition != null && held;
    public void Toggle() => Request(!_requestedHeld);

    public void RestoreHeld()
    {
        if (Definition == null) return;
        _requestedHeld = true;
        State = CharacterWeaponState.Held;
        Overlay = null;
    }

    public void Advance(float dt, bool canStart, bool crouch, bool sprint)
    {
        if (Definition == null) return;
        Relaxed = sprint && !crouch;
        if (!IsChanging && canStart && _requestedHeld != (State == CharacterWeaponState.Held))
        {
            State = _requestedHeld ? CharacterWeaponState.EquippingIn : CharacterWeaponState.HolsteringIn;
            _transitionTime = 0;
            StartPhase();
        }
        if (Overlay == null) return;
        float remaining = dt * Definition.EquipSpeed;
        _transitionTime += remaining;
        while (Overlay is { } action)
        {
            float step = Math.Min(remaining, Math.Max(0, action.Clip.Duration - _time));
            _time += step;
            remaining -= step;
            if (_time < action.Clip.Duration) break;
            State = State switch
            {
                CharacterWeaponState.EquippingIn => CharacterWeaponState.EquippingOut,
                CharacterWeaponState.EquippingOut => CharacterWeaponState.Held,
                CharacterWeaponState.HolsteringIn => CharacterWeaponState.HolsteringOut,
                _ => CharacterWeaponState.Holstered
            };
            StartPhase();
            if (remaining <= 0) break;
        }
    }

    public void UpdatePose(float dt, CharacterAction action, bool airborne)
    {
        if (Definition == null) return;
        // AvatarTrackers.modelMatchNotifier uses the matched action, not requested
        // input/speed. Jump and posture clips retain the preceding movement key.
        string[] parts = action.Name.Split('_');
        if (parts.Length == 4 && (parts[2] is "stay" or "crouch")
            && (parts[0] is "idle" or "walk" or "run" or "sprint" or "turn"))
        {
            _profileCrouch = parts[2] == "crouch";
            _profileMoving = parts[0] is "walk" or "run" or "sprint";
            _profileSprint = parts[0] == "sprint";
            _profileRunning = parts[0] is "run" or "sprint";
        }
        var target = Definition.ResolveProfile(_profileCrouch, _profileMoving, _profileSprint,
            inCombat: !Relaxed, running: _profileRunning);
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
                RightHandRotation = Vector3.Lerp(previous.RightHandRotation, target.RightHandRotation, blend)
            };
        else Profile = target;
        // hand_control.__check_for_weapon transfers Track -> Right in the air.
        // Let the jump clip move the carrying arm instead of pinning it to GUNR.
        float carry = airborne || target.ToAttachWeapon == "Right" ? 1 : 0;
        RightCarryWeight += (carry - RightCarryWeight) * blend;
        if (MathF.Abs(carry - RightCarryWeight) < 0.001f) RightCarryWeight = carry;
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
