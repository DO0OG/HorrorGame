using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Monster_Health))]
public class Monster_Controller : MonoBehaviour
{
    [SerializeField] internal Define.MonsterType type = Define.MonsterType.Normal;

    [Header("Target")]
    [SerializeField] private GameObject player;

    [Header("Trace")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private FieldOfView fov;
    [SerializeField, Range(0, 100)] private float soundDetectionRange = 20.0f;
    [SerializeField, Range(0, 100)] private float soundDetctionMinRange = 5.0f;
    [SerializeField, Range(0, 1)] private float detectionMinSound = 0.6f;
    [SerializeField] private float chasingSpeed = 3.5f;
    [SerializeField] private float normalSpeed = 1.5f;
    [SerializeField] private float attackRange = 5f;
    private Vector3 lastSoundPosition;

    [Header("Wander")]
    [SerializeField] private float WANDER_INTERVAL = 5.0f;
    [SerializeField] private float WANDER_RADIUS = 10.0f;

    [Header("Status")]
    [SerializeField] private bool isChasing = false;
    [SerializeField] private bool isWandering = false;

    const float DELAY = 0.2f;
    internal float damage;
    Rigidbody rb;
    AudioSource audioSource;
    Monster_Health mh;

    // Start is called before the first frame update
    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<FieldOfView>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        mh = GetComponent<Monster_Health>();

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        TypeCheck(type);
        StartCoroutine(DetectRoutine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (isChasing)
            agent.speed = chasingSpeed;
        else
            agent.speed = normalSpeed;
    }

    private void TypeCheck(Define.MonsterType type)
    {
        switch (type)
        {
            case Define.MonsterType.Normal:
                mh.SetHealth(80);
                mh.ableToKill = true;
                soundDetectionRange = 20;
                soundDetctionMinRange = 5;
                damage = 25;
                chasingSpeed = 3.5f;
                normalSpeed = 1.5f;
                break;
            case Define.MonsterType.Scream:
                mh.SetHealth(20);
                mh.ableToKill = true;
                damage = 0;
                chasingSpeed = 2.5f;
                normalSpeed = 2.5f;
                break;
            case Define.MonsterType.Special:
                mh.ableToKill = false;
                soundDetectionRange = 40;
                soundDetctionMinRange = 10;
                damage = 50;
                chasingSpeed = 4f;
                normalSpeed = 2f;
                break;
        }
    }

    private void Attack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (fov.playerDetected && distanceToPlayer <= attackRange)
        {
            Debug.Log("Attack");
        }
    }

    private void Scream()
    {
        AudioClip clip = Managers.Resource.LoadAudioClip($"Monster/Scream");
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator DetectRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(DELAY);

        while (true)
        {
            yield return wait;

            if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
                isChasing = false;

            if (!isChasing && !isWandering)
                StartCoroutine(Wander());

            Attack();
            DetectSight();
            DetectSounds();
        }
    }

    private IEnumerator Wander()
    {
        if (isWandering) yield break;

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
            StopCoroutine(Wander());

            isWandering = false;
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
            StopCoroutine(Wander());

            isWandering = false;
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
