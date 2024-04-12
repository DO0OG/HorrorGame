using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent (typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class NormalCreature_Controller : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject player;

    [Header("Trace")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private FieldOfView fov;
    [SerializeField, Range(0, 100)] private float soundDetectionRange = 20.0f;
    [SerializeField, Range(0, 100)] private float soundDetctionMinRange = 5.0f;
    [SerializeField, Range(0, 1)] private float detectionMinSound = 0.6f;
    private Vector3 lastSoundPosition;

    [Header("Wander")]
    [SerializeField] private float WANDER_INTERVAL = 5.0f;
    [SerializeField] private float WANDER_RADIUS = 10.0f;

    [Header("Status")]
    [SerializeField] private bool isChasing = false;
    [SerializeField] private bool isWandering = false;

    const float DELAY = 0.2f;

    // Start is called before the first frame update
    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<FieldOfView>();

        StartCoroutine(DetectRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator DetectRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(DELAY);

        while (true)
        {
            yield return wait;

            if (!isChasing && !isWandering) // 추적 중이 아닐 때만 배회
            {
                StartCoroutine(Wander());
            }

            DetectSight();
            DetectSounds();
        }
    }

    private IEnumerator Wander()
    {
        isWandering = true;

        // 랜덤한 위치 설정
        Vector3 randomDirection = Random.insideUnitSphere * WANDER_RADIUS;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, WANDER_RADIUS, 1);
        Vector3 finalPosition = hit.position;

        // 목표 위치 설정
        agent.SetDestination(finalPosition);

        // 일정 시간 대기
        yield return new WaitForSeconds(WANDER_INTERVAL);

        isWandering = false;
    }

    private void DetectSight()
    {
        if (fov.playerDetected)
        {
            isChasing = true;
            lastSoundPosition = player.transform.position;
            agent.destination = lastSoundPosition;
            
            Debug.Log("Sight Detected");
        }
        else
            return;
    }

    private void DetectSounds()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (fov.playerDetected) return;

        foreach (var player in from player in players
                               let audioSources = player.GetComponents<AudioSource>()
                               from audioSource in audioSources
                               where audioSource.isPlaying && (Vector3.Distance(transform.position, player.transform.position) < soundDetctionMinRange ? audioSource.volume > 0 : audioSource.volume > detectionMinSound)
                               let distanceToSound = Vector3.Distance(transform.position, player.transform.position)
                               where distanceToSound < soundDetectionRange
                               select player)
        {
            isChasing = true;
            // 마지막으로 들린 사운드의 위치 업데이트
            lastSoundPosition = player.transform.position;
            // 에이전트의 목표 위치를 마지막으로 들린 사운드의 위치로 설정
            agent.destination = lastSoundPosition;

            Debug.Log("Sound Detected");
            return;
        }
    }
}
