using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손전등 : 켜져 있는 동안 배터리가 소모되고, 우클릭으로 흔들어서 충전함.
/// 흔들 때 나는 소리는 플레이어 하위 AudioSource에서 재생되므로
/// Normal 몬스터의 청각(DetectSounds)에 감지되어 추적당할 수 있음.
/// </summary>
[RequireComponent(typeof(Light))]
public class Flashlight_Controller : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float battery = 100f;
    [SerializeField] private float drainPerSecond = 1.5f;
    [SerializeField] private float chargePerShake = 4f;
    [SerializeField, Range(0, 1)] private float minimumPoweredBrightness = 0.05f;
    [SerializeField, Range(0.1f, 1f)] private float minimumPoweredRange = 0.35f;

    [Header("Beam Focus")]
    [SerializeField, Range(5f, 45f)] private float focusedSpotAngle = 14f;
    [SerializeField, Range(0.01f, 0.5f)] private float focusStepPerScroll = 0.12f;
    [SerializeField, Min(1f)] private float focusedIntensityBoost = 1.35f;
    [SerializeField, Min(1f)] private float focusedDrainMultiplier = 1.2f;
    [SerializeField, Range(0f, 1f)] private float hollowFocusThreshold = 0.8f;
    [SerializeField, Range(0.02f, 0.5f)] private float focusedHitRadius = 0.16f;
    [SerializeField] private string focusedBeamHit;

    [Header("Shake")]
    [SerializeField] private float shakeInterval = 0.4f;
    [SerializeField, Range(0, 1)] private float shakeSoundVolume = 1f;    // 몬스터 청각 기준(0.6)보다 크게
    [SerializeField] private float shakeBobbingSpeed = 18f;               // 가로 지터 노이즈 속도
    [SerializeField] private float shakeBobbingAmount = 0.05f;            // 모델 위치 흔들림 진폭 (작게)
    [SerializeField] private float shakePumpSpeed = 28f;                  // 위아래 펌핑 속도 (rad/s)
    [SerializeField] private float shakeAngle = 8f;                       // 빔 각도 흔들림(도) — 빛의 출렁임 크기
    [SerializeField, Range(0, 1)] private float clickSoundVolume = 0.35f;

    [Header("Draw")]
    [SerializeField] private float drawDuration = 0.35f;
    [SerializeField] private Vector3 drawOffset = new Vector3(0.18f, -0.4f, -0.12f);   // 시작 위치 오프셋 (오른쪽 아래 대각선)
    [SerializeField] private Vector3 drawEuler = new Vector3(40f, 25f, -30f);          // 시작 회전 오프셋

    [Header("Battery Inspection")]
    [SerializeField] private Vector3 inspectionOffset = new Vector3(-0.08f, 0.07f, 0.1f);
    [SerializeField] private Vector3 inspectionEuler = new Vector3(-18f, 28f, 12f);
    [SerializeField, Min(1f)] private float inspectionBlendSpeed = 8f;
    [SerializeField, Range(0.25f, 0.85f)] private float inspectionGaugeLengthRatio = 0.58f;
    [SerializeField, Range(0.15f, 0.7f)] private float inspectionGaugeWidthRatio = 0.42f;
    [SerializeField, Min(0.0001f)] private float inspectionGaugeSurfaceOffset = 0.003f;
    [SerializeField, Min(1f)] private float inspectionGaugeFadeSpeed = 7f;

    private Light lightSource;
    private AudioSource audioSource;
    private float defaultIntensity;
    private float defaultRange;
    private float wideSpotAngle;
    [SerializeField, Range(0f, 1f)] private float beamFocus;
    private Coroutine shakeRoutine;
    private Coroutine drawRoutine;
    private AudioClip clickClip;   // 반복 재생되는 클립은 경로 조회 없이 캐싱해서 사용
    private AudioClip shakeClip;
    private readonly RaycastHit[] focusedBeamHits = new RaycastHit[24];
    private float inspectionBlend;
    private CanvasGroup batteryInspectionCanvas;
    private Image batteryFillImage;
    private Material batteryGaugeMaterial;
    private bool batteryInspectionVisible;

    public bool IsDrawing { get; private set; }
    private bool isHeld;                      // 현재 손에 들고 있는지 (초기화 시 연출 없이 숨기기 위한 상태 추적)
    private Transform model;                  // 손에 든 손전등 모델 (Flashlight_Model)
    private Vector3 originalLocalPosition;    // 흔들기 복귀용 원위치
    private Quaternion originalLocalRotation; // 흔들기 복귀용 원회전

    public bool IsOn => lightSource != null && lightSource.enabled;
    public bool IsShaking { get; private set; }
    public bool IsInspecting { get; private set; }
    public float BatteryRate => maxBattery > 0f ? battery / maxBattery : 0f;
    public float BeamFocus => beamFocus;
    public bool IsFocusedForHollow => beamFocus >= hollowFocusThreshold;
    public float CurrentIntensity => lightSource != null ? lightSource.intensity : 0f;

    private void Awake()
    {
        lightSource = GetComponent<Light>();
        defaultIntensity = lightSource.intensity;
        defaultRange = lightSource.range;
        wideSpotAngle = lightSource.spotAngle;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        battery = maxBattery;
        lightSource.enabled = false;
        beamFocus = 0f;
        UpdateBeamShape();

        model = transform.Find("Flashlight_Model");
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        UpdateBatteryInspectionGauge();
        UpdateShakeMotion();

        if (!IsOn) return;

        // 흔드는 중에도 켜져 있으면 소모되지만, 충전량(chargePerShake)이 더 커서 배터리는 차오름 (DECAYED 방식)
        float drainMultiplier = Mathf.Lerp(1f, focusedDrainMultiplier, beamFocus);
        battery = Mathf.Clamp(
            battery - drainPerSecond * drainMultiplier * Time.deltaTime,
            0f,
            maxBattery);
        UpdateIntensity();
        UpdateFocusedHollowExposure();

        if (battery <= 0f)
            lightSource.enabled = false;
    }

    /// <summary>
    /// 배터리 잔량이 줄어드는 전체 구간에서 연속적으로 어두워진다.
    /// </summary>
    private void UpdateIntensity()
    {
        float batteryBrightness = FlashlightBeamModel.GetBatteryBrightness(
            BatteryRate, minimumPoweredBrightness);
        float focusBoost = Mathf.Lerp(1f, focusedIntensityBoost, beamFocus);
        lightSource.intensity = defaultIntensity * batteryBrightness * focusBoost;
        lightSource.range = defaultRange * Mathf.Lerp(
            minimumPoweredRange, 1f, BatteryRate);
    }

    public void AdjustFocus(float scrollDelta)
    {
        if (Mathf.Abs(scrollDelta) < 0.01f)
            return;

        beamFocus = FlashlightBeamModel.ApplyScrollFocus(
            beamFocus, scrollDelta, focusStepPerScroll);
        UpdateBeamShape();
        if (IsOn)
            UpdateIntensity();
    }

    private void UpdateBeamShape()
    {
        if (lightSource == null || lightSource.type != LightType.Spot)
            return;

        lightSource.spotAngle = FlashlightBeamModel.GetSpotAngle(
            wideSpotAngle, focusedSpotAngle, beamFocus);
        lightSource.innerSpotAngle = Mathf.Lerp(
            wideSpotAngle * 0.65f,
            focusedSpotAngle * 0.8f,
            beamFocus);
    }

    private void UpdateFocusedHollowExposure()
    {
        focusedBeamHit = string.Empty;
        if (!IsFocusedForHollow || lightSource.range <= 0f)
            return;

        int hitCount = Physics.SphereCastNonAlloc(
            transform.position,
            focusedHitRadius,
            transform.forward,
            focusedBeamHits,
            lightSource.range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        RaycastHit nearest = default;
        bool found = false;
        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = focusedBeamHits[i].transform;
            if (hitTransform == null || hitTransform.IsChildOf(transform.root))
                continue;

            if (!found || focusedBeamHits[i].distance < nearest.distance)
            {
                nearest = focusedBeamHits[i];
                found = true;
            }
        }

        if (!found)
            return;

        focusedBeamHit = nearest.transform.name;
        Monster_Controller monster = nearest.collider.GetComponentInParent<Monster_Controller>();
        if (monster != null)
            monster.ReceiveFocusedFlashlight(Time.deltaTime);
    }

    public void Toggle()
    {
        if (!IsOn && battery <= 0f) return;   // 배터리가 없으면 켜지지 않음

        lightSource.enabled = !lightSource.enabled;
        if (lightSource.enabled)
            UpdateIntensity();

        if (clickClip == null)
            clickClip = Managers.Resource.LoadAudioClip("Player/FlashlightClick");

        if (clickClip != null)
        {
            audioSource.volume = clickSoundVolume;
            audioSource.PlayOneShot(clickClip);
        }
    }

    /// <summary>
    /// 장비 전환 시 호출. 들고 있지 않으면 강제로 끄고 흔들기 중단
    /// </summary>
    public void SetHeld(bool held)
    {
        bool wasHeld = isHeld;
        isHeld = held;

        // 진행 중이던 꺼내기/넣기 연출 정리
        if (drawRoutine != null)
        {
            StopCoroutine(drawRoutine);
            drawRoutine = null;
        }
        IsDrawing = false;

        if (held)
        {
            if (model != null)
                model.gameObject.SetActive(true);

            // 오른쪽 아래 대각선에서 회전하며 제 위치로 올라오는 꺼내기 연출
            drawRoutine = StartCoroutine(DrawRoutine());
        }
        else
        {
            SetInspecting(false);
            SetBatteryInspectionVisible(false);
            if (batteryInspectionCanvas != null)
                batteryInspectionCanvas.alpha = 0f;
            StopShake();
            lightSource.enabled = false;

            if (wasHeld && model != null && model.gameObject.activeSelf)
            {
                // 들고 있던 상태에서 넣는 경우 : 꺼내기의 역재생으로 내려간 뒤 숨김
                drawRoutine = StartCoroutine(HolsterRoutine());
            }
            else
            {
                // 초기화 등 연출이 필요 없는 경우 : 즉시 숨기고 원위치 복구
                if (model != null)
                    model.gameObject.SetActive(false);
                transform.localPosition = originalLocalPosition;
                transform.localRotation = originalLocalRotation;
            }
        }
    }

    /// <summary>
    /// 손전등 꺼내기 연출 : 대각선 아래에서 기울어진 채 올라와 제자리에 안착
    /// </summary>
    private IEnumerator DrawRoutine()
    {
        IsDrawing = true;

        Vector3 startPos = originalLocalPosition + originalLocalRotation * drawOffset;
        Quaternion startRot = originalLocalRotation * Quaternion.Euler(drawEuler);

        float t = 0f;
        while (t < drawDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / drawDuration));

            transform.localPosition = Vector3.Lerp(startPos, originalLocalPosition, k);
            transform.localRotation = Quaternion.Slerp(startRot, originalLocalRotation, k);

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        IsDrawing = false;
        drawRoutine = null;
    }

    /// <summary>
    /// 손전등 집어넣기 연출 : 꺼내기의 역재생. 현재 위치에서 대각선 아래로 내려가며 기울어진 뒤 숨김
    /// </summary>
    private IEnumerator HolsterRoutine()
    {
        IsDrawing = true;

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 endPos = originalLocalPosition + originalLocalRotation * drawOffset;
        Quaternion endRot = originalLocalRotation * Quaternion.Euler(drawEuler);

        float t = 0f;
        while (t < drawDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / drawDuration));

            transform.localPosition = Vector3.Lerp(startPos, endPos, k);
            transform.localRotation = Quaternion.Slerp(startRot, endRot, k);

            yield return null;
        }

        if (model != null)
            model.gameObject.SetActive(false);

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        IsDrawing = false;
        drawRoutine = null;
    }

    public void StartShake()
    {
        if (IsInspecting || IsShaking) return;
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
        IsShaking = false;
    }

    /// <summary>
    /// 흔들어 충전하는 동안 손전등을 Perlin 노이즈로 흔드는 시각 연출 (DECAYED에서 가져옴)
    /// </summary>
    private void UpdateShakeMotion()
    {
        // 꺼내기 연출 중에는 DrawRoutine이 트랜스폼을 제어함
        if (IsDrawing) return;

        inspectionBlend = Mathf.MoveTowards(
            inspectionBlend,
            IsInspecting ? 1f : 0f,
            Time.deltaTime * inspectionBlendSpeed);

        Vector3 inspectionPosition = originalLocalPosition +
            originalLocalRotation * (inspectionOffset * inspectionBlend);
        Quaternion inspectionRotation = originalLocalRotation *
            Quaternion.Slerp(
                Quaternion.identity,
                Quaternion.Euler(inspectionEuler),
                inspectionBlend);

        if (IsShaking)
        {
            // 세로는 사인파 펌핑(흔드는 손동작), 가로는 노이즈 잔떨림
            float pump = Mathf.Sin(Time.time * shakePumpSpeed);
            float jitter = Mathf.PerlinNoise(0f, Time.time * shakeBobbingSpeed) * 2f - 1f;

            // 빛(빔 각도)은 크게 출렁이고 — 벽에 비치는 원뿔이 크게 흔들림
            transform.localRotation = inspectionRotation
                * Quaternion.Euler(-pump * shakeAngle, jitter * shakeAngle * 0.5f, 0f);

            // 모델은 제자리에서 작게만 움직임
            Vector3 offset = inspectionRotation
                * new Vector3(jitter * shakeBobbingAmount * 0.5f, pump * shakeBobbingAmount, 0f);
            transform.localPosition = inspectionPosition + offset;
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, inspectionPosition,
                Time.deltaTime * shakeBobbingSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, inspectionRotation,
                Time.deltaTime * shakeBobbingSpeed);
        }
    }

    public void SetInspecting(bool inspecting)
    {
        if (!isHeld)
            inspecting = false;

        IsInspecting = inspecting;
        if (inspecting)
        {
            StopShake();
        }
        else
            SetBatteryInspectionVisible(false);
    }

    public void InitializeBatteryInspection(Material ammoGaugeMaterialTemplate)
    {
        if (batteryInspectionCanvas != null)
            return;

        if (model == null)
            model = transform.Find("Flashlight_Model");

        GameObject gaugeObject = new GameObject(
            "BatteryCheck",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup));
        Transform gaugeParent = model != null ? model : transform;
        gaugeObject.transform.SetParent(gaugeParent, false);

        RectTransform gaugeTransform = gaugeObject.GetComponent<RectTransform>();
        gaugeTransform.sizeDelta = new Vector2(100f, 20f);
        PlaceGaugeOnFlashlight(gaugeTransform, gaugeParent);

        Canvas canvas = gaugeObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 6;

        batteryInspectionCanvas = gaugeObject.GetComponent<CanvasGroup>();

        Image background = CreateGaugeImage(
            "Background",
            gaugeTransform,
            new Color(0.018f, 0.02f, 0.018f, 0.88f));
        StretchGaugeImage(background.rectTransform, Vector2.zero, Vector2.zero);

        GameObject fillTrackObject = new GameObject("Fill Track", typeof(RectTransform));
        fillTrackObject.transform.SetParent(gaugeTransform, false);
        RectTransform fillTrack = fillTrackObject.GetComponent<RectTransform>();
        StretchGaugeImage(
            fillTrack,
            new Vector2(4f, 4f),
            new Vector2(-4f, -4f));

        batteryFillImage = CreateGaugeImage(
            "Fill",
            fillTrack,
            DiegeticGaugeUtility.BatteryColor);
        if (ammoGaugeMaterialTemplate != null)
            batteryFillImage.material = ammoGaugeMaterialTemplate;
        StretchGaugeImage(
            batteryFillImage.rectTransform,
            Vector2.zero,
            Vector2.zero);

        batteryInspectionCanvas.alpha = 0f;
        batteryGaugeMaterial = DiegeticGaugeUtility.Configure(
            batteryInspectionCanvas,
            batteryFillImage,
            DiegeticGaugeUtility.BatteryColor,
            true);
        DiegeticGaugeUtility.MakeTwoSided(batteryGaugeMaterial);
        DiegeticGaugeUtility.SetLevel(
            batteryGaugeMaterial,
            batteryFillImage,
            BatteryRate,
            DiegeticGaugeUtility.BatteryColor,
            false);
        UpdateBatteryFillWidth();
    }

    private void PlaceGaugeOnFlashlight(RectTransform gaugeTransform, Transform gaugeParent)
    {
        MeshFilter meshFilter = gaugeParent.GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            gaugeTransform.localPosition = new Vector3(0f, 0.035f, -0.09f);
            gaugeTransform.localRotation = Quaternion.Euler(0f, 180f, 68f);
            gaugeTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            return;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 longAxis = GetLongestBoundsAxis(bounds.size);
        float longExtent = GetAxisValue(bounds.extents, longAxis);

        Vector3 localFlashlightUp =
            gaugeParent.InverseTransformDirection(transform.up).normalized;
        Vector3 surfaceDirection =
            localFlashlightUp - longAxis * Vector3.Dot(localFlashlightUp, longAxis);
        if (surfaceDirection.sqrMagnitude < 0.000001f)
            surfaceDirection = Vector3.back;
        surfaceDirection.Normalize();

        Vector3 absoluteSurfaceDirection = new Vector3(
            Mathf.Abs(surfaceDirection.x),
            Mathf.Abs(surfaceDirection.y),
            Mathf.Abs(surfaceDirection.z));
        float surfaceDistance = Vector3.Dot(absoluteSurfaceDirection, bounds.extents);

        gaugeTransform.localPosition =
            bounds.center +
            surfaceDirection * (surfaceDistance + inspectionGaugeSurfaceOffset);

        Vector3 gaugeUp = Vector3.Cross(surfaceDirection, longAxis).normalized;
        gaugeTransform.localRotation = Quaternion.LookRotation(surfaceDirection, gaugeUp);

        float gaugeLength = longExtent * 2f * inspectionGaugeLengthRatio;
        float crossSectionSize = GetSmallestCrossSection(bounds.size, longAxis);
        float gaugeWidth = crossSectionSize * inspectionGaugeWidthRatio;
        gaugeTransform.localScale = new Vector3(
            gaugeLength / gaugeTransform.sizeDelta.x,
            gaugeWidth / gaugeTransform.sizeDelta.y,
            1f);
    }

    private static Vector3 GetLongestBoundsAxis(Vector3 size)
    {
        if (size.x >= size.y && size.x >= size.z)
            return Vector3.right;
        if (size.y >= size.x && size.y >= size.z)
            return Vector3.up;
        return Vector3.forward;
    }

    private static float GetAxisValue(Vector3 value, Vector3 axis)
    {
        if (axis == Vector3.right)
            return value.x;
        if (axis == Vector3.up)
            return value.y;
        return value.z;
    }

    private static float GetSmallestCrossSection(Vector3 size, Vector3 longAxis)
    {
        if (longAxis == Vector3.right)
            return Mathf.Min(size.y, size.z);
        if (longAxis == Vector3.up)
            return Mathf.Min(size.x, size.z);
        return Mathf.Min(size.x, size.y);
    }

    private static Image CreateGaugeImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void StretchGaugeImage(
        RectTransform rect,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public void SetBatteryInspectionVisible(bool visible)
    {
        batteryInspectionVisible = visible && isHeld;
    }

    private void UpdateBatteryInspectionGauge()
    {
        if (batteryInspectionCanvas == null || batteryFillImage == null)
            return;

        DiegeticGaugeUtility.Fade(
            batteryInspectionCanvas,
            batteryInspectionVisible,
            inspectionGaugeFadeSpeed,
            Time.deltaTime);
        DiegeticGaugeUtility.SetLevel(
            batteryGaugeMaterial,
            batteryFillImage,
            BatteryRate,
            DiegeticGaugeUtility.BatteryColor,
            batteryInspectionVisible);
        UpdateBatteryFillWidth();
    }

    private void UpdateBatteryFillWidth()
    {
        if (batteryFillImage == null)
            return;

        float rate = BatteryRate;
        RectTransform fillRect = batteryFillImage.rectTransform;
        fillRect.anchorMax = new Vector2(Mathf.Max(0.001f, rate), 1f);
        batteryFillImage.enabled = rate > 0.001f;
    }

    private void OnDestroy()
    {
        if (batteryGaugeMaterial != null)
            Destroy(batteryGaugeMaterial);
    }

    private IEnumerator ShakeRoutine()
    {
        // DECAYED와 동일하게 켜진 채로 흔들면서 충전
        IsShaking = true;

        WaitForSeconds wait = new WaitForSeconds(shakeInterval);

        while (true)
        {
            battery = Mathf.Clamp(battery + chargePerShake, 0f, maxBattery);
            if (IsOn)
                UpdateIntensity();

            // 흔들기 소리 재생 → Normal 몬스터가 들을 수 있음
            if (shakeClip == null)
                shakeClip = Managers.Resource.LoadAudioClip("Player/FlashlightShake");

            audioSource.volume = shakeSoundVolume;
            if (shakeClip != null)
                audioSource.PlayOneShot(shakeClip);

            yield return wait;
        }
    }
}
