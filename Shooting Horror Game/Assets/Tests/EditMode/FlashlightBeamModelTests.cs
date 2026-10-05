using NUnit.Framework;
using UnityEngine;

public class FlashlightBeamModelTests
{
    [Test]
    public void BatteryBrightness_DimsContinuouslyAndReachesZero()
    {
        Assert.That(FlashlightBeamModel.GetBatteryBrightness(1f, 0.15f), Is.EqualTo(1f));
        Assert.That(FlashlightBeamModel.GetBatteryBrightness(0.5f, 0.15f), Is.EqualTo(0.3625f).Within(0.001f));
        Assert.That(FlashlightBeamModel.GetBatteryBrightness(0f, 0.15f), Is.Zero);
    }

    [Test]
    public void SpotAngle_NarrowsAsFocusIncreases()
    {
        Assert.That(FlashlightBeamModel.GetSpotAngle(60f, 15f, 0f), Is.EqualTo(60f));
        Assert.That(FlashlightBeamModel.GetSpotAngle(60f, 15f, 1f), Is.EqualTo(15f));
    }

    [Test]
    public void ScrollFocus_UsesAStableStepRegardlessOfDeviceMagnitude()
    {
        Assert.That(FlashlightBeamModel.ApplyScrollFocus(0.5f, 1f, 0.1f), Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(FlashlightBeamModel.ApplyScrollFocus(0.5f, 120f, 0.1f), Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(FlashlightBeamModel.ApplyScrollFocus(0.5f, -120f, 0.1f), Is.EqualTo(0.4f).Within(0.001f));
    }

    [Test]
    public void FocusedBeam_RequiresFocusRangeAndConeAlignment()
    {
        Assert.That(FlashlightBeamModel.IsInsideFocusedBeam(
            Vector3.zero, Vector3.forward, Vector3.forward * 5f, 10f, 20f, 1f, 0.75f), Is.True);
        Assert.That(FlashlightBeamModel.IsInsideFocusedBeam(
            Vector3.zero, Vector3.forward, Vector3.right * 5f, 10f, 20f, 1f, 0.75f), Is.False);
        Assert.That(FlashlightBeamModel.IsInsideFocusedBeam(
            Vector3.zero, Vector3.forward, Vector3.forward * 5f, 10f, 20f, 0.5f, 0.75f), Is.False);
    }
}
