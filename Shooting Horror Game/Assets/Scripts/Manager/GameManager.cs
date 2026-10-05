using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 난이도 및 난이도별 수치를 관리하는 정적 매니저.
/// 난이도 선택 UI가 생기면 SetDifficulty()만 호출하면 됨.
/// </summary>
public static class GameManager
{
    public static Define.Difficulty Difficulty { get; private set; } = Define.Difficulty.Normal;

    public class DifficultyData
    {
        // Player
        public float playerMaxHealth;

        // Normal Monster
        public float monsterDamage;             // 일반 몬스터 1회 공격 데미지
        public float sightRadius;               // 시야 반경
        public float sightAngle;                // 시야각
        public float soundDetectionRange;       // 청각 감지 최대 범위
        public float soundDetectionMinRange;    // 이 범위 안에서는 아무리 작은 소리도 감지
        public float lightDetectionRange;       // 손전등 빛 감지 최대 범위
        public float stunDuration;              // 총격 시 경직 시간

        // Hollow Monster
        public float hollowApproachRange;       // 플레이어 접근 시작 범위
        public float hollowSlowRate;            // 슬로우 배율 (0.5 = 이동속도 절반)
        public float hollowSlowDuration;        // 슬로우 지속 시간
    }

    private static readonly DifficultyData easy = new DifficultyData
    {
        playerMaxHealth = 150f,
        monsterDamage = 25f,
        sightRadius = 12f,
        sightAngle = 100f,
        soundDetectionRange = 15f,
        soundDetectionMinRange = 4f,
        lightDetectionRange = 15f,
        stunDuration = 6f,
        hollowApproachRange = 8f,
        hollowSlowRate = 0.7f,
        hollowSlowDuration = 4f,
    };

    // Normal : 체력 100, 데미지 30 → 3회 피격까지 생존, 4회째 피격 시 처형
    private static readonly DifficultyData normal = new DifficultyData
    {
        playerMaxHealth = 100f,
        monsterDamage = 30f,
        sightRadius = 15f,
        sightAngle = 110f,
        soundDetectionRange = 20f,
        soundDetectionMinRange = 5f,
        lightDetectionRange = 20f,
        stunDuration = 4f,
        hollowApproachRange = 10f,
        hollowSlowRate = 0.5f,
        hollowSlowDuration = 5f,
    };

    private static readonly DifficultyData hard = new DifficultyData
    {
        playerMaxHealth = 75f,
        monsterDamage = 40f,
        sightRadius = 20f,
        sightAngle = 130f,
        soundDetectionRange = 30f,
        soundDetectionMinRange = 7f,
        lightDetectionRange = 30f,
        stunDuration = 2.5f,
        hollowApproachRange = 12f,
        hollowSlowRate = 0.4f,
        hollowSlowDuration = 6f,
    };

    public static DifficultyData Data
    {
        get
        {
            switch (Difficulty)
            {
                case Define.Difficulty.Easy: return easy;
                case Define.Difficulty.Hard: return hard;
                default: return normal;
            }
        }
    }

    public static void SetDifficulty(Define.Difficulty difficulty)
    {
        Difficulty = difficulty;
    }
}
