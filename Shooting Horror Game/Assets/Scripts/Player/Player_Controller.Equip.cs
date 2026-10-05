using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player_Controller의 장비 전환 파트.
/// 총/손전등 전환 시퀀스, Draw 애니메이션 재생, 총·팔 렌더러 표시 제어,
/// 전환 중 카메라(총구 추적) 처리를 담당한다.
/// </summary>
public partial class Player_Controller
{
    [Header("Equip")]
    [SerializeField] private float drawSpeedMultiplier = 1.3f;   // 전환 애니메이션 재생 속도 배율
    private bool isSwitchingEquip;
    private Define.EquipType? pendingEquip;   // 전환 중 입력된 다음 장비 (전환이 끝나면 바로 이어서 실행)
    private readonly List<Renderer> gunRenderers = new List<Renderer>();   // 총 + 팔 렌더러 (손전등 제외)
    private float drawClipLength = 1f;   // "Draw 2" 클립 길이 (런타임에 갱신)

    // 클립 끝 프레임에서 얼어붙는 느낌이 없도록, 유효 동작 구간까지만 기다리고 다음 모션으로 블렌드
    private const float HOLSTER_CUTOFF = 0.9f;    // 넣기(역재생)는 90% 구간이면 팔이 거의 다 내려감
    private const float DRAW_CUTOFF = 0.9f;       // 꺼내기는 90% 구간이면 조준 자세 완성

    // 이동 중에는 걸음/달리기 모션이 끊겨 보이지 않도록 조금 더 이른 지점에서 이동 모션으로 연결
    private const float HOLSTER_CUTOFF_MOVING = 0.8f;
    private const float DRAW_CUTOFF_MOVING = 0.8f;

    /// <summary>
    /// Draw 클립의 유효 구간 대기. 이른 지점(movingCutoff)까지 기다린 뒤,
    /// 그 시점에 이동 중이면 바로 끝내고 정지 상태면 나머지(idleCutoff까지)를 마저 기다림
    /// → 애니메이션 도중에 움직이기 시작한 경우도 이르게 연결됨
    /// </summary>
    private IEnumerator WaitDrawSegment(float movingCutoff, float idleCutoff)
    {
        yield return new WaitForSeconds(drawClipLength * movingCutoff / drawSpeedMultiplier);

        if (!isMoving)
            yield return new WaitForSeconds(drawClipLength * (idleCutoff - movingCutoff) / drawSpeedMultiplier);
    }

    // 장비 넣기/꺼내기 동안 카메라가 총구(FirePoint) 대신 바라볼 임시 타깃
    private CinemachineStateDrivenCamera stateCam;
    private readonly Dictionary<CinemachineVirtualCameraBase, Transform> originalLookAts
        = new Dictionary<CinemachineVirtualCameraBase, Transform>();
    private Transform flashLookTarget;

    // 전환 중 카메라 걸음 보빙 : 원래는 총구가 걷기 애니메이션을 타며 화면이 흔들리는데,
    // 전환 중엔 고정 타깃을 바라봐서 이동 중 화면 흔들림이 뚝 멈추는 위화감이 생김 → 절차적으로 이어줌
    [SerializeField] private float equipBobAmount = 0.035f;
    [SerializeField] private float equipBobWalkSpeed = 7f;
    [SerializeField] private float equipBobSprintSpeed = 11f;
    private Vector3 equipLookBaseLocalPos;
    private float equipBobPhase;
    private float equipBobWeight;

    /// <summary>
    /// 장비 전환에 필요한 참조 초기화 (Awake에서 호출)
    /// </summary>
    private void InitEquip()
    {
        // 총 + 팔 렌더러 수집 : 팔 리그 아래 전체 (손전등 쪽은 제외) → 총을 넣으면 팔도 같이 사라짐
        foreach (Renderer r in anim.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.transform.IsChildOf(flashLight.transform))
                gunRenderers.Add(r);
        }

        // 장비 넣기/꺼내기 동안 카메라가 바라볼 임시 타깃.
        // Arms 하위라 마우스 이동은 따라가지만, 전환 시작 시 총구 위치로 재배치되어 시선이 튀지 않음
        flashLookTarget = new GameObject("EquipLookTarget").transform;
        flashLookTarget.SetParent(anim.transform, false);
        flashLookTarget.localPosition = Vector3.zero;

