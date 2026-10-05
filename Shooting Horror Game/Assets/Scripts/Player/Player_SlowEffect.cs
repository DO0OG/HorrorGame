using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hollow 접촉으로 슬로우가 걸린 동안의 화면 효과.
///
/// 포스트프로세싱은 일부러 쓰지 않는다. 이 프로젝트는 Main Camera의 Render Post Processing이
/// 꺼진 상태를 기준으로 룩이 잡혀 있어서, 런타임에 켜면 렌더 경로가 바뀌며 평상시 화면 톤까지
/// 달라지고 효과가 켜지고 꺼질 때 하드컷이 생긴다.
///
/// 대신 두 가지를 쓴다.
///  1) Cinemachine 가상 카메라의 렌즈 : FOV를 좁혀 확대, Dutch로 좌우 기울임
///  2) 화면 오버레이 캔버스 : 전체 어두워짐 + 비네트 + 일렁이는 안개
///
/// 둘 다 렌더 설정을 바꾸지 않으므로 평상시 화면은 100% 그대로다.
///
/// Camera.main의 FOV는 Cinemachine 브레인이 매 프레임 덮어쓰기 때문에 직접 쓰면 안 되고,
/// 반드시 가상 카메라 쪽 렌즈를 수정해야 한다. 무기용 오버레이 카메라는 FOV.cs가 Main Camera를
/// 따라가도록 되어 있어서 자동으로 같이 움직인다.
/// </summary>
public class Player_SlowEffect : MonoBehaviour
{
    [Header("Zoom")]
    // 1보다 작으면 시야각이 좁아져 화면이 확대된다. vcam마다 기본 FOV가 달라서(30/60/70)
    // 절대값이 아니라 배율로 적용해야 어느 카메라가 활성이든 같은 비율로 확대된다.
    [SerializeField, Range(0.5f, 1f)] private float zoomScale = 0.82f;

    [Header("Sway")]
    [SerializeField] private float maxDutch = 5f;           // 화면이 좌우로 기우는 최대 각도
    [SerializeField] private float swaySpeed = 0.4f;        // 기울어짐 왕복 주기 (Hz)
    [SerializeField] private float breathSpeed = 0.9f;      // 확대가 미세하게 들썩이는 주기 (Hz)
    [SerializeField, Range(0f, 1f)] private float breathAmount = 0.25f;

    [Header("Overlay")]
    [SerializeField, Range(0f, 1f)] private float darkenAmount = 0.22f;     // 화면 전체가 어두워지는 정도
    [SerializeField, Range(0f, 1f)] private float vignetteAmount = 0.55f;   // 가장자리가 조여드는 정도
    [SerializeField, Range(0f, 1f)] private float hazeAmount = 0.18f;       // 일렁이는 안개의 진하기
    [SerializeField] private float hazeScrollSpeed = 0.04f;
    [SerializeField] private float vignettePulseSpeed = 0.7f;               // 비네트가 조였다 풀리는 주기 (Hz)
    [SerializeField, Range(0f, 1f)] private float vignettePulseAmount = 0.3f;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.4f;
    [SerializeField] private float fadeOutTime = 1.2f;

    private CinemachineVirtualCamera[] vcams;
    private float[] baseFov;
    private float[] baseDutch;
    private Coroutine routine;

    private GameObject overlayRoot;
    private RawImage darkenImage;
    private RawImage vignetteImage;
    private RawImage hazeImage;
    private Texture2D solidTexture;
    private Texture2D vignetteTexture;
    private Texture2D hazeTexture;

    private const int VIGNETTE_SIZE = 128;
    private const int HAZE_SIZE = 128;
    private const float HAZE_TILING = 2f;

    private void Awake()
    {
        BuildOverlay();
    }

    private void Start()
    {
        // 상태에 따라 여러 vcam이 번갈아 활성화되므로, 전부에 같은 배율을 걸어야
        // 도중에 카메라가 바뀌어도 효과가 끊기지 않는다.
        vcams = FindObjectsOfType<CinemachineVirtualCamera>(true);

        if (vcams == null || vcams.Length == 0)
        {
            Debug.LogWarning("[Player_SlowEffect] CinemachineVirtualCamera를 찾지 못해 화면 확대 효과가 적용되지 않습니다.");
            return;
        }

        baseFov = new float[vcams.Length];
        baseDutch = new float[vcams.Length];

        for (int i = 0; i < vcams.Length; i++)
        {
            baseFov[i] = vcams[i].m_Lens.FieldOfView;
            baseDutch[i] = vcams[i].m_Lens.Dutch;
        }
    }

    private void OnDisable()
    {
        // 효과 도중 비활성화되어도 렌즈는 원래대로 돌려놓는다
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        Apply(0f, 0f);
    }

    private void OnDestroy()
    {
        if (solidTexture != null) Destroy(solidTexture);
        if (vignetteTexture != null) Destroy(vignetteTexture);
        if (hazeTexture != null) Destroy(hazeTexture);
    }

