using System;
using UnityEngine;

public enum MonsterIntent
{
    Patrol,
    Investigate,
    Search,
    Hunt,
    Ambush,
}

public enum MonsterTactic
{
    None,
    Listen,
    Intercept,
    Flank,
    Hold,
    DoubleBack,
    SilentApproach,
    DirectPursuit,
}

public enum MonsterDecisionReason
{
    None,
    AmbiguousStimulus,
    FreshEscapeVector,
    ExposedApproach,
    ChokePointControl,
    BrokenSearchLoop,
    UnawareTarget,
    DirectVisualPressure,
}

public struct MonsterDecisionContext
{
    public float Now;
    public float Suspicion;
    public bool HasVisualContact;
    public bool HasFreshStimulus;
    public bool TargetAwareOfMonster;
    public bool HasReachableTargetPath;
    public bool HasInterceptRoute;
    public bool HasFlankRoute;
    public bool HasHoldPoint;
    public bool HasCoverApproach;
    public bool HasRecentSearchFailure;
    public float DistanceToTarget;
    public float DistanceToLastKnownPosition;
    public float TimeSinceVisualContact;
    public float TimeSinceStimulus;
    public Vector3 SelfPosition;
    public Vector3 TargetPosition;
    public Vector3 TargetVelocity;
    public Vector3 LastKnownPosition;
    public MonsterStimulus PrimaryStimulus;
}

public readonly struct MonsterDecision
{
    public MonsterIntent Intent { get; }
    public MonsterTactic Tactic { get; }
    public MonsterDecisionReason Reason { get; }
    public Vector3 FocusPoint { get; }
    public float Score { get; }

    public MonsterDecision(
        MonsterIntent intent,
        MonsterTactic tactic,
        MonsterDecisionReason reason,
        Vector3 focusPoint,
        float score)
    {
        Intent = intent;
        Tactic = tactic;
        Reason = reason;
        FocusPoint = focusPoint;
        Score = score;
    }
}

public struct MonsterDecisionSettings
{
    public float RepeatWindow;
    public int MaxConsecutiveUses;
    public float ListenCooldown;
    public float InterceptCooldown;
    public float FlankCooldown;
    public float HoldCooldown;
    public float DoubleBackCooldown;
    public float SilentApproachCooldown;
    public float DirectPursuitCooldown;
    public float InterceptLeadSeconds;
    public float FlankOffsetDistance;
    public float HoldOffsetDistance;
    public float DoubleBackDistance;
    public float SilentApproachOffset;

    public static MonsterDecisionSettings CreateDefault()
    {
        return new MonsterDecisionSettings
        {
            RepeatWindow = 7f,
            MaxConsecutiveUses = 2,
            ListenCooldown = 1.5f,
            InterceptCooldown = 4f,
            FlankCooldown = 5f,
            HoldCooldown = 3f,
            DoubleBackCooldown = 4.5f,
            SilentApproachCooldown = 6f,
            DirectPursuitCooldown = 0.25f,
            InterceptLeadSeconds = 1.6f,
            FlankOffsetDistance = 3.5f,
            HoldOffsetDistance = 1.25f,
            DoubleBackDistance = 2.75f,
            SilentApproachOffset = 2.5f,
        };
    }
}

/// <summary>
/// Deterministic tactical chooser for psychological-horror monster AI.
/// Requires caller-provided time and environment facts; uses no Time/Random/MonoBehaviour state.
/// </summary>
public sealed class MonsterDecisionModel
{
    private readonly MonsterDecisionSettings settings;
    private readonly float[] lastUsedTimes;
    private MonsterTactic lastTactic;
    private float lastTacticTime;
    private int consecutiveUses;

    public MonsterDecisionSettings Settings => settings;
    public MonsterTactic LastTactic => lastTactic;
    public int ConsecutiveUses => consecutiveUses;

    public MonsterDecisionModel()
        : this(MonsterDecisionSettings.CreateDefault())
    {
    }

    public MonsterDecisionModel(MonsterDecisionSettings settings)
    {
        this.settings = settings;
        lastUsedTimes = new float[Enum.GetValues(typeof(MonsterTactic)).Length];
        Reset();
    }

    public void Reset()
    {
        for (int i = 0; i < lastUsedTimes.Length; i++)
            lastUsedTimes[i] = float.NegativeInfinity;

        lastTactic = MonsterTactic.None;
        lastTacticTime = float.NegativeInfinity;
        consecutiveUses = 0;
    }

