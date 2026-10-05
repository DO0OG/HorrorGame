using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

/// <summary>
/// 플레이어 이동/입력/스태미나/발소리 담당.
/// 장비 전환 관련 로직은 Player_Controller.Equip.cs 참고
/// </summary>
public partial class Player_Controller : MonoBehaviour
{
    [Header("Keybinds")]
    internal static PlayerInput playerInput;
    internal static InputActionMap playerActionMap;
    internal static InputAction moveAction;
    internal static InputAction sprintAction;
    internal static InputAction crouchAction;
    internal static InputAction fireAction;
    internal static InputAction aimAction;
    internal static InputAction reloadAction;
    internal static InputAction funcAction;
    internal static InputAction flashAction;
    internal static InputAction equip1Action;
    internal static InputAction equip2Action;

    [Header("Movement")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform orientation;
    [SerializeField] private float gravityForce = 5f;
    [SerializeField] private float nowSpeed;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float sprintSpeed = 3.5f;
    [SerializeField] private float groundDrag;
    [SerializeField] private float dValue = 5f;
    [SerializeField] private float stamina;
    [SerializeField] private float maxStamina;
    internal static Vector2 moveInput;
    private Vector3 moveDirection;

    [Header("Ground Check")]
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask Ground;

    [Header("Animator")]
    [SerializeField] private Animator anim;

    [Header("FootStep")]
    private AudioSource audioSource;
    private float footstepCheckTimer;
    private bool waitingFootstepDisable;
    private const float FOOTSTEP_CHECK_INTERVAL = 0.25f;   // 바닥 재질 확인 주기

    [Header("ETC")]
    private CapsuleCollider capsuleCollider;
    public Light flashLight;
    private Flashlight_Controller flashlightController;
    private PlayerNoiseEmitter noiseEmitter;

    [Header("Equipment Inspection")]
    [SerializeField, Min(0.1f)] private float inspectionHoldDuration = 0.5f;
    private bool reloadPressActive;
    private Define.EquipType reloadPressEquip;
    private float flashlightInspectionTimer;

    [Header("Debuff")]
    [SerializeField] private float slowMultiplier = 1f;   // Hollow 접촉 시 이동속도 배율
    private Coroutine slowRoutine;
    private Player_SlowEffect slowEffect;

    internal static Define.EquipType currentEquip = Define.EquipType.Gun;

    private bool isSprint { get; set; }
    private bool isMoving { get; set; }
    private bool isRestoreStamina { get; set; }
    private bool grounded { get; set; }
    internal bool isCrouch { get; private set; }   // 몬스터 시야 판정 등 외부에서 참조
    private bool wantStand;   // 앉기 키를 뗐지만 바로 위에 엄폐물이 있어 앉기를 유지 중인 상태
    private Player_Shot playerShot { get; set; }
    internal static bool isFire { get; set; }
    internal static bool isAim { get; set; }
    internal static bool isReload { get; set; }

    // Start is called before the first frame update
    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        flashLight = GetComponentInChildren<Light>();
        audioSource = GetComponent<AudioSource>();
        playerShot = GetComponent<Player_Shot>();

        rb.freezeRotation = true;
        flashLight.enabled = false;

        // 손전등 컨트롤러 연결 (없으면 Light 오브젝트에 자동 추가)
        flashlightController = GetComponentInChildren<Flashlight_Controller>();
        if (flashlightController == null)
            flashlightController = flashLight.gameObject.AddComponent<Flashlight_Controller>();
        flashlightController.InitializeBatteryInspection(playerShot.AmmoInspectionMaterial);

        // 슬로우 화면 효과 (없으면 자동 추가)
        slowEffect = GetComponent<Player_SlowEffect>();
        if (slowEffect == null)
            slowEffect = gameObject.AddComponent<Player_SlowEffect>();

        noiseEmitter = GetComponent<PlayerNoiseEmitter>();
        if (noiseEmitter == null)
            noiseEmitter = gameObject.AddComponent<PlayerNoiseEmitter>();

        currentEquip = Define.EquipType.Gun;

        maxStamina = stamina;

        // 시작 장비에 맞춰 손전등 모델/라이트 상태 동기화
        flashlightController.SetHeld(currentEquip == Define.EquipType.Flashlight);

        InitEquip();   // 장비 전환용 참조 초기화 (Player_Controller.Equip.cs)

        KeyBind(); // Ű ���ε� ����
    }

