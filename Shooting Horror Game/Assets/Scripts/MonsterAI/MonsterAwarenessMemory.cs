using UnityEngine;

/// <summary>
/// Normal 몬스터가 즉시 모든 것을 아는 대신, 감각 정보와 마지막 목격을 기억하게 하는
/// 순수 런타임 모델. MonoBehaviour와 분리되어 있어 상태 전이를 EditMode에서 검증할 수 있다.
/// </summary>
public sealed class MonsterAwarenessMemory
{
    public enum AwarenessState
    {
        Calm,
        Suspicious,
        Alerted,
    }

    private const float SuspiciousThreshold = 0.15f;
    private const float AlertedThreshold = 0.999f;

    public float Suspicion { get; private set; }
    public float TimeSinceVisualContact { get; private set; } = float.PositiveInfinity;
    public float TimeSinceStimulus { get; private set; } = float.PositiveInfinity;
    public Vector3 LastKnownPosition { get; private set; }
    public Vector3 LastKnownVelocity { get; private set; }
    public float PositionUncertainty { get; private set; }
    public float LastStimulusConfidence { get; private set; }
    public bool IsSharedMemory { get; private set; }
    public bool HasMemory { get; private set; }

    public AwarenessState State
    {
        get
        {
            if (Suspicion >= AlertedThreshold) return AwarenessState.Alerted;
            if (Suspicion >= SuspiciousThreshold) return AwarenessState.Suspicious;
            return AwarenessState.Calm;
        }
    }

    public void Observe(Vector3 position, Vector3 velocity, float deltaTime, float confirmationTime)
    {
        HasMemory = true;
        LastKnownPosition = position;
        LastKnownVelocity = velocity;
        PositionUncertainty = 0f;
        LastStimulusConfidence = 1f;
        IsSharedMemory = false;
        TimeSinceVisualContact = 0f;
        TimeSinceStimulus = 0f;

        float duration = Mathf.Max(0.01f, confirmationTime);
        Suspicion = Mathf.MoveTowards(Suspicion, 1f, Mathf.Max(0f, deltaTime) / duration);
    }

    public void RegisterStimulus(Vector3 position, float confidence)
    {
        RegisterStimulus(position, confidence, 0f, false);
    }

    public void RegisterStimulus(
        Vector3 position,
        float confidence,
        float positionUncertainty,
        bool isShared)
    {
        float clampedConfidence = Mathf.Clamp01(confidence);

        HasMemory = true;
        LastKnownPosition = position;
        LastKnownVelocity = Vector3.zero;
        PositionUncertainty = Mathf.Max(0f, positionUncertainty);
        LastStimulusConfidence = clampedConfidence;
        IsSharedMemory = isShared;
        TimeSinceStimulus = 0f;

        // 소리나 빛만으로는 플레이어를 확정하지 않는다. 강한 단서도 '의심'까지만 올려
        // 몬스터가 현장을 조사하게 하고, 직접 시야 확인을 거쳐야 전력 추격한다.
        Suspicion = Mathf.Max(Suspicion, Mathf.Lerp(0.2f, 0.8f, clampedConfidence));
    }

    public void Tick(float deltaTime)
    {
        float elapsed = Mathf.Max(0f, deltaTime);
        TimeSinceVisualContact += elapsed;
        TimeSinceStimulus += elapsed;
    }

    /// <summary>
    /// 단서가 오래될수록 확신은 줄고 위치 오차는 커진다. 유예 시간을 넘긴
    /// 구간에 대해서만 감쇠를 적용해 큰 프레임 간격에서도 결과가 일관된다.
    /// </summary>
    public void AgeEvidence(
        float deltaTime,
        float decayDelay,
        float suspicionDecayPerSecond,
        float uncertaintyGrowthPerSecond)
    {
        float elapsed = Mathf.Max(0f, deltaTime);
        float ageBeforeTick = TimeSinceStimulus;
        Tick(elapsed);

        if (!HasMemory || TimeSinceStimulus <= decayDelay)
            return;

        float decayStart = Mathf.Max(ageBeforeTick, Mathf.Max(0f, decayDelay));
        float agingDuration = Mathf.Max(0f, TimeSinceStimulus - decayStart);
        if (agingDuration <= 0f)
            return;

        PositionUncertainty += Mathf.Max(0f, uncertaintyGrowthPerSecond) * agingDuration;
        Decay(Mathf.Max(0f, suspicionDecayPerSecond) * agingDuration);
    }

    public void Decay(float amount)
    {
        Suspicion = Mathf.MoveTowards(Suspicion, 0f, Mathf.Max(0f, amount));
        if (Suspicion <= 0f)
            Forget();
    }

    public Vector3 PredictPosition(float maximumLeadTime)
    {
        if (!HasMemory) return LastKnownPosition;

        float lead = Mathf.Min(TimeSinceVisualContact, Mathf.Max(0f, maximumLeadTime));
        return LastKnownPosition + LastKnownVelocity * lead;
    }

    public void Forget()
    {
        Suspicion = 0f;
        HasMemory = false;
        LastKnownVelocity = Vector3.zero;
        PositionUncertainty = 0f;
        LastStimulusConfidence = 0f;
        IsSharedMemory = false;
        TimeSinceVisualContact = float.PositiveInfinity;
        TimeSinceStimulus = float.PositiveInfinity;
    }
}
