using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Monster_Health))]
[RequireComponent(typeof(FieldOfView))]
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
    [SerializeField, Range(0, 100)] private float lightDetectionRange = 20.0f;
    [SerializeField] private float lightSpotMaxDistance = 30f;   // 빛 자국(벽/바닥의 밝은 원)을 찾는 최대 거리 (손전등 Range와 동일)
    private Flashlight_Controller flashlight;
    private AudioSource[] playerAudioSources;   // 매 감지 주기마다 GetComponentsInChildren을 돌지 않도록 캐싱
    [SerializeField] private float chasingSpeed = 3.5f;
    [SerializeField] private float sightChasingSpeed = 4.5f;   // 시야로 직접 발견했을 때의 추적 속도 (소리/빛 감지는 chasingSpeed)
    [SerializeField] private float normalSpeed = 1.5f;
    [SerializeField] private float investigationSpeed = 2.2f;
    [SerializeField] private float attackRange = 2f;        // 공격 모션을 시작하는 거리
    [SerializeField] private float attackHitRange = 2.5f;   // 타격 프레임에 실제로 데미지가 들어가는 거리 (공격 중 조금 물러나는 것까지만 허용)
    [SerializeField, Range(0f, 360f)] private float attackHitAngle = 100f;  // 타격 판정 좌우 각도. 등 뒤의 플레이어는 맞지 않음
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField, Range(0f, 1f)] private float attackHitNormalizedTime = 0.54f;  // Attack 클립의 몇 % 지점에서 타격이 들어가는지 (원본 클립의 타격 이벤트 시점 0.534~0.550에 맞춤)
    [SerializeField] private float attackRecovery = 0.3f;   // 클립이 끝난 뒤 다음 공격까지의 후딜레이
    [SerializeField] private float attackTurnSpeed = 8f;    // 공격 준비 중 플레이어를 향해 도는 속도
    [SerializeField] private float attackAnimSpeed = 1.25f; // 공격 애니메이션 재생 배속. 판정은 클립 진행도 기반이라 자동으로 함께 빨라짐
    private static readonly int AttackStateHash = Animator.StringToHash("Attack");
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private Coroutine attackRoutine;

    [Header("Normal Monster Intelligence")]
    [SerializeField, Min(0.05f)] private float visualConfirmationTime = 0.65f;
    [SerializeField, Min(0f)] private float closeRangeInstantDetection = 3f;
    [SerializeField, Min(0f)] private float visualMemoryDuration = 3.5f;
    [SerializeField, Min(1f)] private float investigationTimeout = 9f;
    [SerializeField, Min(1f)] private float searchDuration = 16f;
    [SerializeField, Min(1f)] private float searchRadius = 8f;
    [SerializeField, Min(0.1f)] private float searchPointTolerance = 1.25f;
    [SerializeField] private Vector2 searchPauseRange = new Vector2(0.65f, 1.6f);
    [SerializeField, Min(0f)] private float predictionLeadTime = 1.25f;
    [SerializeField, Min(0f)] private float directPursuitRange = 6f;
    [SerializeField, Min(0f)] private float flankDistance = 4f;
    [SerializeField, Min(0.1f)] private float tacticalRepathInterval = 0.55f;
    [SerializeField, Min(0f)] private float hearingPositionError = 4f;
    [SerializeField, Range(0f, 1f)] private float hearingOcclusionMultiplier = 0.45f;
    [SerializeField, Min(0f)] private float suspicionDecayDelay = 3f;
    [SerializeField, Min(0f)] private float suspicionDecayPerSecond = 0.08f;
    [SerializeField, Min(0f)] private float uncertaintyGrowthPerSecond = 0.35f;
    [SerializeField, Min(0f)] private float coordinationRange = 18f;
    [SerializeField, Range(0f, 1f)] private float coordinationConfidenceLoss = 0.7f;
    [SerializeField] private Vector2 coordinationDelayRange = new Vector2(0.3f, 1.2f);
    [SerializeField, Min(0f)] private float coordinationSpread = 3f;
    [SerializeField, Min(0f)] private float coordinationBroadcastCooldown = 4f;
    [SerializeField, Min(0f)] private float patrolPause = 2f;
    [SerializeField, Min(0f)] private float searchTurnSpeed = 3f;

    private enum NormalAiState
    {
        Patrol,
        Investigating,
        Hunting,
        Searching,
    }

    [SerializeField] private NormalAiState normalAiState;
    private readonly MonsterAwarenessMemory awareness = new MonsterAwarenessMemory();
    private MonsterDecisionModel decisionModel;
    private MonsterStimulus primaryStimulus;
    private Rigidbody playerRigidbody;
    private float stateEnteredTime;
    private float nextTacticalDecisionTime;
    private float nextPatrolDecisionTime;
    private float searchDeadline;
    private float nextSearchMoveTime;
    private float nextHearingUpdateTime;
    private float nextLightUpdateTime;
    private float nextCoordinationBroadcastTime;
    private bool hasPendingNoise;
    private Vector3 pendingNoisePosition;
    private float pendingNoiseConfidence;
    private float pendingNoiseUncertainty;
    private int searchStep;
    private int flankDirection = 1;
    private bool isSearchPausing;
    private float deliberatePauseUntil;
    private bool silentApproachActive;
    private Vector3 searchCenter;
    private Vector3 searchLookTarget;
    private NavMeshPath pathBuffer;

    [Header("Wander")]
    [SerializeField] private float WANDER_INTERVAL = 5.0f;
    [SerializeField] private float WANDER_RADIUS = 10.0f;

    [Header("Status")]
    [SerializeField] private bool isChasing = false;
    [SerializeField] private bool isWandering = false;
    [SerializeField] private bool isStunned = false;
    [SerializeField] private bool isExecuting = false;

    [Header("AI Debug")]
    [SerializeField] private string currentIntent;
    [SerializeField] private string currentTactic;
    [SerializeField] private string currentDecisionReason;
    [SerializeField] private float currentSuspicion;
    [SerializeField] private float currentPositionUncertainty;

    [Header("Hollow")]
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private float hollowRange = 10f;           // 쉐이더가 보이기 시작하는 범위
    [SerializeField] private float hollowApproachRange = 10f;   // 플레이어에게 접근을 시작하는 범위
    [SerializeField] private float hollowTouchRange = 1.2f;     // 접촉 판정 범위
    [SerializeField, Min(0.1f)] private float hollowFocusedLightExposure = 2f;
    [SerializeField] private float hollowLightExposureProgress;
    [SerializeField] private string hollowLightExposureState;
    private float lastFocusedLightFrame = -999f;
    public string HollowLightExposureState => hollowLightExposureState;
    private Material hollowMaterial;                            // 매 프레임 .material 접근을 피하기 위한 캐시
    private static readonly int DistortionId = Shader.PropertyToID("_Distortion");

    [Header("ETC")]
    [SerializeField] private Animator animator;
    [SerializeField] internal float damage;
    const float DELAY = 0.2f;
    Rigidbody rb;
    AudioSource audioSource;
    Monster_Health mh;
    Player_Health ph;
    Player_Controller pc;
    private Coroutine stunRoutine;
    private bool hasMoveParameter;
    const float MOVE_ANIM_THRESHOLD = 0.1f;   // 이 속도 미만이면 멈춘 것으로 보고 Idle

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
        pc = player.GetComponent<Player_Controller>();
        playerRigidbody = player.GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        if (pathBuffer == null)
            pathBuffer = new NavMeshPath();

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 비활성화 도중 코루틴이 끊겼다가 재활성화되는 경우를 대비한 상태 초기화
        isAttacking = false;
        isStunned = false;
        isExecuting = false;
        attackRoutine = null;
        stunRoutine = null;
        awareness.Forget();
        decisionModel = new MonsterDecisionModel();
        primaryStimulus = default;
        normalAiState = NormalAiState.Patrol;
        currentIntent = NormalAiState.Patrol.ToString();
        currentTactic = MonsterTactic.None.ToString();
        currentDecisionReason = MonsterDecisionReason.None.ToString();
        isChasing = false;
        isWandering = false;
        stateEnteredTime = Time.time;
        nextPatrolDecisionTime = Time.time;
        nextTacticalDecisionTime = Time.time;
        nextHearingUpdateTime = Time.time;
        nextLightUpdateTime = Time.time;
        nextCoordinationBroadcastTime = Time.time;
        hasPendingNoise = false;
        searchStep = 0;
        isSearchPausing = false;
        deliberatePauseUntil = 0f;
        silentApproachActive = false;
        hollowLightExposureProgress = 0f;
        hollowLightExposureState = "None";
        lastFocusedLightFrame = -999f;
        flankDirection = Random.value < 0.5f ? -1 : 1;
        agent.isStopped = false;
        animator.speed = 1f;   // 공격 도중 비활성화된 경우 가속이 남지 않도록 초기화

        // 스턴/공격 도중 비활성화된 경우 Bool이 켜진 채로 남을 수 있으므로 함께 초기화.
        // Hollow용 컨트롤러에는 없는 파라미터도 있어서 존재 여부를 확인한다.
        ResetBool(MonsterAnimParameter.Stun);
        ResetBool(MonsterAnimParameter.Attack);
        ResetBool(MonsterAnimParameter.Move);

        hasMoveParameter = HasParameter(MonsterAnimParameter.Move);

        TypeCheck(type);
        if (type == Define.MonsterType.Hollow)
        {
            meshRenderer = GetComponent<SkinnedMeshRenderer>();
            hollowMaterial = meshRenderer.material;
        }
        else
        {
            MonsterNoiseSystem.NoiseEmitted -= OnNoiseEmitted;
            MonsterNoiseSystem.NoiseEmitted += OnNoiseEmitted;
            MonsterCoordinationHub.AlertBroadcast -= OnSharedAlert;
            MonsterCoordinationHub.AlertBroadcast += OnSharedAlert;
        }

        StartCoroutine(DetectRoutine());
    }

    void OnDisable()
    {
        MonsterNoiseSystem.NoiseEmitted -= OnNoiseEmitted;
        MonsterCoordinationHub.AlertBroadcast -= OnSharedAlert;
        StopAllCoroutines();
    }

    void Update()
    {
        if (isExecuting) return;

        if (type == Define.MonsterType.Hollow &&
            hollowLightExposureProgress > 0f &&
            Time.time - lastFocusedLightFrame > 0.08f)
        {
            hollowLightExposureProgress = 0f;
            hollowLightExposureState = "Beam lost";
        }

        SpeedControl();
        MoveAnimationControl();

        if (type == Define.MonsterType.Hollow)
            Hollow();
        else if (!isStunned && !isAttacking &&
                 normalAiState == NormalAiState.Searching && isSearchPausing)
            FaceWorldPoint(searchLookTarget, searchTurnSpeed);
        else if (!isStunned && !isAttacking && Time.time < deliberatePauseUntil)
            FaceWorldPoint(primaryStimulus.Position, searchTurnSpeed);

        if (type != Define.MonsterType.Hollow)
        {
            currentSuspicion = awareness.Suspicion;
            currentPositionUncertainty = awareness.PositionUncertainty;
        }
    }

    /// <summary>
    /// Move Bool은 목적지 설정 시점이 아니라 에이전트가 실제로 움직이고 있는지로 결정한다.
    /// 켜는 쪽만 여기저기 흩어져 있으면 목적지에 도착해 멈춰도 walk 모션이 계속 돌게 된다.
    /// </summary>
    private void MoveAnimationControl()
    {
        if (!hasMoveParameter || isStunned || isAttacking) return;

        bool isMoving = !agent.isStopped
                        && agent.velocity.sqrMagnitude > MOVE_ANIM_THRESHOLD * MOVE_ANIM_THRESHOLD;

        animator.SetBool(MonsterAnimParameter.Move, isMoving);
    }

    private void SpeedControl()
    {
        // 공격 중에는 AttackRoutine이 agent.isStopped로 이동을 막으므로 속도만 0으로 맞춰둔다
        if (isStunned || isAttacking || Time.time < deliberatePauseUntil)
        {
            agent.speed = 0;
            return;
        }

        if (normalAiState == NormalAiState.Hunting)
        {
            // 플레이어가 시야에 실제로 들어와 있는 동안에만 가속.
            // 소리/빛 감지나 마지막 위치 추적 중에는 평소 추적 속도 유지
            agent.speed = silentApproachActive
                ? Mathf.Min(investigationSpeed, chasingSpeed * 0.7f)
                : (fov.playerDetected ? sightChasingSpeed : chasingSpeed);
        }
        else if (normalAiState == NormalAiState.Investigating ||
                 normalAiState == NormalAiState.Searching)
        {
            agent.speed = investigationSpeed;
        }
        else
        {
            agent.speed = normalSpeed;
        }
    }

    /// <summary>
    /// 몬스터 타입별 기본 수치 설정. 데미지/시야/청각은 난이도(GameManager)에 따라 결정됨
    /// </summary>
    private void TypeCheck(Define.MonsterType type)
    {
        GameManager.DifficultyData data = GameManager.Data;

        switch (type)
        {
            case Define.MonsterType.Normal:
                mh.SetHealth(500);
                mh.ableToKill = false;
                soundDetectionRange = data.soundDetectionRange;
                soundDetctionMinRange = data.soundDetectionMinRange;
                lightDetectionRange = data.lightDetectionRange;
                fov.SetSight(data.sightRadius, data.sightAngle);
                damage = data.monsterDamage;
                chasingSpeed = 3.5f;
                normalSpeed = 1.5f;
                break;
            case Define.MonsterType.Hollow:
                // 시각/청각 없음. 총 한 발에 소멸하며, 접촉 시 비명 + 슬로우
                mh.SetHealth(1);
                mh.ableToKill = true;
                damage = 0;
                chasingSpeed = 1.5f;
                normalSpeed = 1.5f;
                hollowApproachRange = data.hollowApproachRange;
                break;
        }
    }

    private void Attack()
    {
        if (ph.IsDead || isAttacking || isStunned || isExecuting) return;

        if (!fov.playerDetected || HorizontalDistanceToPlayer() > attackRange) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    /// <summary>
    /// 공격 한 사이클. 데미지 타이밍을 초 단위 타이머로 따로 재지 않고 Attack 클립의
    /// 재생 진행도(normalizedTime)를 그대로 따라가기 때문에, 클립 길이나 전이 시간이
    /// 바뀌어도 모션과 타격 판정이 항상 같은 프레임에 맞는다.
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        animator.SetBool(MonsterAnimParameter.Move, false);
        animator.SetBool(MonsterAnimParameter.Attack, true);

        // 공격 동작 가속. 공격 중에는 이동하지 않으므로 다른 모션에 영향 없음.
        // 타격 판정이 클립 normalizedTime을 따라가므로 판정 타이밍도 함께 빨라짐
        animator.speed = attackAnimSpeed;

        // 애니메이터가 실제로 Attack 상태에 진입할 때까지 대기 (전이 시간만큼 지연됨)
        float timeout = Time.time + 1f;
        while (!IsInAttackState() && Time.time < timeout)
        {
            if (isStunned || isExecuting) { EndAttack(); yield break; }
            FaceTarget();
            yield return null;
        }

        // 전이가 끝내 일어나지 않으면 데미지도 넣지 않고 취소 (모션 없는 공격 방지)
        if (!IsInAttackState())
        {
            EndAttack();
            yield break;
        }

        // 타격 프레임까지 클립을 따라감
        while (IsInAttackState() && GetAttackProgress() < attackHitNormalizedTime)
        {
            if (isStunned || isExecuting) { EndAttack(); yield break; }
            FaceTarget();
            yield return null;
        }

        // 클립이 끝나고 Idle로 복귀했을 때 AnyState 전이가 다시 걸리지 않도록 여기서 내림
        animator.SetBool(MonsterAnimParameter.Attack, false);

        if (!isStunned && !isExecuting && !ph.IsDead)
        {
            if (IsPlayerInHitZone())
            {
                ph.DecreaseHealth(damage);

                // 체력이 모두 닳으면 처형 (처형 연출은 정상 속도로)
                if (ph.IsDead)
                {
                    animator.speed = 1f;
                    isAttacking = false;
                    attackRoutine = null;
                    Execute();
                    yield break;
                }
            }
        }

        // 남은 클립이 끝날 때까지 대기
        timeout = Time.time + 3f;
        while (IsInAttackState() && Time.time < timeout)
        {
            if (isStunned || isExecuting) { EndAttack(); yield break; }
            yield return null;
        }

        yield return new WaitForSeconds(attackRecovery);

        EndAttack();
    }

    private bool IsInAttackState()
    {
        return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AttackStateHash;
    }

    private float GetAttackProgress()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.loop ? info.normalizedTime % 1f : Mathf.Clamp01(info.normalizedTime);
    }

    /// <summary>
    /// 몬스터의 피벗은 발밑, 플레이어의 피벗은 그보다 위에 있어서 3D 거리로 재면
    /// 실제보다 멀게 나온다. 공격 판정은 항상 수평 거리로 잰다.
    /// </summary>
    private float HorizontalDistanceToPlayer()
    {
        Vector3 delta = player.transform.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    /// <summary>
    /// 타격 프레임에 데미지를 줄지 판정. 거리뿐 아니라 몬스터가 실제로 플레이어를
    /// 바라보고 있는지도 확인해서, 모션과 상관없이 맞는 느낌을 없앤다.
    /// </summary>
    private bool IsPlayerInHitZone()
    {
        Vector3 delta = player.transform.position - transform.position;
        delta.y = 0f;

        if (delta.magnitude > attackHitRange) return false;

        return Vector3.Angle(transform.forward, delta) <= attackHitAngle * 0.5f;
    }

    /// <summary>
    /// 공격 모션이 엉뚱한 방향으로 나가지 않도록 준비 동작 중 플레이어 쪽으로 회전.
    /// NavMeshAgent가 멈춰 있는 동안에는 회전도 멈추므로 직접 돌려준다.
    /// </summary>
    private void FaceTarget()
    {
        Vector3 direction = player.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * attackTurnSpeed);
    }

    private void EndAttack()
    {
        animator.SetBool(MonsterAnimParameter.Attack, false);
        animator.speed = 1f;   // 공격 가속 해제

        isAttacking = false;
        attackRoutine = null;

        if (!isStunned && !isExecuting)
            agent.isStopped = false;
    }

    /// <summary>
    /// 처형 : 처형 애니메이션 재생 후 게임오버
    /// </summary>
    private void Execute()
    {
        if (isExecuting) return;
        isExecuting = true;

        agent.isStopped = true;
        animator.SetBool(MonsterAnimParameter.Attack, false);
        animator.SetBool(MonsterAnimParameter.Move, false);

        pc.OnExecuted(transform);

        // 처형 애니메이션이 끝나면 게임오버 처리.
        // Execution 클립이 아직 애니메이터에 없는 경우에는 트리거가 무시되고 콜백 타이밍도
        // 보장되지 않으므로, 파라미터가 있을 때만 애니메이션에 맡기고 없으면 바로 처리한다.
        if (HasParameter(MonsterAnimParameter.Execution))
            animator.SetTrigger(MonsterAnimParameter.Execution, GameOver);
        else
            GameOver();
    }

    private void ResetBool(int nameHash)
    {
        if (HasParameter(nameHash))
            animator.SetBool(nameHash, false);
    }

    private bool HasParameter(int nameHash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == nameHash)
                return true;
        }
        return false;
    }

    private void GameOver()
    {
        Debug.Log("GAME OVER");
        // TODO : 게임오버 UI / 씬 전환 연결
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 총에 맞았을 때 일시적으로 움직임이 멈춤 (Normal 타입 전용, Monster_Health에서 호출)
    /// </summary>
    public void Stun(float duration)
    {
        if (isExecuting || type != Define.MonsterType.Normal) return;

        // 공격 중 스턴이 걸리면 진행 중이던 공격 사이클을 정리해서 agent.isStopped 등이 충돌하지 않게 함
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            isAttacking = false;
            animator.speed = 1f;   // 공격 가속 해제 (피격/스턴 모션은 정상 속도로)
        }

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    /// <summary>
    /// 모션 순서(피격 -> 스턴 대기 -> 복귀)는 애니메이터가 관장한다.
    /// GetHit는 Trigger라 피격 리액션이 한 번만 재생되고, 그 뒤 Idle로 떨어진다.
    /// Stun Bool은 그동안 Idle -> walk 전이를 막아 스턴이 풀릴 때까지 붙잡아 두는 역할.
    /// </summary>
    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        animator.SetBool(MonsterAnimParameter.Attack, false);
        animator.SetBool(MonsterAnimParameter.Move, false);
        animator.SetBool(MonsterAnimParameter.Stun, true);
        animator.SetTrigger(MonsterAnimParameter.GetHit);

        yield return new WaitForSeconds(duration);

        animator.SetBool(MonsterAnimParameter.Stun, false);

        isStunned = false;
        agent.isStopped = false;
        stunRoutine = null;
    }

    /// <summary>
    /// Hollow의 비명 등 외부 소음 발생 시 해당 위치로 이동 (Normal 타입 전용)
    /// </summary>
    public void OnAlerted(Vector3 position)
    {
        if (type != Define.MonsterType.Normal || isStunned || isExecuting) return;

        awareness.RegisterStimulus(position, 1f, 1f, true);
        RecordStimulus(MonsterStimulusType.SharedAlert, position, 1f, 1f, Vector3.zero, true);
        EnterInvestigating(position);

#if UNITY_EDITOR
        Debug.Log("Alerted");
#endif
    }

    /// <summary>
    /// 거리에 따라 투명 쉐이더의 왜곡값을 조절 (가까울수록 보임)
    /// </summary>
    private void Hollow()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        float currentDistortion = hollowMaterial.GetFloat(DistortionId);
        float target = distanceToPlayer <= hollowRange ? 0.05f : 0f;

        hollowMaterial.SetFloat(DistortionId, Mathf.Lerp(currentDistortion, target, Time.deltaTime * 2f));
    }

    /// <summary>
    /// Hollow 행동 : 시각/청각 없이 일정 거리 안의 플레이어에게 점점 다가가고,
    /// 닿으면 비명을 지르며 Normal 몬스터를 부르고 플레이어에게 슬로우를 검
    /// </summary>
    private void HollowBehaviour()
    {
        if (ph.IsDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= hollowTouchRange)
        {
            Scream();
            return;
        }

        if (distanceToPlayer <= hollowApproachRange)
            agent.SetDestination(player.transform.position);
        else
            agent.ResetPath();
    }

    public void ReceiveFocusedFlashlight(float deltaTime)
    {
        if (type != Define.MonsterType.Hollow ||
            mh == null ||
            mh.isDead ||
            deltaTime <= 0f)
            return;

        lastFocusedLightFrame = Time.time;
        hollowLightExposureState = "Direct focused beam";
        hollowLightExposureProgress += deltaTime;
        if (hollowLightExposureProgress < hollowFocusedLightExposure)
            return;

        hollowLightExposureState = "Dispelled";
        if (agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
        mh.SetHealth(0);
        mh.CheckHealth();
    }

    private void Scream()
    {
        Debug.Log("Scream");
        Instantiate(Managers.Resource.LoadPrefab("Monster/ScreamAudio"), transform.position, transform.rotation);

        // 비명 위치로 Normal 몬스터를 불러들임
        foreach (Monster_Controller monster in FindObjectsOfType<Monster_Controller>())
        {
            if (monster != this)
                monster.OnAlerted(transform.position);
        }

        // 플레이어에게 슬로우 부여
        pc.ApplySlow(GameManager.Data.hollowSlowRate, GameManager.Data.hollowSlowDuration);

        // 비명 후 소멸
        mh.SetHealth(0);
    }

    private IEnumerator DetectRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(DELAY);

        while (true)
        {
            yield return wait;

            if (isStunned || isExecuting) continue;

            // Hollow는 시각/청각 없이 거리 기반으로만 행동
            if (type == Define.MonsterType.Hollow)
            {
                HollowBehaviour();
                continue;
            }

            awareness.AgeEvidence(
                DELAY,
                suspicionDecayDelay,
                suspicionDecayPerSecond,
                uncertaintyGrowthPerSecond);

            bool hasVisualContact = DetectSight();
            if (!hasVisualContact &&
                (normalAiState != NormalAiState.Hunting ||
                 awareness.TimeSinceVisualContact > visualMemoryDuration))
            {
                DetectSounds();
                DetectLight();
            }

            UpdateNormalBehaviour(hasVisualContact);
            Attack();
        }
    }

    private void UpdateNormalBehaviour(bool hasVisualContact)
    {
        switch (normalAiState)
        {
            case NormalAiState.Hunting:
                if (hasVisualContact)
                    UpdateTacticalPursuit();
                else if (awareness.TimeSinceVisualContact <= visualMemoryDuration)
                    TrySetDestination(awareness.PredictPosition(predictionLeadTime), 2f);
                else
                    EnterSearching(awareness.PredictPosition(predictionLeadTime));
                break;

            case NormalAiState.Investigating:
                UpdateInvestigation();
                break;

            case NormalAiState.Searching:
                UpdateSearch();
                break;

            default:
                UpdatePatrol();
                break;
        }
    }

    private bool DetectSight()
    {
        if (!fov.playerDetected) return false;

        bool wasHunting = normalAiState == NormalAiState.Hunting;
        Vector3 playerVelocity = playerRigidbody != null ? playerRigidbody.velocity : Vector3.zero;
        float confirmation = HorizontalDistanceToPlayer() <= closeRangeInstantDetection
            ? 0.01f
            : visualConfirmationTime;

        awareness.Observe(player.transform.position, playerVelocity, DELAY, confirmation);
        RecordStimulus(MonsterStimulusType.Visual, player.transform.position, 1f, 0f,
            playerVelocity, false);
        if (awareness.State == MonsterAwarenessMemory.AwarenessState.Alerted)
        {
            EnterState(NormalAiState.Hunting);
            if (!wasHunting && Time.time >= nextCoordinationBroadcastTime)
            {
                MonsterCoordinationHub.Broadcast(
                    GetInstanceID(),
                    player.transform.position,
                    1f,
                    0f,
                    coordinationRange);
                nextCoordinationBroadcastTime =
                    Time.time + coordinationBroadcastCooldown;
            }
        }
        else if (normalAiState != NormalAiState.Hunting)
            EnterInvestigating(player.transform.position);

#if UNITY_EDITOR
        Debug.Log("Sight Detected");
#endif
        return true;
    }

    private void DetectSounds()
    {
        if (fov.playerDetected || Time.time < nextHearingUpdateTime) return;

        if (hasPendingNoise)
        {
            hasPendingNoise = false;
            awareness.RegisterStimulus(
                pendingNoisePosition,
                pendingNoiseConfidence,
                pendingNoiseUncertainty,
                false);
            RecordStimulus(MonsterStimulusType.Sound, pendingNoisePosition,
                pendingNoiseConfidence, pendingNoiseUncertainty, Vector3.zero, false);
            EnterInvestigating(pendingNoisePosition);
            nextHearingUpdateTime = Time.time + Random.Range(0.35f, 0.65f);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer >= soundDetectionRange) return;

        // 손전등 흔들기 등 자식 오브젝트의 소리도 감지하도록 자식 AudioSource까지 캐싱해서 사용
        if (playerAudioSources == null)
            playerAudioSources = player.GetComponentsInChildren<AudioSource>(true);

        bool nearRange = distanceToPlayer < soundDetctionMinRange;   // 이 안에서는 아무리 작은 소리도 감지
        bool occluded = IsSoundOccluded();
        float bestConfidence = 0f;

        foreach (AudioSource source in playerAudioSources)
        {
            if (source == null || !source.isPlaying) continue;
            if (!(nearRange ? source.volume > 0f : source.volume > detectionMinSound)) continue;

            float distanceConfidence = 1f - Mathf.InverseLerp(soundDetctionMinRange,
                soundDetectionRange, distanceToPlayer);
            float confidence = Mathf.Clamp01(source.volume * 0.45f + distanceConfidence * 0.55f);
            if (nearRange) confidence = Mathf.Max(confidence, 0.65f);
            if (occluded) confidence *= hearingOcclusionMultiplier;
            bestConfidence = Mathf.Max(bestConfidence, confidence);
        }

        if (bestConfidence < 0.1f) return;

        // 멀거나 벽 너머의 소리는 방향만 대략 파악한다. 소리만으로 현재 위치를 정확히
        // 추적하지 않아, 플레이어가 조용히 자리를 옮기면 실제로 따돌릴 수 있다.
        float uncertainty = hearingPositionError * (1f - bestConfidence);
        Vector3 heardPosition = player.transform.position + Random.insideUnitSphere * uncertainty;
        heardPosition.y = player.transform.position.y;
        heardPosition = SampleNavMeshPosition(heardPosition, Mathf.Max(1f, uncertainty));

        awareness.RegisterStimulus(heardPosition, bestConfidence, uncertainty, false);
        RecordStimulus(MonsterStimulusType.Sound, heardPosition, bestConfidence,
            uncertainty, playerRigidbody != null ? playerRigidbody.velocity : Vector3.zero, false);
        EnterInvestigating(heardPosition);
        nextHearingUpdateTime = Time.time + Random.Range(0.65f, 1f);

#if UNITY_EDITOR
        Debug.Log(occluded ? "Muffled sound investigated" : "Sound investigated");
#endif
    }

    /// <summary>
    /// 켜져 있는 손전등 빛 감지 (Normal 전용).
    /// 광원 자체 또는 빛이 닿는 지점(벽/바닥의 밝은 원)이 감지 범위 안에 있고
    /// 벽에 가려지지 않았으면 보이는 광원/빛 자국을 조사. 범위는 난이도별로 청각과 비슷하게 설정됨
    /// </summary>
    private void DetectLight()
    {
        if (fov.playerDetected || Time.time < nextLightUpdateTime) return;

        // Flashlight_Controller는 플레이어 Awake에서 런타임에 추가되므로 지연 조회
        if (flashlight == null)
        {
            flashlight = player.GetComponentInChildren<Flashlight_Controller>(true);
            if (flashlight == null) return;
        }

        if (!flashlight.IsOn) return;

        Transform lightTr = flashlight.transform;

        // 빛이 닿는 지점(벽/바닥의 밝은 원)을 구함
        Vector3 spot;
        if (Physics.Raycast(lightTr.position, lightTr.forward, out RaycastHit spotHit,
                lightSpotMaxDistance, fov.ObstructionMask))
            spot = spotHit.point;
        else
            spot = lightTr.position + lightTr.forward * lightSpotMaxDistance;

        Vector3 eye = transform.position + Vector3.up;   // 발밑 피벗 보정

        bool seesSource = CanSeeLightPoint(eye, lightTr.position);
        bool seesSpot = CanSeeLightPoint(eye, spot);
        if (!seesSource && !seesSpot) return;

        // 광원을 직접 봤을 때만 플레이어 근처를 의심한다. 벽이나 바닥의 빛 자국만
        // 보였다면 그 지점을 조사하며, 벽 너머 플레이어 좌표를 초능력처럼 알지 못한다.
        Vector3 investigationPoint = seesSource ? lightTr.position : spot;
        float confidence = seesSource ? 0.75f : 0.45f;
        investigationPoint = SampleNavMeshPosition(investigationPoint, 2f);

        awareness.RegisterStimulus(
            investigationPoint,
            confidence,
            seesSource ? 0.5f : 2f,
            false);
        RecordStimulus(MonsterStimulusType.Light, investigationPoint, confidence,
            seesSource ? 0.5f : 2f, Vector3.zero, false);
        EnterInvestigating(investigationPoint);
        nextLightUpdateTime = Time.time + Random.Range(0.8f, 1.2f);

#if UNITY_EDITOR
        Debug.Log(seesSource ? "Flashlight source investigated" : "Light spot investigated");
#endif
    }

    private void EnterState(NormalAiState nextState)
    {
        if (normalAiState == nextState) return;

        normalAiState = nextState;
        stateEnteredTime = Time.time;
        isChasing = nextState == NormalAiState.Hunting;
        currentIntent = nextState.ToString();
        currentTactic = MonsterTactic.None.ToString();
        currentDecisionReason = MonsterDecisionReason.None.ToString();
        if (nextState != NormalAiState.Hunting)
            silentApproachActive = false;
        isWandering = false;

        if (nextState != NormalAiState.Searching)
            isSearchPausing = false;
    }

    private void EnterInvestigating(Vector3 position)
    {
        if (normalAiState != NormalAiState.Hunting ||
            awareness.TimeSinceVisualContact > visualMemoryDuration)
            EnterState(NormalAiState.Investigating);

        stateEnteredTime = Time.time;
        TrySetDestination(position, 2f);
    }

    private void UpdateInvestigation()
    {
        if (awareness.State == MonsterAwarenessMemory.AwarenessState.Calm)
        {
            ReturnToPatrol();
            return;
        }

        if (Time.time - stateEnteredTime >= investigationTimeout)
        {
            EnterSearching(awareness.LastKnownPosition);
            return;
        }

        if (Time.time >= nextTacticalDecisionTime)
        {
            MonsterDecision decision = EvaluateDecision(false);
            ApplyDecisionDebug(decision);
            if (decision.Tactic == MonsterTactic.Listen)
            {
                deliberatePauseUntil = Time.time + Random.Range(0.8f, 1.6f);
                if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
                nextTacticalDecisionTime = deliberatePauseUntil + tacticalRepathInterval;
                return;
            }

            if (decision.Tactic == MonsterTactic.Hold ||
                decision.Tactic == MonsterTactic.Intercept ||
                decision.Tactic == MonsterTactic.Flank)
                TrySetDestination(decision.FocusPoint, 3f);

            nextTacticalDecisionTime = Time.time + tacticalRepathInterval;
        }

        if (!agent.pathPending &&
            (!agent.hasPath || agent.remainingDistance <= searchPointTolerance))
        {
            EnterSearching(awareness.LastKnownPosition);
        }
    }

    /// <summary>
    /// 먼 거리에서는 플레이어의 마지막 이동 방향을 예측하고 옆으로 돌아 들어간다.
    /// 가까워졌을 때만 직선 추격하므로 문/복도에서 계속 뒤만 따라가는 단순한 움직임을 줄인다.
    /// </summary>
    private void UpdateTacticalPursuit()
    {
        if (Time.time < nextTacticalDecisionTime) return;

        Vector3 predictedVelocity = Vector3.ClampMagnitude(awareness.LastKnownVelocity,
            sightChasingSpeed * 1.5f);
        Vector3 predicted = awareness.LastKnownPosition + predictedVelocity * predictionLeadTime;
        MonsterDecision decision = EvaluateDecision(true);
        ApplyDecisionDebug(decision);
        silentApproachActive = decision.Tactic == MonsterTactic.SilentApproach;

        Vector3 destination = decision.Tactic == MonsterTactic.None
            ? predicted
            : decision.FocusPoint;
        if (decision.Tactic == MonsterTactic.Flank && flankDirection < 0)
            destination = player.transform.position -
                          (destination - player.transform.position);

        if (!TrySetDestination(destination, 2.5f))
            TrySetDestination(predicted, 2.5f);

        if (decision.Tactic == MonsterTactic.Flank && Random.value < 0.25f)
            flankDirection *= -1;
        nextTacticalDecisionTime = Time.time + tacticalRepathInterval;
    }
    private void EnterSearching(Vector3 center)
    {
        EnterState(NormalAiState.Searching);
        searchCenter = SampleNavMeshPosition(center, 3f);
        searchDeadline = Time.time + searchDuration;
        searchStep = 0;
        isSearchPausing = false;
        SetNextSearchDestination();
    }

    private void UpdateSearch()
    {
        if (Time.time >= searchDeadline ||
            awareness.State == MonsterAwarenessMemory.AwarenessState.Calm)
        {
            ReturnToPatrol();
            return;
        }

        if (isSearchPausing)
        {
            if (Time.time < nextSearchMoveTime) return;

            isSearchPausing = false;
            SetNextSearchDestination();
            return;
        }

        if (agent.pathPending) return;
        if (agent.hasPath && agent.remainingDistance > searchPointTolerance) return;

        isSearchPausing = true;
        nextSearchMoveTime = Time.time + Random.Range(
            Mathf.Min(searchPauseRange.x, searchPauseRange.y),
            Mathf.Max(searchPauseRange.x, searchPauseRange.y));

        float lookAngle = Random.Range(-110f, 110f);
        Vector3 lookDirection = Quaternion.Euler(0f, lookAngle, 0f) * transform.forward;
        searchLookTarget = transform.position + lookDirection * 4f;
        if (agent.isOnNavMesh) agent.ResetPath();
    }

    private void SetNextSearchDestination()
    {
        float effectiveSearchRadius = searchRadius +
                                      Mathf.Min(searchRadius, awareness.PositionUncertainty);
        MonsterSearchContext context = new MonsterSearchContext
        {
            Now = Time.time,
            Center = searchCenter,
            LastKnownVelocity = awareness.LastKnownVelocity,
            SearchRadius = effectiveSearchRadius,
            RecentVisitWindow = 6f,
            FallbackStartIndex = searchStep,
        };
        Vector3 destination = SampleNavMeshPosition(
            MonsterSearchPlanner.GetFallbackPoint(context, searchStep), 3f);
        searchStep++;

        if (searchStep >= 4 && Time.time >= nextTacticalDecisionTime)
        {
            MonsterDecision decision = EvaluateDecision(false);
            if (decision.Tactic == MonsterTactic.DoubleBack ||
                decision.Tactic == MonsterTactic.Intercept)
            {
                ApplyDecisionDebug(decision);
                destination = SampleNavMeshPosition(decision.FocusPoint, 3f);
            }
            nextTacticalDecisionTime = Time.time + tacticalRepathInterval;
        }

        if (!TrySetDestination(destination, 3f))
            TrySetDestination(searchCenter, 3f);
    }
    private void UpdatePatrol()
    {
        if (Time.time < nextPatrolDecisionTime || agent.pathPending) return;

        if (agent.hasPath && agent.remainingDistance > searchPointTolerance)
        {
            nextPatrolDecisionTime = Time.time + 0.5f;
            return;
        }

        if (isWandering)
        {
            isWandering = false;
            nextPatrolDecisionTime = Time.time + patrolPause + Random.Range(0f, 1f);
            if (agent.isOnNavMesh) agent.ResetPath();
            return;
        }

        Vector3 randomDirection = Random.insideUnitSphere * WANDER_RADIUS + transform.position;
        randomDirection.y = transform.position.y;
        Vector3 destination = SampleNavMeshPosition(randomDirection, WANDER_RADIUS);
        isWandering = TrySetDestination(destination, WANDER_RADIUS);
        nextPatrolDecisionTime = Time.time + WANDER_INTERVAL;
    }

    private void ReturnToPatrol()
    {
        awareness.Forget();
        EnterState(NormalAiState.Patrol);
        nextPatrolDecisionTime = Time.time + patrolPause;
        if (agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    private bool IsSoundOccluded()
    {
        return IsSoundOccluded(player.transform.position);
    }

    private bool IsSoundOccluded(Vector3 soundPosition)
    {
        Vector3 from = transform.position + Vector3.up;
        Vector3 to = soundPosition + Vector3.up;
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.1f) return false;

        return Physics.Raycast(from, delta / distance, distance - 0.1f, fov.ObstructionMask);
    }

    private void OnNoiseEmitted(MonsterNoiseEvent noise)
    {
        if (type != Define.MonsterType.Normal || isStunned || isExecuting)
            return;

        float effectiveRange = Mathf.Min(soundDetectionRange, noise.Radius);
        float distance = Vector3.Distance(transform.position, noise.Position);
        if (effectiveRange <= 0f || distance > effectiveRange)
            return;

        float distanceConfidence = 1f - Mathf.Clamp01(distance / effectiveRange);
        float confidence = Mathf.Clamp01(noise.Loudness * 0.6f + distanceConfidence * 0.4f);
        if (IsSoundOccluded(noise.Position))
            confidence *= hearingOcclusionMultiplier;
        if (confidence < 0.1f || hasPendingNoise && confidence <= pendingNoiseConfidence)
            return;

        float uncertainty = hearingPositionError * (1f - confidence);
        Vector3 perceivedPosition = noise.Position + Random.insideUnitSphere * uncertainty;
        perceivedPosition.y = noise.Position.y;

        pendingNoisePosition = SampleNavMeshPosition(
            perceivedPosition,
            Mathf.Max(1f, uncertainty));
        pendingNoiseConfidence = confidence;
        pendingNoiseUncertainty = uncertainty;
        hasPendingNoise = true;
    }

    private void OnSharedAlert(MonsterSharedAlert alert)
    {
        if (type != Define.MonsterType.Normal ||
            alert.SenderId == GetInstanceID() ||
            isStunned ||
            isExecuting ||
            fov.playerDetected)
            return;

        float distance = Vector3.Distance(transform.position, alert.Position);
        if (alert.Radius <= 0f || distance > alert.Radius)
            return;

        float minDelay = Mathf.Min(coordinationDelayRange.x, coordinationDelayRange.y);
        float maxDelay = Mathf.Max(coordinationDelayRange.x, coordinationDelayRange.y);
        StartCoroutine(ReceiveSharedAlert(
            alert,
            distance,
            Random.Range(minDelay, maxDelay)));
    }

    private IEnumerator ReceiveSharedAlert(
        MonsterSharedAlert alert,
        float distance,
        float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isStunned || isExecuting || fov.playerDetected)
            yield break;

        float distanceFactor = 1f - Mathf.Clamp01(distance / alert.Radius);
        float confidence = alert.Confidence * coordinationConfidenceLoss *
                           Mathf.Lerp(0.65f, 1f, distanceFactor);
        if (IsSoundOccluded(alert.Position))
            confidence *= 0.75f;
        if (confidence < 0.15f)
            yield break;

        float uncertainty = alert.Uncertainty +
                            hearingPositionError * (1f - confidence) +
                            coordinationSpread * (1f - distanceFactor);
        Vector3 offset = Random.insideUnitSphere * uncertainty;
        offset.y = 0f;
        Vector3 perceivedPosition = SampleNavMeshPosition(
            alert.Position + offset,
            Mathf.Max(1f, uncertainty));

        awareness.RegisterStimulus(
            perceivedPosition,
            confidence,
            uncertainty,
            true);
        RecordStimulus(MonsterStimulusType.SharedAlert, perceivedPosition, confidence,
            uncertainty, Vector3.zero, true);
        EnterInvestigating(perceivedPosition);
    }

    private void RecordStimulus(
        MonsterStimulusType stimulusType,
        Vector3 position,
        float confidence,
        float uncertaintyInMeters,
        Vector3 observedVelocity,
        bool shared)
    {
        float uncertainty = Mathf.Clamp01(
            uncertaintyInMeters / Mathf.Max(1f, searchRadius + hearingPositionError));
        primaryStimulus = new MonsterStimulus(
            stimulusType,
            position,
            confidence,
            uncertainty,
            Time.time,
            observedVelocity,
            shared);
    }

    private MonsterDecision EvaluateDecision(bool hasVisualContact)
    {
        if (decisionModel == null)
            decisionModel = new MonsterDecisionModel();

        Vector3 toMonster = transform.position - player.transform.position;
        toMonster.y = 0f;
        bool targetAware = toMonster.sqrMagnitude > 0.01f &&
                           Vector3.Dot(player.transform.forward, toMonster.normalized) > 0.45f;
        float stimulusAge = primaryStimulus.Type == MonsterStimulusType.None
            ? float.PositiveInfinity
            : primaryStimulus.Age(Time.time);
        float distance = HorizontalDistanceToPlayer();
        bool canRoute = agent.enabled && agent.isOnNavMesh;

        MonsterDecisionContext context = new MonsterDecisionContext
        {
            Now = Time.time,
            Suspicion = awareness.Suspicion,
            HasVisualContact = hasVisualContact,
            HasFreshStimulus = stimulusAge <= 3f,
            TargetAwareOfMonster = targetAware,
            HasReachableTargetPath = canRoute,
            HasInterceptRoute = canRoute && awareness.LastKnownVelocity.sqrMagnitude > 0.04f,
            HasFlankRoute = canRoute && flankDistance > 0f && distance > directPursuitRange,
            HasHoldPoint = canRoute && !hasVisualContact,
            HasCoverApproach = canRoute && hasVisualContact && !targetAware && distance > 5f,
            HasRecentSearchFailure = normalAiState == NormalAiState.Searching && searchStep >= 4,
            DistanceToTarget = distance,
            DistanceToLastKnownPosition =
                Vector3.Distance(transform.position, awareness.LastKnownPosition),
            TimeSinceVisualContact = awareness.TimeSinceVisualContact,
            TimeSinceStimulus = stimulusAge,
            SelfPosition = transform.position,
            TargetPosition = player.transform.position,
            TargetVelocity = playerRigidbody != null ? playerRigidbody.velocity : Vector3.zero,
            LastKnownPosition = awareness.LastKnownPosition,
            PrimaryStimulus = primaryStimulus,
        };
        return decisionModel.Evaluate(context);
    }

    private void ApplyDecisionDebug(MonsterDecision decision)
    {
        currentIntent = decision.Intent.ToString();
        currentTactic = decision.Tactic.ToString();
        currentDecisionReason = decision.Reason.ToString();
    }

    private Vector3 SampleNavMeshPosition(Vector3 position, float radius)
    {
        int areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas;
        return NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Max(0.5f, radius),
            areaMask)
            ? hit.position
            : position;
    }

    private bool TrySetDestination(Vector3 position, float sampleRadius)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;
        if (pathBuffer == null)
            pathBuffer = new NavMeshPath();

        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Max(0.5f, sampleRadius),
                agent.areaMask))
            return false;

        // 측면/수색 후보가 같은 NavMesh 섬에 실제로 연결되는지 먼저 확인한다.
        // SetDestination은 부분 경로도 성공으로 반환할 수 있어, 이 검사가 없으면 벽을 오래 밀 수 있다.
        if (!NavMesh.CalculatePath(agent.nextPosition, hit.position, agent.areaMask, pathBuffer) ||
            pathBuffer.status != NavMeshPathStatus.PathComplete)
            return false;

        return agent.SetDestination(hit.position);
    }

    private void FaceWorldPoint(Vector3 point, float turnSpeed)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direction), Time.deltaTime * turnSpeed);
    }

    /// <summary>
    /// 지점이 빛 감지 범위 안에 있고 벽에 가려지지 않았는지 판정
    /// </summary>
    private bool CanSeeLightPoint(Vector3 from, Vector3 point)
    {
        float distance = Vector3.Distance(from, point);
        if (distance > lightDetectionRange) return false;
        if (distance <= 0.1f) return true;

        // 벽 너머의 빛은 감지하지 않음 (표면 자체에 막히지 않도록 살짝 앞에서 멈춤)
        Vector3 direction = (point - from) / distance;
        return !Physics.Raycast(from, direction, Mathf.Max(0f, distance - 0.3f), fov.ObstructionMask);
    }

}
