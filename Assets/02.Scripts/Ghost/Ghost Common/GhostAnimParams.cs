using UnityEngine;

public static class GhostAnimParams
{
    // 공통 애니메이션
    public static readonly int GhostIdle = Animator.StringToHash("idle");
    public static readonly int GhostWalk = Animator.StringToHash("walk");
    // public static readonly int GhostRunHash = Animator.StringToHash("run");
    public static readonly int GhostChase = Animator.StringToHash("chase");
    public static readonly int GhostHit = Animator.StringToHash("hit");
    public static readonly int GhostDie = Animator.StringToHash("dead");
    public static readonly int GhostAttack = Animator.StringToHash("attack");
    public static readonly int GhostCrawlAttack = Animator.StringToHash("crawlAttack");
    public static readonly int GhostIsCrawling = Animator.StringToHash("isCrawling");
    public static readonly int GhostExorcism = Animator.StringToHash("exorcism");

    // public const string IdleName = "Idle";
    // public const string HitName = "Hit";
    // public const string DieName = "Death";
    // public const string AttackName = "Attack";
    // public const string CrawlAttackName = "CrawlAttack";
    // public const string ExorcismName = "Exorcism";

    // Random Attack
    public static readonly int AttackIndex = Animator.StringToHash("attackIndex");
    public const string Attack_1 = "Attack_1";
    public const string Attack_2 = "Attack_2";
    public const string Attack_3 = "Attack_3";
    public const string Attack_4 = "Attack_4";

    public static readonly string[] AttackStates = { Attack_1, Attack_2, Attack_3, Attack_4 };
}

