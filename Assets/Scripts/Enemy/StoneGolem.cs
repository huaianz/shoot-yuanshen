using UnityEngine;

/// <summary>
/// 远程石头人:保持距离丢石头,近身用拳击
/// </summary>
public class StoneGolem : EnemyBase
{
    [Header("远程攻击")]
    public GameObject rockPrefab;      // 石头预制体
    public Transform rockSpawnPoint;   // 丢石位置(右手)

    protected override void Awake()
    {
        base.Awake();
        walkAnimName = "Walk";
        hitHash = Animator.StringToHash("GetHit");
        navMeshAgent.speed = stats.patrolSpeed;
        BuildTree();
    }

    public override void SwitchState(EnemyState state)
    {
    }

    public override void Hurt(PlayerWeaponBullet bullet, float damageMultiplier = 1)
    {
        base.Hurt(bullet, damageMultiplier);
        if (!IsDead)
        {
            animator.CrossFadeInFixedTime("GetHit", 0.1f);
            currentPhase = EnemyPhase.Hit;
            ResetAnimationOnceCache();
        }
    }

    /// <summary>
    /// 由 Attack01 动画事件调用:动画挥手的那个时间点丢出石头
    /// </summary>
    public void OnThrowRock()
    {
        PlayerModel target = perception != null ? perception.Target : null;
        if (target == null) return;
        SpawnRock(target);
    }

    private void SpawnRock(PlayerModel target)
    {
        if (rockPrefab == null || rockSpawnPoint == null) return;

        Vector3 spawnPos = rockSpawnPoint.position + Vector3.up * 0.5f;
        Vector3 toTarget = target.transform.position - spawnPos;
        Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z);

        // 抛物线:慢速 13,按飞行时间算向上初速度
        float speed = 13f;
        float flightTime = flatDir.magnitude / speed;
        if (flightTime < 0.01f) return;

        float gravity = Physics.gravity.magnitude;
        float upSpeed = (toTarget.y + 0.5f * gravity * flightTime * flightTime) / flightTime;

        Vector3 velocity = flatDir.normalized * speed + Vector3.up * upSpeed;

        GameObject rock = Object.Instantiate(rockPrefab, spawnPos, Quaternion.identity);
        rock.GetComponent<RockProjectile>()?.Launch(spawnPos, velocity);
    }

    private void BuildTree()
    {
        behaviorTree = new Selector(
            new Sequence(new IsDeadCondition(this), new DeathAction(this, "Die")),
            new Sequence(new IsHitCondition(this), new HitAction(this, 0.3f)),
            new Sequence(new HasTargetCondition(this),
                new Selector(
                    new Sequence(new PlayerDistanceCondition(this, -1f, 3f),
                        new MeleeAttackAction(this, "Attack02", 1.2f, 1f, 3f)),
                    new Sequence(new PlayerDistanceCondition(this, -1f, 3.5f),
                        new RetreatAction(this, 8f)),
                    new Sequence(new PlayerDistanceCondition(this, 15f, -1f),
                        new ChaseAction(this)),
                    new RangedAttackAction(this)
                )
            ),
            new Sequence(new IsAlertCondition(this), new FaceLastKnownAction(this)),
            new PatrolAction(this, 5f, "Victory")
        );
    }
}