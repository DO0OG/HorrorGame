using System;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterInterestPointKind
{
    Doorway,
    Intersection,
    HidingSpot,
    DeadEnd,
    CoverEdge,
    EscapeRoute,
    NoiseTrap,
}

public readonly struct MonsterInterestPoint
{
    public MonsterInterestPointKind Kind { get; }
    public Vector3 Position { get; }
    public float Interest { get; }
    public float LastVisitedAt { get; }
    public bool IsReachable { get; }
    public bool IsOccluded { get; }

    public MonsterInterestPoint(
        MonsterInterestPointKind kind,
        Vector3 position,
        float interest,
        float lastVisitedAt,
        bool isReachable,
        bool isOccluded)
    {
        Kind = kind;
        Position = position;
        Interest = Mathf.Clamp01(interest);
        LastVisitedAt = lastVisitedAt;
        IsReachable = isReachable;
        IsOccluded = isOccluded;
    }
}

public struct MonsterSearchContext
{
    public float Now;
    public Vector3 Center;
    public Vector3 LastKnownVelocity;
    public float SearchRadius;
    public float RecentVisitWindow;
    public int FallbackStartIndex;
}

public readonly struct MonsterSearchPoint
{
    public Vector3 Position { get; }
    public float Score { get; }
    public MonsterInterestPointKind Kind { get; }
    public bool IsFallback { get; }

    public MonsterSearchPoint(Vector3 position, float score, MonsterInterestPointKind kind, bool isFallback)
    {
        Position = position;
        Score = score;
        Kind = kind;
        IsFallback = isFallback;
    }
}

/// <summary>
/// Pure deterministic search planner. Ranks authored interest points first,
/// then fills remaining slots with golden-angle sweep positions.
/// </summary>
public static class MonsterSearchPlanner
{
    private const float GoldenAngleDegrees = 137.50776405f;
    private const float MinFallbackRadius = 1.5f;
    private const float FallbackRadiusStep = 1.35f;

    public static int BuildPlan(
        MonsterSearchContext context,
        IList<MonsterInterestPoint> candidates,
        MonsterSearchPoint[] results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        if (results.Length == 0)
            return 0;

        int written = RankCandidates(context, candidates, results);

        for (int i = written; i < results.Length; i++)
        {
            int fallbackIndex = context.FallbackStartIndex + (i - written);
            Vector3 point = GetFallbackPoint(context, fallbackIndex);
            results[i] = new MonsterSearchPoint(point, 0.2f - fallbackIndex * 0.01f, MonsterInterestPointKind.Intersection, true);
        }

        return results.Length;
    }

    public static int RankCandidates(
        MonsterSearchContext context,
        IList<MonsterInterestPoint> candidates,
        MonsterSearchPoint[] results)
    {
        if (results == null)
            throw new ArgumentNullException(nameof(results));

        if (candidates == null || candidates.Count == 0 || results.Length == 0)
            return 0;

        int count = 0;
        float[] scores = new float[results.Length];

        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterInterestPoint candidate = candidates[i];
            if (!candidate.IsReachable)
                continue;

            float score = ScoreCandidate(context, candidate);
            if (count < results.Length)
            {
                results[count] = new MonsterSearchPoint(candidate.Position, score, candidate.Kind, false);
                scores[count] = score;
                count++;
                continue;
            }

            int worstIndex = FindWorstIndex(scores, count);
            if (score <= scores[worstIndex])
                continue;

            results[worstIndex] = new MonsterSearchPoint(candidate.Position, score, candidate.Kind, false);
            scores[worstIndex] = score;
        }

        SortDescending(results, scores, count);
        return count;
    }

    public static float ScoreCandidate(MonsterSearchContext context, MonsterInterestPoint candidate)
    {
        float baseWeight = GetKindWeight(candidate.Kind);
        Vector3 direction = candidate.Position - context.Center;
        float distance = direction.magnitude;
        float radius = Mathf.Max(MinFallbackRadius, context.SearchRadius);
        float distanceScore = 1f - Mathf.Clamp01(distance / radius);

        Vector3 velocity = context.LastKnownVelocity;
        float alignment = 0.5f;
        if (velocity.sqrMagnitude > 0.04f && direction.sqrMagnitude > 0.04f)
            alignment = 0.5f + 0.5f * Mathf.Clamp(Vector3.Dot(velocity.normalized, direction.normalized), -1f, 1f);

        float visitedPenalty = 0f;
        if (context.Now - candidate.LastVisitedAt < Mathf.Max(0f, context.RecentVisitWindow))
            visitedPenalty = 0.35f;

        float occlusionBonus = candidate.IsOccluded ? 0.08f : 0f;
        return baseWeight + candidate.Interest * 0.35f + alignment * 0.25f + distanceScore * 0.15f + occlusionBonus - visitedPenalty;
    }

    public static Vector3 GetFallbackPoint(MonsterSearchContext context, int stepIndex)
    {
        int nonNegativeIndex = Mathf.Max(0, stepIndex);
        float radius = Mathf.Min(
            Mathf.Max(MinFallbackRadius, context.SearchRadius),
            MinFallbackRadius + nonNegativeIndex * FallbackRadiusStep);

        Vector3 bias = context.LastKnownVelocity;
        float baseAngle = bias.sqrMagnitude > 0.04f
            ? Mathf.Atan2(bias.z, bias.x) * Mathf.Rad2Deg
            : 0f;

        float angle = baseAngle + nonNegativeIndex * GoldenAngleDegrees;
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
        return context.Center + offset;
    }

    private static float GetKindWeight(MonsterInterestPointKind kind)
    {
        switch (kind)
        {
            case MonsterInterestPointKind.EscapeRoute:
                return 0.95f;
            case MonsterInterestPointKind.Doorway:
                return 0.9f;
            case MonsterInterestPointKind.Intersection:
                return 0.82f;
            case MonsterInterestPointKind.HidingSpot:
                return 0.78f;
            case MonsterInterestPointKind.DeadEnd:
                return 0.72f;
            case MonsterInterestPointKind.CoverEdge:
                return 0.68f;
            case MonsterInterestPointKind.NoiseTrap:
                return 0.65f;
            default:
                return 0.5f;
        }
    }

    private static int FindWorstIndex(float[] scores, int count)
    {
        int worstIndex = 0;
        float worstScore = scores[0];
        for (int i = 1; i < count; i++)
        {
            if (scores[i] >= worstScore)
                continue;

            worstScore = scores[i];
            worstIndex = i;
        }

        return worstIndex;
    }

    private static void SortDescending(MonsterSearchPoint[] results, float[] scores, int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                if (scores[j] <= scores[i])
                    continue;

                MonsterSearchPoint point = results[i];
                results[i] = results[j];
                results[j] = point;

                float score = scores[i];
                scores[i] = scores[j];
                scores[j] = score;
            }
        }
    }
}
