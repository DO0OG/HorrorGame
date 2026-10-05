using NUnit.Framework;
using UnityEngine;

public class MonsterAwarenessMemoryTests
{
    [Test]
    public void Observe_RequiresVisualConfirmationBeforeAlert()
    {
        var memory = new MonsterAwarenessMemory();

        memory.Observe(Vector3.one, Vector3.forward, 0.2f, 0.6f);
        Assert.That(memory.State, Is.EqualTo(MonsterAwarenessMemory.AwarenessState.Suspicious));

        memory.Observe(Vector3.one, Vector3.forward, 0.4f, 0.6f);
        Assert.That(memory.State, Is.EqualTo(MonsterAwarenessMemory.AwarenessState.Alerted));
    }

    [Test]
    public void RegisterStimulus_InvestigatesWithoutGrantingPerfectAlert()
    {
        var memory = new MonsterAwarenessMemory();

        memory.RegisterStimulus(new Vector3(7f, 0f, 3f), 1f);

        Assert.That(memory.State, Is.EqualTo(MonsterAwarenessMemory.AwarenessState.Suspicious));
        Assert.That(memory.LastKnownPosition, Is.EqualTo(new Vector3(7f, 0f, 3f)));
        Assert.That(memory.LastKnownVelocity, Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void PredictPosition_UsesLastVelocityButCapsLeadTime()
    {
        var memory = new MonsterAwarenessMemory();
        memory.Observe(Vector3.zero, Vector3.right * 3f, 1f, 1f);
        memory.Tick(4f);

        Vector3 predicted = memory.PredictPosition(1.5f);

        Assert.That(predicted, Is.EqualTo(Vector3.right * 4.5f));
    }

    [Test]
    public void Decay_ForgetsStaleEvidenceAtZeroSuspicion()
    {
        var memory = new MonsterAwarenessMemory();
        memory.RegisterStimulus(Vector3.one, 0.5f);

        memory.Decay(1f);

        Assert.That(memory.State, Is.EqualTo(MonsterAwarenessMemory.AwarenessState.Calm));
        Assert.That(memory.HasMemory, Is.False);
    }

    [Test]
    public void RegisterStimulus_StoresUncertaintyAndSharedOrigin()
    {
        var memory = new MonsterAwarenessMemory();

        memory.RegisterStimulus(new Vector3(2f, 0f, 4f), 0.6f, 3f, true);

        Assert.That(memory.PositionUncertainty, Is.EqualTo(3f));
        Assert.That(memory.LastStimulusConfidence, Is.EqualTo(0.6f));
        Assert.That(memory.IsSharedMemory, Is.True);
    }

    [Test]
    public void AgeEvidence_DecaysAfterGraceAndExpandsUncertainty()
    {
        var memory = new MonsterAwarenessMemory();
        memory.RegisterStimulus(Vector3.zero, 0.5f, 1f, false);

        memory.AgeEvidence(1f, 1.5f, 0.1f, 0.5f);
        Assert.That(memory.Suspicion, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(memory.PositionUncertainty, Is.EqualTo(1f).Within(0.0001f));

        memory.AgeEvidence(1f, 1.5f, 0.1f, 0.5f);
        Assert.That(memory.Suspicion, Is.EqualTo(0.45f).Within(0.0001f));
        Assert.That(memory.PositionUncertainty, Is.EqualTo(1.25f).Within(0.0001f));
    }

    [Test]
    public void Observe_ReplacesUncertainSharedEvidenceWithDirectKnowledge()
    {
        var memory = new MonsterAwarenessMemory();
        memory.RegisterStimulus(Vector3.one, 0.8f, 4f, true);

        memory.Observe(Vector3.right, Vector3.forward, 0.2f, 0.5f);

        Assert.That(memory.PositionUncertainty, Is.Zero);
        Assert.That(memory.LastStimulusConfidence, Is.EqualTo(1f));
        Assert.That(memory.IsSharedMemory, Is.False);
    }
}
