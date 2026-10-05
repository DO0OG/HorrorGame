using NUnit.Framework;
using UnityEngine;

public class MonsterSearchPlannerTests
{
    [Test]
    public void RankCandidates_PrefersDoorwayAlignedWithLastKnownVelocity()
    {
        MonsterSearchContext context = CreateContext();
        var points = new[]
        {
            new MonsterInterestPoint(MonsterInterestPointKind.Doorway, new Vector3(6f, 0f, 0f), 0.8f, -10f, true, false),
            new MonsterInterestPoint(MonsterInterestPointKind.HidingSpot, new Vector3(-3f, 0f, 0f), 0.8f, -10f, true, true),
            new MonsterInterestPoint(MonsterInterestPointKind.Intersection, new Vector3(2f, 0f, 4f), 0.6f, -10f, true, false),
        };
        var results = new MonsterSearchPoint[3];

        int count = MonsterSearchPlanner.RankCandidates(context, points, results);

        Assert.That(count, Is.EqualTo(3));
        Assert.That(results[0].Kind, Is.EqualTo(MonsterInterestPointKind.Doorway));
    }

    [Test]
    public void BuildPlan_FillsMissingEntriesWithDeterministicGoldenAnglePoints()
    {
        MonsterSearchContext context = CreateContext();
        var results = new MonsterSearchPoint[3];

        int count = MonsterSearchPlanner.BuildPlan(context, null, results);

        Assert.That(count, Is.EqualTo(3));
        Assert.That(results[0].IsFallback, Is.True);
        Assert.That(results[0].Position.x, Is.EqualTo(1.5f).Within(0.0001f));
        Assert.That(results[0].Position.z, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(results[1].Position.x, Is.LessThan(0f));
        Assert.That(results[2].Position.z, Is.LessThan(0f));
    }

    [Test]
    public void ScoreCandidate_PenalizesRecentlyVisitedPoints()
    {
        MonsterSearchContext context = CreateContext();
        MonsterInterestPoint fresh = new MonsterInterestPoint(MonsterInterestPointKind.Doorway, new Vector3(4f, 0f, 0f), 0.7f, 9f, true, false);
        MonsterInterestPoint stale = new MonsterInterestPoint(MonsterInterestPointKind.Doorway, new Vector3(4f, 0f, 0f), 0.7f, -20f, true, false);

        float freshScore = MonsterSearchPlanner.ScoreCandidate(context, fresh);
        float staleScore = MonsterSearchPlanner.ScoreCandidate(context, stale);

        Assert.That(staleScore, Is.GreaterThan(freshScore));
    }

    private static MonsterSearchContext CreateContext()
    {
        return new MonsterSearchContext
        {
            Now = 10f,
            Center = Vector3.zero,
            LastKnownVelocity = Vector3.right,
            SearchRadius = 8f,
            RecentVisitWindow = 5f,
            FallbackStartIndex = 0,
        };
    }
}