    public MonsterDecision Evaluate(MonsterDecisionContext context)
    {
        MonsterDecision best = BuildBaselineDecision(context);
        float bestScore = best.Score;

        Consider(ref best, ref bestScore, context, BuildListenDecision(context));
        Consider(ref best, ref bestScore, context, BuildInterceptDecision(context));
        Consider(ref best, ref bestScore, context, BuildFlankDecision(context));
        Consider(ref best, ref bestScore, context, BuildHoldDecision(context));
        Consider(ref best, ref bestScore, context, BuildDoubleBackDecision(context));
        Consider(ref best, ref bestScore, context, BuildSilentApproachDecision(context));

        Register(best.Tactic, context.Now);
        return best;
    }

    private static void Consider(
        ref MonsterDecision best,
        ref float bestScore,
        MonsterDecisionContext context,
        MonsterDecision candidate)
    {
        if (candidate.Score <= bestScore)
            return;

        best = candidate;
        bestScore = candidate.Score;
    }

    private MonsterDecision BuildBaselineDecision(MonsterDecisionContext context)
    {
        if (context.HasVisualContact && context.HasReachableTargetPath)
        {
            return new MonsterDecision(
                MonsterIntent.Hunt,
                MonsterTactic.DirectPursuit,
                MonsterDecisionReason.DirectVisualPressure,
                context.TargetPosition,
                0.45f + Mathf.Clamp01(context.Suspicion) * 0.1f);
        }

        if (context.HasFreshStimulus || context.Suspicion >= 0.2f)
        {
            return new MonsterDecision(
                MonsterIntent.Search,
                MonsterTactic.None,
                MonsterDecisionReason.None,
                GetEvidenceFocus(context),
                0.2f);
        }

        return new MonsterDecision(
            MonsterIntent.Patrol,
            MonsterTactic.None,
            MonsterDecisionReason.None,
            context.SelfPosition,
            0.05f);
    }

    private MonsterDecision BuildListenDecision(MonsterDecisionContext context)
    {
        float reliability = context.PrimaryStimulus.Reliability;
        if (context.HasVisualContact || !context.HasFreshStimulus)
            return default;

        if (reliability > 0.65f || context.TimeSinceStimulus > 2.5f)
            return default;

        return CreateDecision(
            context,
            MonsterIntent.Investigate,
            MonsterTactic.Listen,
            MonsterDecisionReason.AmbiguousStimulus,
            context.PrimaryStimulus.Position,
            0.72f + (0.65f - reliability) * 0.2f);
    }

    private MonsterDecision BuildInterceptDecision(MonsterDecisionContext context)
    {
        if (!context.HasInterceptRoute)
            return default;

        Vector3 velocity = context.HasVisualContact ? context.TargetVelocity : context.PrimaryStimulus.ObservedVelocity;
        if (velocity.sqrMagnitude < 0.04f)
            return default;

        float reliability = Mathf.Max(context.PrimaryStimulus.Reliability, context.HasVisualContact ? 1f : 0f);
        Vector3 interceptPoint = GetEvidenceFocus(context) + velocity.normalized * settings.InterceptLeadSeconds * Mathf.Max(1f, velocity.magnitude);
        float score = 0.66f + reliability * 0.18f + Mathf.Clamp01(context.DistanceToLastKnownPosition / 12f) * 0.08f;

        return CreateDecision(
            context,
            context.HasVisualContact ? MonsterIntent.Hunt : MonsterIntent.Search,
            MonsterTactic.Intercept,
            MonsterDecisionReason.FreshEscapeVector,
            interceptPoint,
            score);
    }

    private MonsterDecision BuildFlankDecision(MonsterDecisionContext context)
    {
        if (!context.HasFlankRoute)
            return default;

        Vector3 focalPoint = context.HasVisualContact ? context.TargetPosition : GetEvidenceFocus(context);
        Vector3 referenceDirection = context.TargetVelocity.sqrMagnitude > 0.04f
            ? context.TargetVelocity.normalized
            : (focalPoint - context.SelfPosition).normalized;

        if (referenceDirection.sqrMagnitude < 0.01f)
            referenceDirection = Vector3.forward;

        Vector3 lateral = Vector3.Cross(Vector3.up, referenceDirection).normalized;
        Vector3 flankPoint = focalPoint + lateral * settings.FlankOffsetDistance;
        float score = 0.62f + Mathf.Clamp01(context.DistanceToTarget / 12f) * 0.08f;

        if (context.HasVisualContact && !context.TargetAwareOfMonster)
            score += 0.08f;

        return CreateDecision(
            context,
            context.HasVisualContact ? MonsterIntent.Hunt : MonsterIntent.Search,
            MonsterTactic.Flank,
            MonsterDecisionReason.ExposedApproach,
            flankPoint,
            score);
    }

