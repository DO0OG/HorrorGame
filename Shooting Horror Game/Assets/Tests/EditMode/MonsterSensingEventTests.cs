using NUnit.Framework;
using UnityEngine;

public class MonsterSensingEventTests
{
    [Test]
    public void NoiseEvent_ClampsInvalidStrengthAndRadius()
    {
        var noise = new MonsterNoiseEvent(
            MonsterNoiseKind.Gunshot,
            Vector3.one,
            2f,
            -5f,
            3f);

        Assert.That(noise.Loudness, Is.EqualTo(1f));
        Assert.That(noise.Radius, Is.Zero);
        Assert.That(noise.Position, Is.EqualTo(Vector3.one));
    }

    [Test]
    public void NoiseSystem_PublishesSemanticEvent()
    {
        MonsterNoiseEvent received = default;
        bool invoked = false;
        System.Action<MonsterNoiseEvent> handler = noise =>
        {
            received = noise;
            invoked = true;
        };

        MonsterNoiseSystem.NoiseEmitted += handler;
        try
        {
            MonsterNoiseSystem.Publish(new MonsterNoiseEvent(
                MonsterNoiseKind.Crouch,
                Vector3.forward,
                0.2f,
                5f,
                9f));
        }
        finally
        {
            MonsterNoiseSystem.NoiseEmitted -= handler;
        }

        Assert.That(invoked, Is.True);
        Assert.That(received.Kind, Is.EqualTo(MonsterNoiseKind.Crouch));
        Assert.That(received.Timestamp, Is.EqualTo(9f));
    }

    [Test]
    public void SharedAlert_ClampsConfidenceUncertaintyAndRadius()
    {
        var alert = new MonsterSharedAlert(
            42,
            Vector3.right,
            3f,
            -2f,
            -10f,
            1f);

        Assert.That(alert.Confidence, Is.EqualTo(1f));
        Assert.That(alert.Uncertainty, Is.Zero);
        Assert.That(alert.Radius, Is.Zero);
    }
}
