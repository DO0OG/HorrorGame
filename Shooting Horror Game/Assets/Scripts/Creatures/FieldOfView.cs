using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private float radius;
    [Range(0, 360), SerializeField] private float angle;

    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstructionMask;

    [SerializeField] public bool playerDetected;

    const float DELAY = 0.2f;

    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        targetMask = LayerMask.GetMask("Player");

        StartCoroutine(FOVRoutine());
    }

    void OnDisable()
    {
       StopAllCoroutines();
    }

    private void Update()
    {
        DrawFOV();
        if (playerDetected)
        {
            Debug.Log("PLAYER");
        }
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
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                {
                    playerDetected = true;
                }
                else
                    playerDetected = false;
            }
            else
                playerDetected = false;
        }
        else if (playerDetected)
            playerDetected = false;
    }

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
}
