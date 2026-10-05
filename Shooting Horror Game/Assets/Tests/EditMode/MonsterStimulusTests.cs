using NUnit.Framework;
using UnityEngine;

public class MonsterStimulusTests
{
    [Test]
    public void Constructor_ClampsConfidenceAndUncertainty()
    {
        var stimulus = new MonsterStimulus(
            MonsterStimulusType.Sound,
            Vector3.one,
            2f,
            -1f,
            4f,
            Vector3.forward,
            true);

        Assert.That(stimulus.Confidence, Is.EqualTo(1f));
        Assert.That(stimulus.Uncertainty, Is.EqualTo(0f));
        Assert.That(stimulus.Shared, Is.True);
    }

    [Test]
    public void PredictPosition_UsesAgeAndUncertainty()
    {
        var stimulus = new MonsterStimulus(
            MonsterStimulusType.Visual,
            Vector3.zero,
            0.9f,
            0.25f,
            10f,
            Vector3.right * 4f,
            false);

        Vector3 predicted = stimulus.PredictPosition(12f, 5f);

        Assert.That(predicted.x, Is.EqualTo(6f).Within(0.0001f));
        Assert.That(predicted.y, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(predicted.z, Is.EqualTo(0f).Within(0.0001f));
    }
}
