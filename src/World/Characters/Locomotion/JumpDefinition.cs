namespace ZoneReborn.World.Characters.Locomotion;

// animation_jump_config.pyc_dis: J_RUN_R, J_RUN_L and J_IDLE_FALL.
public sealed record JumpDefinition(string Name, int FromFrame, int ToFrame,
    string Up, string Fly, string FlyHigh, string DownMove, string DownStop,
    float DownMoveTime, float DownStopTime, int ResumeFrame, float HardLandingTime = 0.4f)
{
    public const string HardLanding = "jump_fallDwnStrong";
    public static readonly JumpDefinition Right = new("J_RUN_R", 4, 14,
        "run_jump_up_right", "run_jump_fly_right", "run_jump_fly_OLD2",
        "run_jump_down_move_right", "run_jump_down_stop_right", 0.5f, 0.4f, 1);
    public static readonly JumpDefinition Left = new("J_RUN_L", 15, 19,
        "run_jump_up_left", "run_jump_fly_left", "run_jump_fly_OLD2",
        "run_jump_down_move_left", "run_jump_down_stop_left", 0.5f, 0.4f, 9);
    public static readonly JumpDefinition Fall = new("J_IDLE_FALL", -1, -1,
        "run_forward_jump_up__unarmed_OLD", "run_forward_jump_fly__unarmed_OLD", "run_forward_jump_fly__unarmed_OLD",
        HardLanding, HardLanding, 0.7f, 0.4f, -1, 0.7f);
    // The resource includes separate stationary jump phases; PlayerAvatar's bytecode was not decompiled.
    public static readonly JumpDefinition Stationary = new("IDLE", -1, -1,
        "idle_jump_up__unarmed", "idle_jump_fly__unarmed", "idle_jump_fly__unarmed",
        "idle_jump_down__unarmed", "idle_jump_down__unarmed", 0.4f, 0.4f, -1);

    public float WaitFrames(float current, float count) => FromFrame < 0 || current >= FromFrame && current <= ToFrame
        ? 0 : current < FromFrame ? FromFrame - current : count - current + FromFrame;
}
