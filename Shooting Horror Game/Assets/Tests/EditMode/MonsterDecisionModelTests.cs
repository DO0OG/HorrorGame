using NUnit.Framework;
using UnityEngine;

public class MonsterDecisionModelTests
{
    [Test]
    public void Evaluate_UsesListenForFreshAmbiguousStimulus()
    {
        var model = new MonsterDecisionModel();
        MonsterDecisionContext context = CreateBaseContext(1f);
        context.HasFreshStimulus = true;
        context.TimeSinceStimulus = 0.3f;
        context.PrimaryStimulus = new MonsterStimulus(
            MonsterStimulusType.Sound,
            new Vector3(4f, 0f, 2f),
            0.55f,
            0.35f,
            0.8f,
            Vector3.zero,
            false);

        MonsterDecision decision = model.Evaluate(context);

        Assert.That(decision.Tactic, Is.EqualTo(MonsterTactic.Listen));
        Assert.That(decision.Intent, Is.EqualTo(MonsterIntent.Investigate));
        Assert.That(decision.Reason, Is.EqualTo(MonsterDecisionReason.AmbiguousStimulus));
    }

    [Test]
    public void Evaluate_UsesSilentApproachWhenTargetIsUnaware()
    {
        var model = new MonsterDecisionModel();
        MonsterDecisionContext context = CreateBaseContext(4f);
        context.HasVisualContact = true;
        context.HasReachableTargetPath = true;
        context.HasCoverApproach = true;
        context.TargetAwareOfMonster = false;
        context.Suspicion = 0.85f;
        context.DistanceToTarget = 8f;
        context.TargetPosition = new Vector3(10f, 0f, 0f);
        context.TargetVelocity = Vector3.right * 2f;

        MonsterDecision decision = model.Evaluate(context);

        Assert.That(decision.Tactic, Is.EqualTo(MonsterTactic.SilentApproach));
        Assert.That(decision.Intent, Is.EqualTo(MonsterIntent.Ambush));
        Assert.That(decision.FocusPoint.x, Is.LessThan(context.TargetPosition.x));
    }

    [Test]
    public void Evaluate_BlocksThirdConsecutiveUseWithinRepeatWindow()
    {
        var model = new MonsterDecisionModel();
        MonsterDecisionContext context = CreateBaseContext(1f);
        context.HasVisualContact = true;
        context.HasReachableTargetPath = true;
        context.HasCoverApproach = true;
        context.TargetAwareOfMonster = false;
        context.Suspicion = 0.9f;
        context.DistanceToTarget = 7f;
        context.TargetPosition = new Vector3(6f, 0f, 0f);
        context.TargetVelocity = Vector3.right;

        MonsterDecision first = model.Evaluate(context);
        context.Now = 8f;
        MonsterDecision second = model.Evaluate(context);
        context.Now = 14.5f;
        context.HasFlankRoute = true;
        MonsterDecision third = model.Evaluate(context);

        Assert.That(first.Tactic, Is.EqualTo(MonsterTactic.SilentApproach));
        Assert.That(second.Tactic, Is.EqualTo(MonsterTactic.SilentApproach));
        Assert.That(third.Tactic, Is.Not.EqualTo(MonsterTactic.SilentApproach));
    }

    [Test]
    public void Evaluate_PrefersInterceptForReliableEscapeVector()
    {
        var model = new MonsterDecisionModel();
        MonsterDecisionContext context = CreateBaseContext(3f);
        context.HasFreshStimulus = true;
        context.HasInterceptRoute = true;
        context.DistanceToLastKnownPosition = 9f;
        context.PrimaryStimulus = new MonsterStimulus(
            MonsterStimulusType.Sound,
            new Vector3(5f, 0f, 0f),
            0.9f,
            0.05f,
            2.5f,
            Vector3.forward * 3f,
            false);

        MonsterDecision decision = model.Evaluate(context);

        Assert.That(decision.Tactic, Is.EqualTo(MonsterTactic.Intercept));
        Assert.That(decision.Intent, Is.EqualTo(MonsterIntent.Search));
        Assert.That(decision.FocusPoint.z, Is.GreaterThan(context.PrimaryStimulus.Position.z));
    }

    private static MonsterDecisionContext CreateBaseContext(float now)
    {
        return new MonsterDecisionContext
        {
            Now = now,
            SelfPosition = Vector3.zero,
            TargetPosition = new Vector3(5f, 0f, 0f),
            LastKnownPosition = new Vector3(5f, 0f, 0f),
            TimeSinceStimulus = 10f,
            TimeSinceVisualContact = 10f,
            DistanceToTarget = 5f,
            DistanceToLastKnownPosition = 5f,
        };
    }
}