        // 장비 전환 애니메이션(Draw 2) 길이
        foreach (AnimationClip c in anim.runtimeAnimatorController.animationClips)
        {
            if (c.name == "Draw 2")
            {
                drawClipLength = c.length;
                break;
            }
        }
    }

    private void Start()
    {
        // 씬의 State-Driven Camera와 자식 vcam들의 원래 LookAt(총구 추적) 저장
        stateCam = FindObjectOfType<CinemachineStateDrivenCamera>();
        if (stateCam != null)
        {
            foreach (CinemachineVirtualCameraBase child in stateCam.ChildCameras)
                originalLookAts[child] = child.LookAt;
        }
    }

    /// <summary>
    /// 카메라의 총구(FirePoint) 추적을 켜고 끔.
    /// 장비 넣기/꺼내기 애니메이션 동안에만 끊어서 화면이 총구를 따라 내려가는 것을 막고,
    /// 그 외에는 항상 원래 추적을 유지해 카메라/비네트 동작이 기존과 동일하게 동작함
    /// </summary>
    private void SetCameraGunTracking(bool track)
    {
        if (stateCam == null) return;

        if (!track)
        {
            // 교체 순간 시선이 튀지 않도록, 가짜 타깃을 현재 총구 위치에 정확히 맞춘 뒤 교체.
            // 타깃은 Arms 하위라 이후 마우스 이동은 그대로 따라가고, 넣기/꺼내기 모션만 무시됨
            foreach (Transform t in originalLookAts.Values)
            {
                if (t != null)
                {
                    flashLookTarget.position = t.position;
                    break;
                }
            }

            // 걸음 보빙 기준점/위상 초기화
            equipLookBaseLocalPos = flashLookTarget.localPosition;
            equipBobPhase = 0f;
            equipBobWeight = 0f;
        }

        foreach (KeyValuePair<CinemachineVirtualCameraBase, Transform> kv in originalLookAts)
            kv.Key.LookAt = track ? kv.Value : flashLookTarget;
    }

    /// <summary>
    /// 전환 중 이동하고 있으면 카메라 타깃에 걸음 보빙을 절차적으로 적용.
    /// 총 들고 걸을 때 총구가 만들던 화면 흔들림이 전환 중에도 끊기지 않고 이어짐
    /// </summary>
    private void LateUpdate()
    {
        if (!isSwitchingEquip || flashLookTarget == null || stateCam == null) return;

        float targetWeight = isMoving && grounded ? 1f : 0f;
        equipBobWeight = Mathf.MoveTowards(equipBobWeight, targetWeight, Time.deltaTime * 4f);

        if (equipBobWeight <= 0f)
        {
            flashLookTarget.localPosition = equipLookBaseLocalPos;
            return;
        }

        equipBobPhase += Time.deltaTime * (isSprint ? equipBobSprintSpeed : equipBobWalkSpeed);

        // Arms 로컬 기준 +Y = 화면 위, +Z = 화면 오른쪽.
        // 상하는 스텝(2배 주기), 좌우는 무게중심 이동(1배 주기)처럼 흔들림
        Vector3 bob = new Vector3(
            0f,
            Mathf.Sin(equipBobPhase * 2f) * equipBobAmount,
            Mathf.Sin(equipBobPhase) * equipBobAmount * 0.6f) * equipBobWeight;

        flashLookTarget.localPosition = equipLookBaseLocalPos + bob;
    }

    private void ChangeEquip(Define.EquipType equip)
    {
        if (playerShot.nowReload) return;   // 장전 중에는 전환 불가

        // 전환 중 입력은 버리지 않고 기억해 뒀다가 현재 전환이 끝나는 즉시 이어서 실행
        if (isSwitchingEquip)
        {
            pendingEquip = equip;
            return;
        }

        if (currentEquip == equip) return;

        StartCoroutine(EquipRoutine(equip));
    }

    /// <summary>
    /// 장비 전환 : 현재 장비를 집어넣고(총은 Draw 2 역재생) 새 장비를 꺼냄(총은 Draw 2 정재생)
    /// </summary>
    private IEnumerator EquipRoutine(Define.EquipType equip)
    {
        isSwitchingEquip = true;
        CancelEquipmentInspection();
        isAim = false;
        isFire = false;
        flashlightController.StopShake();

        // 남아있는 트리거가 Draw 재생을 끊지 않도록 리셋
        anim.ResetTrigger(PlayerAnimParameter.Shot);
        anim.ResetTrigger(PlayerAnimParameter.Reload);

        // 넣기/꺼내기 애니메이션 동안만 총구 추적을 끊음 (화면이 총구를 따라 내려가는 것 방지)
        SetCameraGunTracking(false);

        // 1. 현재 장비 집어넣기
        if (currentEquip == Define.EquipType.Gun)
        {
            if (PlayDraw(reverse: true))
            {
                // 끝 프레임에서 얼어붙지 않도록 유효 구간까지만 기다림 (이동 중이면 더 이르게 연결)
                yield return WaitDrawSegment(HOLSTER_CUTOFF_MOVING, HOLSTER_CUTOFF);
            }
            SetGunVisible(false);

            // 팔은 숨겨졌으니 Base Layer를 현재 이동 상태(달리기/걷기/Idle)로 복귀시킴.
            // 보이지 않는 총구(FirePoint)가 이동 애니메이션을 그대로 타므로
            // 손전등 상태에서도 총 들었을 때와 동일한 카메라 흔들림이 유지됨
            CrossFadeToLocomotion(isMoving ? 0.3f : 0.2f);
        }
        else if (currentEquip == Define.EquipType.Flashlight)
        {
            // 꺼내기의 역재생(HolsterRoutine)으로 집어넣는 연출 → 모션이 읽힐 시간을 준 뒤 다음 장비를 꺼냄
            flashlightController.SetHeld(false);
            yield return new WaitForSeconds(0.2f);
        }

        currentEquip = equip;

        // 2. 새 장비 꺼내기
        if (equip == Define.EquipType.Gun)
        {
            bool played = PlayDraw(reverse: false);

            // 애니메이터 포즈가 Draw 시작 프레임으로 갱신된 뒤에 표시해야
            // 총 든 자세가 한 프레임 비쳐 보이는 잔상이 없음
            if (played)
                yield return null;

            SetGunVisible(true);

            if (played)
            {
                // 끝까지 기다리지 않고 현재 이동 모션으로 바로 블렌드 (이동 중이면 더 이르게, 더 길게 섞음)
                yield return WaitDrawSegment(DRAW_CUTOFF_MOVING, DRAW_CUTOFF);
                CrossFadeToLocomotion(isMoving ? 0.3f : 0.15f);
            }
        }
        else if (equip == Define.EquipType.Flashlight)
        {
            // 빈 화면 구간이 없도록 손전등 꺼내기 연출을 즉시 시작하고,
            // 총구가 제 위치로 돌아오는 카메라 안정화 대기는 연출과 겹쳐서 진행
            flashlightController.SetHeld(true);
            yield return new WaitForSeconds(0.15f);
        }

        // 전환이 끝나면 항상 원래의 총구 추적으로 복원 → 카메라/비네트 동작이 총 들었을 때와 동일
        SetCameraGunTracking(true);

        isSwitchingEquip = false;

        // 전환 중 들어온 입력이 있으면 바로 이어서 처리
        if (pendingEquip.HasValue)
        {
            Define.EquipType next = pendingEquip.Value;
            pendingEquip = null;

            if (next != currentEquip && !playerShot.nowReload)
                StartCoroutine(EquipRoutine(next));
        }
    }

    /// <summary>
    /// Draw 2 애니메이션 재생. reverse면 끝에서부터 역재생(집어넣기). 상태가 없으면 false.
    /// Base Layer(0)에만 재생함 : State Layer는 State-Driven Camera의 상태 전환용이라
    /// 건드리면 달리기 카메라(흔들림/PP) 전환이 멈춤
    /// </summary>
    private bool PlayDraw(bool reverse)
    {
        if (!anim.HasState(0, PlayerAnimParameter.Draw))
            return false;

        anim.SetFloat(PlayerAnimParameter.DrawSpeed,
            (reverse ? -1f : 1f) * drawSpeedMultiplier);

        if (reverse)
        {
            // 넣기 : 현재(총 든) 포즈에서 역재생 진입.
            // 이동 중에는 팔이 흔들리는 걷기/달리기 포즈에서 넘어오므로 블렌드를 길게 잡아 부드럽게 연결.
            // Draw 상태의 실제 길이는 DrawSpeed 배율만큼 줄어들므로,
            // 시작점(normalized 0.97)을 속도 보정된 초 단위로 환산해서 지정한다
            float blend = isMoving ? 0.25f : 0.1f;
            float stateLength = drawClipLength / drawSpeedMultiplier;
            anim.CrossFadeInFixedTime(PlayerAnimParameter.Draw, blend, 0, 0.97f * stateLength);
        }
        else
        {
            // 꺼내기 : 팔이 숨겨진 상태에서 시작하므로 블렌드 없이 시작 프레임(손이 화면 밖)으로 스냅.
            // 블렌드를 쓰면 이전 포즈(총 든 자세)가 섞여 한순간 비쳐 보임
            anim.Play(PlayerAnimParameter.Draw, 0, 0f);
        }

        return true;
    }

    /// <summary>
    /// 현재 이동 상태(달리기/걷기/정지)에 맞는 로코모션 상태로 직접 블렌드.
    /// Idle을 거쳐가는 이중 전이 없이 장비 전환 후 모션이 자연스럽게 이어짐
    /// </summary>
    private void CrossFadeToLocomotion(float duration)
    {
        int target = PlayerAnimParameter.Idle;

        if (isSprint && anim.HasState(0, PlayerAnimParameter.Sprint))
            target = PlayerAnimParameter.Sprint;
        else if (isMoving && anim.HasState(0, PlayerAnimParameter.WalkState))
            target = PlayerAnimParameter.WalkState;

        if (anim.HasState(0, target))
            anim.CrossFadeInFixedTime(target, duration, 0);
    }

    private void SetGunVisible(bool visible)
    {
        foreach (Renderer r in gunRenderers)
        {
            if (r != null)
                r.enabled = visible;
        }
    }
}
