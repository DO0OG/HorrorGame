using System;
using UnityEngine;

public readonly struct MonsterSharedAlert
{
    public MonsterSharedAlert(
        int senderId,
        Vector3 position,
        float confidence,
        float uncertainty,
        float radius,
        float timestamp)
    {
        SenderId = senderId;
        Position = position;
        Confidence = Mathf.Clamp01(confidence);
        Uncertainty = Mathf.Max(0f, uncertainty);
        Radius = Mathf.Max(0f, radius);
        Timestamp = timestamp;
    }

    public int SenderId { get; }
    public Vector3 Position { get; }
    public float Confidence { get; }
    public float Uncertainty { get; }
    public float Radius { get; }
    public float Timestamp { get; }
}

/// <summary>
/// Normal 몬스터 사이의 제한적 지역 경보 버스. 수신자가 거리, 지연, 장애물,
/// 위치 오차를 적용하므로 전역 텔레파시로 사용되지 않는다.
/// </summary>
public static class MonsterCoordinationHub
{
    public static event Action<MonsterSharedAlert> AlertBroadcast;

    public static void Broadcast(
        int senderId,
        Vector3 position,
        float confidence,
        float uncertainty,
        float radius)
    {
        AlertBroadcast?.Invoke(new MonsterSharedAlert(
            senderId,
            position,
            confidence,
            uncertainty,
            radius,
            Time.time));
    }

    public static void Publish(MonsterSharedAlert alert)
    {
        AlertBroadcast?.Invoke(alert);
    }
}