    // Update is called once per frame
    void Update()
    {
        //�� üũ
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight / 2 + 0.1f, Ground);

        if (!grounded) rb.AddForce(Vector3.down * gravityForce);

        SpeedControl();
        AnimateControl();
        FootStep();
        UpdateEquipmentInspection();

        // 손전등을 들고 있을 때만 On/Off 가능 (전환 중에는 불가)
        if (flashAction.triggered && currentEquip == Define.EquipType.Flashlight && !isSwitchingEquip)
        {
            flashlightController.Toggle();
            MonsterNoiseSystem.Emit(MonsterNoiseKind.Equipment, transform.position, 0.2f, 5f);
        }

        if (currentEquip == Define.EquipType.Flashlight &&
            !isSwitchingEquip &&
            !flashlightController.IsInspecting &&
            Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                flashlightController.AdjustFocus(scroll);
        }

        if (isSprint) DecreaseStamina();
        if (!isSprint && stamina != maxStamina) IncreaseStamina();

        if (grounded) rb.drag = groundDrag;
        else rb.drag = 0;

        // 앉기 키를 뗐지만 엄폐물에 막혀 있던 경우, 벗어나면 자동으로 일어남
        if (wantStand && isCrouch && !HasCoverAbove())
            StandUp();
    }

    /// <summary>
    /// 앉은 상태에서 바로 위에 엄폐물이 있는지 확인.
    /// 얇은 레이를 짧게(키의 70%) 쏘므로 서 있을 수 있는 일반 천장에는 걸리지 않고,
    /// 책상/구조물처럼 바로 위를 덮는 엄폐물만 걸림 → 이전처럼 아무 데서나 갇히는 문제 없음
    /// </summary>
    private bool HasCoverAbove()
    {
        return Physics.Raycast(transform.position, Vector3.up, playerHeight * 0.7f,
            ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore);
    }

    private void StandUp()
    {
        capsuleCollider.height = playerHeight;
        isCrouch = false;
        wantStand = false;
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void KeyBind()
    {
        playerInput = GetComponent<PlayerInput>();

        playerActionMap = playerInput.actions.FindActionMap("Player");
        moveAction = playerActionMap.FindAction("Move");
        sprintAction = playerActionMap.FindAction("Sprint");
        crouchAction = playerActionMap.FindAction("Crouch");
        fireAction = playerActionMap.FindAction("Fire");
        aimAction = playerActionMap.FindAction("Aim");
        reloadAction = playerActionMap.FindAction("Reload");
        funcAction = playerActionMap.FindAction("Function");
        flashAction = playerActionMap.FindAction("Flash");
        equip1Action = playerActionMap.FindAction("Equip1");
        equip2Action = playerActionMap.FindAction("Equip2");

        moveAction.performed += ctx => {
            moveInput = ctx.ReadValue<Vector2>();
            nowSpeed = moveSpeed;
            isMoving = true;
        };
        moveAction.canceled += ctx => {
            moveInput = Vector2.zero;
            isMoving = false;
        };

        sprintAction.performed += ctx => {
            if (grounded && isMoving && !isCrouch && !isAim)
            {
                isSprint = true;
                nowSpeed = sprintSpeed;
            }
        };
        sprintAction.canceled += ctx => {
            isSprint = false;
            nowSpeed = moveSpeed;
        };

        crouchAction.performed += ctx => {
            wantStand = false;   // 다시 앉기를 누르면 자동 기상 대기 취소

            if (!isCrouch && grounded)
            {
                capsuleCollider.height = playerHeight / 2;
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                isSprint = false;
                isCrouch = true;
            }
        };
        crouchAction.canceled += ctx => {
            if (!isCrouch) return;

            // 바로 위에 엄폐물이 있으면 앉기 유지, 벗어나면 Update에서 자동으로 일어남
            if (HasCoverAbove())
                wantStand = true;
            else
                StandUp();
        };

        fireAction.started += ctx =>
        {
            // 총기를 들고 있을 때만 발사 가능 (전환 중에는 불가)
            if (currentEquip != Define.EquipType.Gun || isSwitchingEquip) return;
            if (playerShot.nowReload || playerShot.ammoCheck) return;
            if (Player_Shot.ammo == 0) GunSound.EmptySound();
            isFire = true;
            playerShot.Shot();
        };
        fireAction.canceled += ctx =>
        {
            isFire = false;
        };

        aimAction.started += ctx =>
        {
            if (isSwitchingEquip) return;

            // 손전등 장비 중 우클릭 → 흔들어서 배터리 충전
            if (currentEquip == Define.EquipType.Flashlight)
            {
                flashlightController.StartShake();
                return;
            }

            if (playerShot.nowReload) return;
            if (isSprint) isSprint = false;
            isAim = true;
        };
        aimAction.canceled += ctx =>
        {
            isAim = false;
            flashlightController.StopShake();
        };

        reloadAction.started += ctx =>
        {
            if (isSwitchingEquip) return;

            reloadPressActive = true;
            reloadPressEquip = currentEquip;

            if (reloadPressEquip == Define.EquipType.Gun)
            {
                isReload = true;
            }
            else if (reloadPressEquip == Define.EquipType.Flashlight)
            {
                flashlightInspectionTimer = 0f;
            }
        };
        reloadAction.canceled += ctx =>
        {
            if (!reloadPressActive) return;

            Define.EquipType pressedEquip = reloadPressEquip;
            reloadPressActive = false;

            if (pressedEquip == Define.EquipType.Gun)
            {
                isReload = false;
                if (!isSwitchingEquip && currentEquip == Define.EquipType.Gun)
                    playerShot.ReloadFunc();
                else
                    playerShot.CancelAmmoInspection();
            }
            else if (pressedEquip == Define.EquipType.Flashlight)
            {
                flashlightInspectionTimer = 0f;
                flashlightController.SetInspecting(false);
                flashlightController.SetBatteryInspectionVisible(false);
            }
        };

        // 1 : 총기 / 2 : 손전등
        if (equip1Action != null)
            equip1Action.started += ctx => ChangeEquip(Define.EquipType.Gun);
        if (equip2Action != null)
            equip2Action.started += ctx => ChangeEquip(Define.EquipType.Flashlight);
    }

    /// <summary>
    /// 처형 시작 시 몬스터가 호출. 모든 조작을 잠금
    /// </summary>
    public void OnExecuted(Transform monster)
    {
        playerActionMap.Disable();

        moveInput = Vector2.zero;
        isMoving = false;
        isSprint = false;
        isFire = false;
        isAim = false;

        rb.velocity = Vector3.zero;
        flashlightController.StopShake();
        CancelEquipmentInspection();

        // TODO : 처형 연출 카메라(몬스터 바라보기) 연결
    }

    private void AnimateControl()
    {
        float multiplier = 1;

        anim.SetBool(PlayerAnimParameter.Move, isMoving);
        anim.SetBool(PlayerAnimParameter.Crouch, isCrouch);
        anim.SetBool(PlayerAnimParameter.Aim, isAim);
        if (!isMoving) isSprint = false;
        anim.SetBool(PlayerAnimParameter.Sprint, isSprint);

        if (isCrouch)
            anim.SetFloat(PlayerAnimParameter.CrouchSpeed, multiplier / 2);
        else
            anim.SetFloat(PlayerAnimParameter.CrouchSpeed, multiplier);
    }

    private void UpdateEquipmentInspection()
    {
        if (isSwitchingEquip)
        {
            flashlightController.SetBatteryInspectionVisible(false);
            return;
        }

        if (currentEquip == Define.EquipType.Gun)
            return;

        bool holdingFlashlightInspection =
            reloadPressActive &&
            reloadPressEquip == Define.EquipType.Flashlight;

        if (!holdingFlashlightInspection)
        {
            flashlightController.SetInspecting(false);
            flashlightController.SetBatteryInspectionVisible(false);
            return;
        }

        flashlightInspectionTimer += Time.deltaTime;
        if (flashlightInspectionTimer < inspectionHoldDuration)
            return;

        flashlightController.SetInspecting(true);
        flashlightController.SetBatteryInspectionVisible(true);
    }

    private void CancelEquipmentInspection()
    {
        reloadPressActive = false;
        flashlightInspectionTimer = 0f;
        isReload = false;

        if (playerShot != null)
            playerShot.CancelAmmoInspection();
        if (flashlightController != null)
        {
            flashlightController.SetInspecting(false);
            flashlightController.SetBatteryInspectionVisible(false);
        }
    }

    private void MovePlayer()
    {
        if (isMoving)
        {
            moveDirection = orientation.forward * moveInput.y + orientation.right * moveInput.x;
            moveDirection.y = 0;

            rb.AddForce(moveDirection.normalized * nowSpeed * slowMultiplier * 10f, ForceMode.Force);
        }
    }

    /// <summary>
    /// Hollow 접촉 시 일정 시간 이동속도 감소
    /// </summary>
    public void ApplySlow(float rate, float duration)
    {
        if (slowRoutine != null)
            StopCoroutine(slowRoutine);
        slowRoutine = StartCoroutine(SlowRoutine(rate, duration));
    }

    private IEnumerator SlowRoutine(float rate, float duration)
    {
        slowMultiplier = rate;

        // 속도가 많이 깎일수록 화면 효과도 강해짐
        slowEffect.Play(duration, 1f - rate);

        yield return new WaitForSeconds(duration);

        slowMultiplier = 1f;
        slowRoutine = null;
    }

    private void SpeedControl()
    {
        if (isAim || isCrouch)
        {
            isSprint = false;
            nowSpeed = moveSpeed / 2f;
            return;
        }
        else
        {
            nowSpeed = moveSpeed;
        }

        if (stamina <= 0)
        {
            isRestoreStamina = true;
            isSprint = false;
            return;
        }
        else if (stamina >= 25f)
        {
            isRestoreStamina = false;
        }

        if (!isRestoreStamina && isSprint && isMoving && !isCrouch)
        {
            nowSpeed = sprintSpeed;
        }
        else if (isRestoreStamina)
        {
            isSprint = false;
            nowSpeed = moveSpeed;
        }
    }

    private void DecreaseStamina()
    {
        if (stamina > 0)
        {
            stamina -= dValue * Time.deltaTime;
        }
    }

    private void IncreaseStamina()
    {
        if (stamina < maxStamina)
        {
            stamina += dValue * Time.deltaTime / 2;
        }
    }

    /// <summary>
    /// 발밑 바닥의 태그(Concrete/Metal/Grass)에 맞는 발소리 경로 반환. 그 외에는 기본 Walk
    /// </summary>
    private string GetFootstepPath()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, playerHeight / 2 + 0.3f, Ground))
        {
            switch (hit.collider.tag)
            {
                case "Concrete": return "FootStep/Concrete";
                case "Metal": return "FootStep/Metal";
                case "Grass": return "FootStep/Grass";
            }
        }

        return "FootStep/Walk";
    }

    private void FootStep()
    {
        noiseEmitter.TickMovement(isMoving, isSprint, isCrouch, isAim);

        // 바닥 재질 확인(레이캐스트 + 리소스 조회)은 매 프레임 할 필요가 없어 주기적으로만 수행.
        // 바닥 종류가 바뀔 때만 클립 교체 (교체 시 재생이 끊기고 아래에서 다시 Play됨)
        footstepCheckTimer -= Time.deltaTime;
        if (footstepCheckTimer <= 0f)
        {
            footstepCheckTimer = FOOTSTEP_CHECK_INTERVAL;

            AudioClip clip = Managers.Resource.LoadAudioClip(GetFootstepPath());
            if (clip != null && audioSource.clip != clip)
                audioSource.clip = clip;
        }

        if (isSprint)
        {
            audioSource.pitch = 1.5f;
            audioSource.volume = 1;
        }
        else if (isCrouch || isAim)
        {
            audioSource.pitch = 0.4f;
            audioSource.volume = 0.5f;
        }
        else
        {
            audioSource.pitch = 0.775f;
            audioSource.volume = 0.75f;
        }

        if (isMoving)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.loop = true;
                audioSource.enabled = true;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.loop)
            {
                audioSource.loop = false;
            }

            // 매 프레임 코루틴을 새로 만들지 않도록 대기 중이 아닐 때만 시작
            if (!waitingFootstepDisable && audioSource.enabled)
                StartCoroutine(DisableAudioSourceAfterPlayback());
        }
    }

    private IEnumerator DisableAudioSourceAfterPlayback()
    {
        waitingFootstepDisable = true;

        while (audioSource.isPlaying)
        {
            yield return null;
        }

        audioSource.enabled = false;
        waitingFootstepDisable = false;
    }
}
