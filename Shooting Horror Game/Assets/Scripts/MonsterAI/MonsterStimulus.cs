using UnityEngine;

public enum MonsterStimulusType
{
    None,
    Visual,
    Sound,
    Light,
    SharedAlert,
    Environmental,
}

/// <summary>
/// Pure evidence packet consumed by monster cognition/decision code.
/// Carries enough information to reason about stale, uncertain, or shared observations
/// without relying on MonoBehaviour state.
/// </summary>
public readonly struct MonsterStimulus
{
    public MonsterStimulusType Type { get; }
    public Vector3 Position { get; }
    public float Confidence { get; }
    public float Uncertainty { get; }
    public float Timestamp { get; }
    public Vector3 ObservedVelocity { get; }
    public bool Shared { get; }

    public float Reliability => Confidence * (1f - Uncertainty);

    public MonsterStimulus(
        MonsterStimulusType type,
        Vector3 position,
        float confidence,
        float uncertainty,
        float timestamp,
        Vector3 observedVelocity,
        bool shared)
    {
        Type = type;
        Position = position;
        Confidence = Mathf.Clamp01(confidence);
        Uncertainty = Mathf.Clamp01(uncertainty);
        Timestamp = timestamp;
        ObservedVelocity = observedVelocity;
        Shared = shared;
    }

    public float Age(float now)
    {
        return Mathf.Max(0f, now - Timestamp);
    }

    public Vector3 PredictPosition(float now, float maxLeadTime)
    {
        float leadTime = Mathf.Min(Age(now), Mathf.Max(0f, maxLeadTime));
        float certaintyScale = 1f - Uncertainty;
        return Position + ObservedVelocity * (leadTime * certaintyScale);
    }
}
