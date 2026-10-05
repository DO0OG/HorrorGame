using System;
using UnityEngine;

public enum MonsterNoiseKind
{
    Footstep,
    Sprint,
    Crouch,
    Gunshot,
    Equipment,
    Door,
    Impact,
    HollowScream,
}

public readonly struct MonsterNoiseEvent
{
    public MonsterNoiseEvent(
        MonsterNoiseKind kind,
        Vector3 position,
        float loudness,
        float radius,
        float timestamp)
    {
        Kind = kind;
        Position = position;
        Loudness = Mathf.Clamp01(loudness);
        Radius = Mathf.Max(0f, radius);
        Timestamp = timestamp;
    }

    public MonsterNoiseKind Kind { get; }
    public Vector3 Position { get; }
    public float Loudness { get; }
    public float Radius { get; }
    public float Timestamp { get; }
}

/// <summary>
/// 재생 중인 AudioSource를 추측하는 대신, 게임 행동이 의미와 함께 소음을 발행한다.
/// 구독자는 거리와 장애물에 따라 각자 다르게 해석한다.
/// </summary>
public static class MonsterNoiseSystem
{
    public static event Action<MonsterNoiseEvent> NoiseEmitted;

    public static void Emit(
        MonsterNoiseKind kind,
        Vector3 position,
        float loudness,
        float radius)
    {
        Publish(new MonsterNoiseEvent(kind, position, loudness, radius, Time.time));
    }

    public static void Publish(MonsterNoiseEvent noise)
    {
        NoiseEmitted?.Invoke(noise);
    }
}