    private MonsterDecision BuildHoldDecision(MonsterDecisionContext context)
    {
        if (!context.HasHoldPoint || context.HasVisualContact)
            return default;

        float reliability = context.PrimaryStimulus.Reliability;
        if (reliability < 0.45f && context.Suspicion < 0.45f)
            return default;

        Vector3 anchor = GetEvidenceFocus(context);
        Vector3 toSelf = context.SelfPosition - anchor;
        Vector3 offset = toSelf.sqrMagnitude > 0.01f
            ? toSelf.normalized * Mathf.Min(settings.HoldOffsetDistance, toSelf.magnitude)
            : Vector3.back * settings.HoldOffsetDistance;

        return CreateDecision(
            context,
            MonsterIntent.Ambush,
            MonsterTactic.Hold,
            MonsterDecisionReason.ChokePointControl,
            anchor + offset,
            0.64f + reliability * 0.12f + Mathf.Clamp01(context.Suspicion) * 0.08f);
    }

    private MonsterDecision BuildDoubleBackDecision(MonsterDecisionContext context)
    {
        if (!context.HasRecentSearchFailure && context.TimeSinceStimulus < 4f)
            return default;

        Vector3 evidence = GetEvidenceFocus(context);
        Vector3 away = evidence - context.SelfPosition;
        Vector3 direction = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
        Vector3 point = evidence - direction * settings.DoubleBackDistance;

        return CreateDecision(
            context,
            MonsterIntent.Search,
            MonsterTactic.DoubleBack,
            MonsterDecisionReason.BrokenSearchLoop,
            point,
            0.63f + Mathf.Clamp01(context.TimeSinceStimulus / 10f) * 0.08f);
    }

    private MonsterDecision BuildSilentApproachDecision(MonsterDecisionContext context)
    {
        if (!context.HasVisualContact || context.TargetAwareOfMonster || !context.HasCoverApproach)
            return default;

        Vector3 retreatAxis = context.TargetVelocity.sqrMagnitude > 0.04f
            ? context.TargetVelocity.normalized
            : (context.TargetPosition - context.SelfPosition).normalized;

        if (retreatAxis.sqrMagnitude < 0.01f)
            retreatAxis = Vector3.forward;

        Vector3 point = context.TargetPosition - retreatAxis * settings.SilentApproachOffset;
        float score = 0.8f + Mathf.Clamp01(context.Suspicion) * 0.1f;

        if (context.DistanceToTarget > 5f)
            score += 0.04f;

        return CreateDecision(
            context,
            MonsterIntent.Ambush,
            MonsterTactic.SilentApproach,
            MonsterDecisionReason.UnawareTarget,
            point,
            score);
    }

    private MonsterDecision CreateDecision(
        MonsterDecisionContext context,
        MonsterIntent intent,
        MonsterTactic tactic,
        MonsterDecisionReason reason,
        Vector3 focusPoint,
        float score)
    {
        if (!IsAllowed(tactic, context.Now))
            return default;

        return new MonsterDecision(intent, tactic, reason, focusPoint, score);
    }

    private bool IsAllowed(MonsterTactic tactic, float now)
    {
        if (tactic == MonsterTactic.None)
            return true;

        int index = (int)tactic;
        if (now < lastUsedTimes[index] + GetCooldown(tactic))
            return false;

        if (lastTactic == tactic &&
            consecutiveUses >= Mathf.Max(1, settings.MaxConsecutiveUses) &&
            now - lastTacticTime <= Mathf.Max(0f, settings.RepeatWindow))
            return false;

        return true;
    }

    private void Register(MonsterTactic tactic, float now)
    {
        if (tactic == MonsterTactic.None)
            return;

        lastUsedTimes[(int)tactic] = now;

        if (lastTactic == tactic && now - lastTacticTime <= Mathf.Max(0f, settings.RepeatWindow))
            consecutiveUses++;
        else
            consecutiveUses = 1;

        lastTactic = tactic;
        lastTacticTime = now;
    }

    private float GetCooldown(MonsterTactic tactic)
    {
        switch (tactic)
        {
            case MonsterTactic.Listen:
                return settings.ListenCooldown;
            case MonsterTactic.Intercept:
                return settings.InterceptCooldown;
            case MonsterTactic.Flank:
                return settings.FlankCooldown;
            case MonsterTactic.Hold:
                return settings.HoldCooldown;
            case MonsterTactic.DoubleBack:
                return settings.DoubleBackCooldown;
            case MonsterTactic.SilentApproach:
                return settings.SilentApproachCooldown;
            case MonsterTactic.DirectPursuit:
                return settings.DirectPursuitCooldown;
            default:
                return 0f;
        }
    }

    private static Vector3 GetEvidenceFocus(MonsterDecisionContext context)
    {
        if (context.PrimaryStimulus.Type != MonsterStimulusType.None)
            return context.PrimaryStimulus.Position;

        if (context.LastKnownPosition != Vector3.zero)
            return context.LastKnownPosition;

        return context.TargetPosition;
    }
}
