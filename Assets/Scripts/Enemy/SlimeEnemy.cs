using UnityEngine;

/// <summary>
/// 元素史莱姆:弹跳移动,不依赖 NavMesh;低血量可选自爆或缩壳防御
/// </summary>
public class SlimeEnemy : EnemyBase
{
    [Header("弹跳参数")]
    public float jumpHeight = 0.5f;     // 普通跳跃高度
    public float jumpDuration = 0.8f;   // 一次跳跃时长
    public float restTime = 0.5f;       // 落地休息时间
    public float patrolRadius = 4f;     // 巡逻范围

    [Header("类型")]
    [Tooltip("勾选 = 龟壳防御型(低血缩壳);不勾 = 元素自爆型(低血自爆)")]
    public bool useShellDefense = false;

    // 弹跳状态
    private bool _isJumping;
    private Vector3 _jumpStart;
    private Vector3 _jumpEnd;
    private float _jumpHeight;
    private float _jumpDuration;
    private float _jumpTime;
    private float _landTime;

    /// <summary>是否落地且休息完毕(可以跳下一次)</summary>
    public bool IsGrounded => !_isJumping && Time.time >= _landTime;
    /// <summary>是否处于缩壳防御状态</summary>
    public bool IsDefending { get; set; }

    protected override void Awake()
    {
        base.Awake();
        idleAnimName = "IdleNormal";
        hitHash = Animator.StringToHash("GetHit");
        if (navMeshAgent != null) navMeshAgent.enabled = false;   // 性能:史莱姆不用寻路,直接关闭
        BuildTree();
    }

    protected override void Update()
    {
        UpdateJump();   // 先更新弹跳
        base.Update();  // 再跑行为树和血条
    }

    /// <summary>
    /// 开始一次跳跃(纯数学抛物线,零物理开销)
    /// </summary>
    public void StartJump(Vector3 target, float height, float duration)
    {
        _jumpStart = transform.position;
        _jumpEnd = new Vector3(target.x, _jumpStart.y, target.z);
        _jumpHeight = height;
        _jumpDuration = Mathf.Max(0.1f, duration);
        _jumpTime = 0f;
        _isJumping = true;
    }

    private void UpdateJump()
    {
        if (!_isJumping) return;

        _jumpTime += Time.deltaTime;
        float t = Mathf.Clamp01(_jumpTime / _jumpDuration);

        // 水平匀速移动 + 垂直抛物线高度(4t(1-t) 就是标准抛物线)
        Vector3 flat = Vector3.Lerp(_jumpStart, _jumpEnd, t);
        float height = _jumpHeight * 4f * t * (1f - t);
        transform.position = new Vector3(flat.x, _jumpStart.y + height, flat.z);

        if (t >= 1f)
        {
            _isJumping = false;
            _landTime = Time.time + restTime;   // 落地后休息
        }
    }

    public override void SwitchState(EnemyState state)
    {
    }

    public override void Hurt(PlayerWeaponBullet bullet, float damageMultiplier = 1)
    {
        // 缩壳防御:伤害减半,播防御受击动画
        if (IsDefending)
        {
            base.Hurt(bullet, damageMultiplier * 0.5f);
            ResetAnimationOnceCache();
            animator.CrossFadeInFixedTime("DefendGetHit", 0.1f);
            return;
        }

        base.Hurt(bullet, damageMultiplier);
        if (!IsDead)
        {
            animator.CrossFadeInFixedTime("GetHit", 0.1f);
            currentPhase = EnemyPhase.Hit;
            ResetAnimationOnceCache();
        }
    }

    private void BuildTree()
    {
        // 低血量分支:元素型自爆 / 龟壳型防御,二选一
        BTNode lowHealthBranch;
        if (useShellDefense)
        {
            lowHealthBranch = new Sequence(new LowHealthCondition(this, 0.2f), new DefendAction(this));
        }
        else
        {
            lowHealthBranch = new Sequence(new LowHealthCondition(this, 0.2f), new SelfDestructAction(this));
        }

        behaviorTree = new Selector(
            // 1. 死亡
            new Sequence(new IsDeadCondition(this), new DeathAction(this, "Die")),

            // 2. 受击硬直(史莱姆硬直短:0.2 秒)
            new Sequence(new IsHitCondition(this), new HitAction(this, 0.2f)),

            // 3. 低血量:自爆 或 缩壳防御
            lowHealthBranch,

            // 4. 战斗:≤3米撞击攻击,否则弹跳追击
            new Sequence(new HasTargetCondition(this),
                new Selector(
                    new Sequence(new PlayerDistanceCondition(this, -1f, 3f), new SlimeRamAttackAction(this)),
                    new SlimeChaseAction(this)
                )
            ),

            // 5. 警戒
            new Sequence(new IsAlertCondition(this), new FaceLastKnownAction(this)),

            // 6. 弹跳巡逻
            new SlimePatrolAction(this)
        );
    }
}