using UnityEngine;

/// <summary>
/// 플레이어 이동을 AI가 해석할 수 있는 의미 기반 소음 이벤트로 변환한다.
/// 실제 발소리 AudioSource는 연출용으로 유지하며 AI는 이 이벤트를 우선 사용한다.
/// </summary>
public sealed class PlayerNoiseEmitter : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float walkInterval = 0.55f;
    [SerializeField, Min(0.1f)] private float sprintInterval = 0.34f;
    [SerializeField, Min(0.1f)] private float crouchInterval = 0.8f;
    [SerializeField, Min(0f)] private float walkRadius = 12f;
    [SerializeField, Min(0f)] private float sprintRadius = 22f;
    [SerializeField, Min(0f)] private float crouchRadius = 5f;

    private float nextMovementNoiseTime;

    public void TickMovement(bool isMoving, bool isSprinting, bool isCrouching, bool isAiming)
    {
        if (!isMoving || Time.time < nextMovementNoiseTime)
            return;

        MonsterNoiseKind kind;
        float loudness;
        float radius;
        float interval;

        if (isSprinting)
        {
            kind = MonsterNoiseKind.Sprint;
            loudness = 1f;
            radius = sprintRadius;
            interval = sprintInterval;
        }
        else if (isCrouching || isAiming)
        {
            kind = MonsterNoiseKind.Crouch;
            loudness = 0.2f;
            radius = crouchRadius;
            interval = crouchInterval;
        }
        else
        {
            kind = MonsterNoiseKind.Footstep;
            loudness = 0.55f;
            radius = walkRadius;
            interval = walkInterval;
        }

        MonsterNoiseSystem.Emit(kind, transform.position, loudness, radius);
        nextMovementNoiseTime = Time.time + interval;
    }
}
