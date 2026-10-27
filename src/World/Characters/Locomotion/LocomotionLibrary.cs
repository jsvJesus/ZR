using System.Globalization;
using ZoneReborn.World.Assets;
using ZoneReborn.World.Characters.Formats;

namespace ZoneReborn.World.Characters.Locomotion;

public sealed class LocomotionLibrary
{
    public IReadOnlyDictionary<string, CharacterAction> Actions { get; }
    public CharacterAction[] MovementActions { get; }
    public CharacterAction[] StopActions { get; }
    public CharacterAction Idle => Actions["idle_0_stay_unarmed"];

    private LocomotionLibrary(Dictionary<string, CharacterAction> actions)
    {
        Actions = actions;
        MovementActions = actions.Values.Where(a => a.Trigger != null && !a.Stop && !a.Turn && a.Track == 0).ToArray();
        StopActions = actions.Values.Where(a => a.Stop).ToArray();
    }

    public static LocomotionLibrary Read(AssetPaths assets, PackedSection model, CancellationToken cancellationToken)
    {
        var animationSections = model.All("animation").ToDictionary(a => a.String("name"), StringComparer.Ordinal);
        var clips = new Dictionary<string, CharacterClip>(StringComparer.Ordinal);
        var animations = new Dictionary<string, SourceAnimation>(StringComparer.OrdinalIgnoreCase);
        var actions = new Dictionary<string, CharacterAction>(StringComparer.Ordinal);
        string[] explicitActions =
        [
            "pose_stay_to_crouch_unarmed", "pose_crouch_to_stay_unarmed",
            "turn_left90_stay_unarmed", "turn_right90_stay_unarmed",
            "turn_left90_crouch_unarmed", "turn_right90_crouch_unarmed",
            JumpDefinition.HardLanding
        ];
        var required = new HashSet<string>(explicitActions, StringComparer.Ordinal);
        foreach (var jump in new[] { JumpDefinition.Right, JumpDefinition.Left, JumpDefinition.Fall, JumpDefinition.Stationary })
            required.UnionWith([jump.Up, jump.Fly, jump.FlyHigh, jump.DownMove, jump.DownStop]);
        var availableCaps = new HashSet<int> { (int)AnimationCap.Unarmed, (int)AnimationCap.Crouching };
        foreach (var section in model.All("action"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string name = section.String("name");
            var match = section.Find("match");
            var trigger = ReadMatch(match?.Find("trigger"));
            int track = (int)Number(section, "track", 0);
            // Exclude weapon, water, crawl and NPC tracks from the currently unarmed playable character.
            bool compatible = trigger != null && trigger.CapsOn.Contains((int)AnimationCap.Unarmed)
                && trigger.CapsOn.All(availableCaps.Contains) && track == 0;
            compatible |= trigger != null && (Flag(match, "stopLeftAction") || Flag(match, "stopRightAction"))
                && trigger.CapsOn.All(availableCaps.Contains) && !trigger.CapsOff.Contains((int)AnimationCap.Unarmed) && track == 0;
            if (!compatible && !required.Contains(name)) continue;
            string animationName = section.String("animation");
            if (!clips.TryGetValue(animationName, out var clip))
            {
                var a = animationSections[animationName];
                string path = AssetPathResolver.Resolve(assets.Root, a.String("nodes") + ".animation");
                if (!animations.TryGetValue(path, out var animation)) animations.Add(path, animation = AnimationReader.Read(path));
                float first = Number(a, "firstFrame", 0), last = Number(a, "lastFrame", animation.Frames);
                float fps = Number(a, "frameRate", 30);
                if (first < 0 || last < 0 || first > animation.Frames || last > animation.Frames || first == last || fps <= 0)
                    throw new InvalidDataException($"Invalid frame range for {animationName}.");
                var cues = a.Find("cues")?.All("entry").Select(c => new AnimationCue(Number(c, "time", 0), c.String("name")))
                    .Where(c => c.Frame >= Math.Min(first, last) && c.Frame <= Math.Max(first, last))
                    .OrderBy(c => last >= first ? c.Frame : -c.Frame).ToArray() ?? [];
                var priorities = a.Find("NodePriority")?.Children.ToDictionary(n => n.Name, n => Convert.ToInt32(n.Value), StringComparer.Ordinal)
                    ?? new Dictionary<string, int>(StringComparer.Ordinal);
                clips.Add(animationName, clip = new(animationName, animation, fps, first, last, cues, priorities));
            }
            var action = new CharacterAction(name, clip, trigger, ReadMatch(match?.Find("cancel")),
                Number(section, "blendInTime", 0.2f), Number(section, "blendOutTime", 0.2f),
                Flag(section, "isMovement"), Flag(section, "isImpacting"), Flag(section, "isIgnorGravity"),
                Flag(match, "scalePlaybackSpeed"), Flag(match, "stopLeftAction"), Flag(match, "stopRightAction"),
                Number(match, "prevActionPlayTime", 0), track, Flag(match, "feetFollowDirection"));
            if (!actions.TryAdd(name, action)) throw new InvalidDataException($"Duplicate locomotion action {name}.");
        }
        foreach (string name in required.Append("idle_0_stay_unarmed").Append("idle_0_crouch_unarmed"))
            if (!actions.ContainsKey(name)) throw new InvalidDataException($"Missing locomotion action {name}.");
        return new(actions);
    }

    private static ActionMatch? ReadMatch(PackedSection? section) => section == null ? null : new(
        Number(section, "minEntitySpeed", 0), Number(section, "maxEntitySpeed", float.MaxValue),
        Number(section, "minModelYaw", -360), Number(section, "maxModelYaw", 360),
        Number(section, "minEntityAux1", -360), Number(section, "maxEntityAux1", 360),
        Caps(section.Find("capsOn")), Caps(section.Find("capsOff")));

    private static int[] Caps(PackedSection? section) => section == null ? [] : section.Text
        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
    private static bool Flag(PackedSection? section, string name) => section?.Find(name)?.Value is true;
    private static float Number(PackedSection? section, string name, float fallback)
    {
        var value = section?.Find(name);
        if (value == null) return fallback;
        return value.Value is float[] array ? array.Single() : float.Parse(value.Text, CultureInfo.InvariantCulture);
    }
}
