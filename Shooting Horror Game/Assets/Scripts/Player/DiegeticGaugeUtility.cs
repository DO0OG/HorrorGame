using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Styling and value updates for the existing equipment-mounted inspection gauge.
/// This does not create a screen HUD; it only restyles world-space canvases.
/// </summary>
public static class DiegeticGaugeUtility
{
    private static readonly int SpreadId = Shader.PropertyToID("_Spread");
    private static readonly int FilledColorId = Shader.PropertyToID("_Color_1");
    private static readonly int EmptyColorId = Shader.PropertyToID("_Color_2");
    private static readonly int TextureId = Shader.PropertyToID("_Texture2D");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    public static readonly Color AmmoColor = new Color(0.72f, 0.52f, 0.2f, 1f);
    public static readonly Color BatteryColor = new Color(0.4f, 0.68f, 0.46f, 1f);
    public static readonly Color WarningColor = new Color(0.9f, 0.2f, 0.08f, 1f);
    private static readonly Color EmptyColor = new Color(0.025f, 0.028f, 0.024f, 0.96f);
    private static readonly Color HousingColor = new Color(0.018f, 0.02f, 0.018f, 0.84f);

    public static Material Configure(
        CanvasGroup group,
        Image fill,
        Color accentColor,
        bool preserveSourceTexture)
    {
        if (group == null || fill == null)
            return null;

        group.interactable = false;
        group.blocksRaycasts = false;

        Canvas canvas = group.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 6;
        }

        Graphic[] graphics = group.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        Image[] images = group.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == fill || image.name != "Background")
                continue;

            image.color = HousingColor;
            break;
        }

        Material material = fill.material != null && fill.material.HasProperty(SpreadId)
            ? new Material(fill.material)
            : null;
        if (material != null)
        {
            if (!preserveSourceTexture && material.HasProperty(TextureId))
                material.SetTexture(TextureId, Texture2D.whiteTexture);
            fill.material = material;
        }

        fill.color = new Color(1f, 1f, 1f, 0.92f);
        SetLevel(material, fill, 1f, accentColor, false);
        return material;
    }

    public static void SetLevel(
        Material material,
        Image fill,
        float normalizedValue,
        Color normalColor,
        bool pulseWarning)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        Color activeColor = normalColor;

        if (normalizedValue <= 0.2f)
        {
            float pulse = pulseWarning
                ? Mathf.Sin(Time.unscaledTime * 7f) * 0.12f + 0.88f
                : 1f;
            activeColor = WarningColor * pulse;
            activeColor.a = 1f;
        }

        bool usesGradientMaterial = material != null && material.HasProperty(SpreadId);
        if (usesGradientMaterial)
        {
            material.SetFloat(SpreadId, Mathf.Lerp(0.1f, 1f, normalizedValue));
            if (material.HasProperty(FilledColorId))
                material.SetColor(FilledColorId, activeColor);
            if (material.HasProperty(EmptyColorId))
                material.SetColor(EmptyColorId, EmptyColor);
        }
        else if (fill != null)
        {
            fill.fillAmount = normalizedValue;
            fill.color = activeColor;
        }
    }

    public static void Fade(CanvasGroup group, bool visible, float speed, float deltaTime)
    {
        if (group == null)
            return;

        float target = visible ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, speed * deltaTime);
    }

    public static void MakeTwoSided(Material material)
    {
        if (material != null && material.HasProperty(CullId))
            material.SetFloat(CullId, 0f);
    }
}
