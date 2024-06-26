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

    [Header("Hollow")]
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private float hollowRange = 10f;

    [SerializeField] internal float damage;
    const float DELAY = 0.2f;
    Rigidbody rb;
    AudioSource audioSource;
    Monster_Health mh;
    Player_Health ph;

    // Start is called before the first frame update
    void OnEnable()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<FieldOfView>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        mh = GetComponent<Monster_Health>();
        ph = player.GetComponent<Player_Health>();
        meshRenderer = GetComponent<SkinnedMeshRenderer>();

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
        SpeedControl();

        if (type == Define.MonsterType.Hollow)
            Hollow();
    }

    private void SpeedControl()
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
            case Define.MonsterType.Hollow:
                mh.SetHealth(20);
                mh.ableToKill = true;
                soundDetectionRange = 20;
                soundDetctionMinRange = 5;
                damage = 5;
                chasingSpeed = 1.5f;
                normalSpeed = 1.5f;
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
            switch (type)
            {
                case Define.MonsterType.Normal:
                    Debug.Log("Attack");
                    break;
                case Define.MonsterType.Hollow:
                    Debug.Log("Scream");
                    Instantiate(Managers.Resource.LoadPrefab("Monster/ScreamAudio"), transform.position, transform.rotation);

                    ph.DecreaseHealth(damage);
                    mh.SetHealth(0);
                    //SoundManager.Instance.Play(SoundType.SFX, Path.SOUND + "Monster/Scream");
                    break;
                case Define.MonsterType.Special:
                    Debug.Log("Attack");
                    break;
            }
        }
    }

    private void Hollow()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        float currentDistortion = meshRenderer.material.GetFloat("_Distortion");

        Mathf.Clamp(currentDistortion, 0f, 0.05f);

        if (distanceToPlayer <= hollowRange)
        {
            meshRenderer.material.SetFloat("_Distortion", Mathf.Lerp(currentDistortion, 0.05f, Time.deltaTime * 2f));
            // Debug.Log(currentDistortion);
        }
        else
        {
            meshRenderer.material.SetFloat("_Distortion", Mathf.Lerp(currentDistortion, 0f, Time.deltaTime * 2f));
            // Debug.Log(currentDistortion);
        }
    }

    private IEnumerator DetectRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(DELAY);

        while (true)
        {
            yield return wait;

            float distanceToDestination = Vector3.Distance(transform.position, agent.destination);

            if (distanceToDestination <= 5 && !fov.playerDetected)
                isChasing = false;

            if (!isChasing && !isWandering && type != Define.MonsterType.Hollow)
                StartCoroutine(Wander());

            DetectSight();
            DetectSounds();
            Attack();
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

    private void OnCollisionEnter(Collision collision)
    {
    }
}