    /// <summary>
    /// 슬로우 지속시간 동안 화면 효과를 재생한다.
    /// </summary>
    /// <param name="duration">슬로우 지속시간</param>
    /// <param name="strength">0~1. 이동속도가 많이 깎일수록 1에 가까움</param>
    public void Play(float duration, float strength)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Routine(duration, Mathf.Clamp01(strength)));
    }

    public void StopEffect()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        Apply(0f, 0f);
    }

    #region Overlay 생성

    /// <summary>
    /// 화면 오버레이를 코드로 구성한다. 애셋이나 씬 세팅 없이 동작하도록
    /// 텍스처까지 런타임에 만든다. GraphicRaycaster를 붙이지 않고 raycastTarget도 꺼서
    /// 기존 UI의 입력을 절대 가로채지 않는다.
    /// </summary>
    private void BuildOverlay()
    {
        solidTexture = CreateSolidTexture();
        vignetteTexture = CreateVignetteTexture(VIGNETTE_SIZE);
        hazeTexture = CreateHazeTexture(HAZE_SIZE);

        overlayRoot = new GameObject("SlowEffect Overlay");
        overlayRoot.transform.SetParent(transform, false);

        Canvas canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;   // 씬의 기존 캔버스가 모두 0이라 그 위에 그려진다

        darkenImage = CreateLayer("Darken", solidTexture);
        hazeImage = CreateLayer("Haze", hazeTexture);
        vignetteImage = CreateLayer("Vignette", vignetteTexture);   // 마지막이 가장 위

        hazeImage.uvRect = new Rect(0f, 0f, HAZE_TILING, HAZE_TILING);

        overlayRoot.SetActive(false);
    }

    private RawImage CreateLayer(string name, Texture2D texture)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(overlayRoot.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = go.AddComponent<RawImage>();
        image.texture = texture;
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        return image;
    }

    private Texture2D CreateSolidTexture()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 가운데는 투명하고 가장자리로 갈수록 불투명해지는 원형 그라데이션.
    /// </summary>
    private Texture2D CreateVignetteTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        const float inner = 0.35f;   // 이 반경 안쪽은 완전히 투명
        Vector2 center = new Vector2(0.5f, 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);

                // 중심 0, 모서리 1이 되도록 정규화
                float distance = Vector2.Distance(uv, center) / 0.7071f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, 1f, distance));

                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// 일렁임에 쓸 부드러운 노이즈. 스크롤하면서 반복되므로 이음매가 보이지 않도록
    /// 네 방향으로 감아서 블렌딩한 타일러블 노이즈로 만든다.
    /// </summary>
    private Texture2D CreateHazeTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;

        const float scale = 3f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;

                float a = Mathf.PerlinNoise(u * scale, v * scale);
                float b = Mathf.PerlinNoise((u - 1f) * scale, v * scale);
                float c = Mathf.PerlinNoise(u * scale, (v - 1f) * scale);
                float d = Mathf.PerlinNoise((u - 1f) * scale, (v - 1f) * scale);

                float noise = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);

                texture.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, noise)));
            }
        }

        texture.Apply();
        return texture;
    }

    #endregion

    /// <summary>
    /// weight 0이면 원래 상태 그대로, 1이면 효과가 완전히 적용된 상태.
    /// </summary>
    private void Apply(float weight, float time)
    {
        ApplyLens(weight, time);
        ApplyOverlay(weight, time);
    }

    private void ApplyLens(float weight, float time)
    {
        if (vcams == null) return;

        // 확대가 살아 있는 것처럼 미세하게 들썩이게 함
        float breath = 1f - breathAmount * (0.5f - 0.5f * Mathf.Cos(time * breathSpeed * 2f * Mathf.PI));
        float zoom = Mathf.Lerp(1f, zoomScale, weight * breath);
        float dutch = maxDutch * weight * Mathf.Sin(time * swaySpeed * 2f * Mathf.PI);

        for (int i = 0; i < vcams.Length; i++)
        {
            if (vcams[i] == null) continue;

            vcams[i].m_Lens.FieldOfView = baseFov[i] * zoom;
            vcams[i].m_Lens.Dutch = baseDutch[i] + dutch;
        }
    }

    private void ApplyOverlay(float weight, float time)
    {
        if (overlayRoot == null) return;

        bool visible = weight > 0.001f;
        if (overlayRoot.activeSelf != visible)
            overlayRoot.SetActive(visible);   // 효과가 없을 때는 오버드로를 아예 없앤다

        if (!visible) return;

        darkenImage.color = new Color(0f, 0f, 0f, darkenAmount * weight);

        // 비네트는 주기적으로 조였다 풀리면서 숨쉬는 느낌을 준다
        float pulse = 1f - vignettePulseAmount * (0.5f - 0.5f * Mathf.Cos(time * vignettePulseSpeed * 2f * Mathf.PI));
        vignetteImage.color = new Color(0f, 0f, 0f, vignetteAmount * weight * pulse);
        vignetteImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, weight * (1f - pulse));

        // 안개를 서로 다른 속도로 흘려보내 일렁이는 느낌을 만든다
        hazeImage.color = new Color(0f, 0f, 0f, hazeAmount * weight);
        hazeImage.uvRect = new Rect(
            Mathf.Sin(time * 0.17f) * 0.25f + time * hazeScrollSpeed,
            Mathf.Cos(time * 0.11f) * 0.25f - time * hazeScrollSpeed * 0.6f,
            HAZE_TILING,
            HAZE_TILING);
    }

    /// <summary>
    /// SmoothStep으로 페이드해서 시작과 끝에서 툭 끊기는 느낌을 없앤다.
    /// </summary>
    private IEnumerator Routine(float duration, float strength)
    {
        float time = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            time += Time.deltaTime;
            Apply(Mathf.SmoothStep(0f, strength, elapsed / fadeInTime), time);
            yield return null;
        }

        float hold = Mathf.Max(0f, duration - fadeInTime - fadeOutTime);
        elapsed = 0f;
        while (elapsed < hold)
        {
            elapsed += Time.deltaTime;
            time += Time.deltaTime;
            Apply(strength, time);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            time += Time.deltaTime;
            Apply(Mathf.SmoothStep(strength, 0f, elapsed / fadeOutTime), time);
            yield return null;
        }

        Apply(0f, 0f);
        routine = null;
    }
}
