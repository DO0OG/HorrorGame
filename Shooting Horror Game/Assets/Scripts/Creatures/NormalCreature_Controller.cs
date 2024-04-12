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

            if (!isChasing && !isWandering) // ���� ���� �ƴ� ���� ��ȸ
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

        // ������ ��ġ ����
        Vector3 randomDirection = Random.insideUnitSphere * WANDER_RADIUS;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, WANDER_RADIUS, 1);
        Vector3 finalPosition = hit.position;

        // ��ǥ ��ġ ����
        agent.SetDestination(finalPosition);

        // ���� �ð� ���
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
            // ���������� �鸰 ������ ��ġ ������Ʈ
            lastSoundPosition = player.transform.position;
            // ������Ʈ�� ��ǥ ��ġ�� ���������� �鸰 ������ ��ġ�� ����
            agent.destination = lastSoundPosition;

            Debug.Log("Sound Detected");
            return;
        }
    }
}
