using System;
using System.Collections.Generic;
using UnityEngine;
using FluidHTN;
using FluidHTN.Contexts;
using FluidHTN.Factory;
using FluidHTN.Debug;
using FluidHTN.Operators;

public class AiContext : BaseContext
{
    [Header("--- CORE REFS ---")]
    public AIAgent Agent;
    public GroupAgent Group;

    [Header("--- BLACKBOARD DATA ---")]
    public Transform Target;
    public Vector3 SuspectedLocation;
    public Vector3 LastKnownPosition;
    public ActiveSkill CurrentSelectedSkill { get; set; } = null;
    public AISkillData CurrentExecutingSkill { get; set; } = null;

    [Header("--- PATROL SYSTEM ---")]
    public AIPatrolPath PatrolPath;
    public int CurrentWaypointIndex = 0;
    public Vector3 CurrentMoveDestination;

    public Vector3? LookFocusOverride { get; set; } = null;
    public bool IsScanning { get; set; } = false;

    // --- BẮT BUỘC BỞI FLUID HTN ---
    public override IFactory Factory { get; protected set; } = new DefaultFactory();
    public override IPlannerState PlannerState { get; protected set; } = new DefaultPlannerState();
    public override List<string> MTRDebug { get; set; } = null;
    public override List<string> LastMTRDebug { get; set; } = null;
    public override bool DebugMTR => false;
    public override Queue<IBaseDecompositionLogEntry> DecompositionLog { get; set; } = null;
    public override bool LogDecomposition => false;

    private byte[] _worldState = new byte[Enum.GetValues(typeof(AIWorldState)).Length];
    public override byte[] WorldState => _worldState;

    public bool HasState(AIWorldState state)
    {
        return HasState((int)state, 1);
    }

    public void SetState(AIWorldState state, bool value, EffectType type = EffectType.Permanent)
    {
        SetState((int)state, (byte)(value ? 1 : 0), true, type);
    }

    public void SetTarget(Transform target)
    {
        Target = target;
        SetState(AIWorldState.HasTarget, target != null, EffectType.Permanent);
    }
}