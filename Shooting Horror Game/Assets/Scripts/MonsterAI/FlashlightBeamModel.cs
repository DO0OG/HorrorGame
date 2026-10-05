using UnityEngine;

/// <summary>
/// Pure flashlight math shared by the player light and Hollow exposure checks.
/// </summary>
public static class FlashlightBeamModel
{
    public static float GetBatteryBrightness(float batteryRate, float minimumPoweredBrightness)
    {
        float rate = Mathf.Clamp01(batteryRate);
        if (rate <= 0f)
            return 0f;

        // A quadratic curve remains gradual but is visible even with HDR/tonemapping.
        return Mathf.Lerp(
            Mathf.Clamp01(minimumPoweredBrightness),
            1f,
            rate * rate);
    }

    public static float GetSpotAngle(float wideAngle, float focusedAngle, float focus)
    {
        float min = Mathf.Min(wideAngle, focusedAngle);
        float max = Mathf.Max(wideAngle, focusedAngle);
        return Mathf.Lerp(max, min, Mathf.Clamp01(focus));
    }

    public static float ApplyScrollFocus(float currentFocus, float scrollDelta, float stepPerScroll)
    {
        if (Mathf.Abs(scrollDelta) < 0.01f)
            return Mathf.Clamp01(currentFocus);

        return Mathf.Clamp01(
            currentFocus + Mathf.Sign(scrollDelta) * Mathf.Max(0f, stepPerScroll));
    }

    public static bool IsInsideFocusedBeam(
        Vector3 source,
        Vector3 forward,
        Vector3 target,
        float range,
        float spotAngle,
        float focus,
        float requiredFocus)
    {
        if (focus < requiredFocus || range <= 0f)
            return false;

        Vector3 direction = target - source;
        float distance = direction.magnitude;
        if (distance <= 0.001f || distance > range)
            return false;

        return Vector3.Angle(forward, direction / distance) <= spotAngle * 0.5f;
    }
}
