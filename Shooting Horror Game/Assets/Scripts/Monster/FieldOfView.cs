using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float radius;
    [Range(0, 360), SerializeField] private float angle;
    [SerializeField] private float eyeHeight = 1.6f;          // 시야 판정 기준 눈 높이 (피벗은 발밑이므로 보정)
    [SerializeField] private float coverCheckHeight = 3f;     // 앉았을 때 머리 위 엄폐물을 확인하는 거리

    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;

    private Player_Controller playerController;

    /// <summary>빛 감지 등 외부에서 시야 차단 판정에 같은 마스크를 쓰기 위한 접근자</summary>
    public LayerMask ObstructionMask => obstructionMask;

    [SerializeField] public bool playerDetected;

    const float DELAY = 0.2f;

    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        targetMask = LayerMask.GetMask("Player");
        playerController = player.GetComponent<Player_Controller>();

        StartCoroutine(FOVRoutine());
    }

    void OnDisable()
    {
       StopAllCoroutines();
    }

    /// <summary>
    /// 난이도에 따라 시야 반경/각도를 설정
    /// </summary>
    public void SetSight(float radius, float angle)
    {
        this.radius = radius;
        this.angle = angle;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // 시야 시각화는 디버그용 (프레임당 레이캐스트 50회) — 빌드에서는 제외
        DrawFOV();
#endif
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(DELAY);

        while (true)
        {
            yield return wait;

            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);
        if (rangeChecks.Length != 0)
        {
            Collider target = rangeChecks[0];
            Vector3 center = target.bounds.center;
            Vector3 directionToTarget = (center - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                // 앉은 채 머리 위에 엄폐물이 있으면(책상/구조물 아래) 시야로는 발견하지 못함.
                // 옆이 뚫린 엄폐물이라 레이가 밑으로 통과하더라도 게임플레이상 숨은 것으로 취급
                if (playerController != null && playerController.isCrouch &&
                    Physics.Raycast(center, Vector3.up, coverCheckHeight, obstructionMask))
                {
                    playerDetected = false;
                }
                else
                {
                    // 눈높이에서 몸 중심과 머리 두 지점을 확인 (둘 다 가려져야 미발견)
                    Vector3 eye = transform.position + Vector3.up * eyeHeight;
                    Vector3 head = center + Vector3.up * (target.bounds.extents.y * 0.7f);

                    playerDetected = CanSeePoint(eye, center) || CanSeePoint(eye, head);
                }
            }
            else
                playerDetected = false;
        }
        else if (playerDetected)
            playerDetected = false;
    }

    /// <summary>
    /// 눈 위치에서 지점까지 장애물이 없는지 판정
    /// </summary>
    private bool CanSeePoint(Vector3 from, Vector3 point)
    {
        Vector3 delta = point - from;
        float distance = delta.magnitude;
        if (distance < 0.001f) return true;

        return !Physics.Raycast(from, delta / distance, distance, obstructionMask);
    }

#if UNITY_EDITOR
    private void DrawFOV()
    {
        int rayCount = 50;
        float angleStep = angle / rayCount;
        float currentAngle = -angle / 2;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 rayDirection = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            RaycastHit hit;

            if (Physics.Raycast(transform.position, rayDirection, out hit, radius, obstructionMask))
            {
                Debug.DrawLine(transform.position, hit.point, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, rayDirection * radius, Color.green);
            }

            currentAngle += angleStep;
        }
    }
#endif
}
