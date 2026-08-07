using UnityEngine;

/// <summary>弹跳巡逻:落地休息 → 随机跳一步 → 再休息</summary>
public class SlimePatrolAction : BTNode
{
    private readonly SlimeEnemy _slime;
    private Vector3 _targetPoint;
    private bool _jumpScheduled;

    public SlimePatrolAction(SlimeEnemy slime)
    {
        _slime = slime;
        _targetPoint = slime.SpawnPoint;
        NodeName = "弹跳巡逻";
    }

    public override NodeState Evaluate()
    {
        _slime.currentPhase = EnemyPhase.Patrol;

        // 跳跃中:播移动动画
        if (!_slime.IsGrounded)
        {
            _slime.PlayAnimationOnce("WalkFWD");
            return NodeState.Running;
        }

        // 落地休息:播待机;休息完随机跳向巡逻点
        _slime.PlayAnimationOnce(_slime.idleAnimName);
        if (!_jumpScheduled)
        {
            _jumpScheduled = true;
            Vector2 circle = Random.insideUnitCircle * _slime.patrolRadius;
            _targetPoint = _slime.SpawnPoint + new Vector3(circle.x, 0f, circle.y);
            _slime.StartJump(_targetPoint, _slime.jumpHeight, _slime.jumpDuration);
        }
        return NodeState.Running;
    }
}

/// <summary>弹跳追击:朝玩家方向一跳一跳地追</summary>
public class SlimeChaseAction : BTNode
{
    private readonly SlimeEnemy _slime;
    private bool _jumpScheduled;

    public SlimeChaseAction(SlimeEnemy slime) { _slime = slime; NodeName = "弹跳追击"; }

    public override NodeState Evaluate()
    {
        var target = _slime.perception != null ? _slime.perception.Target : null;
        if (target == null) return NodeState.Failure;

        _slime.currentPhase = EnemyPhase.Combat;
        ChaseAction.FaceTarget(_slime, target.transform.position);

        if (!_slime.IsGrounded)
        {
            _slime.PlayAnimationOnce("RunFWD");
            return NodeState.Running;
        }

        _slime.PlayAnimationOnce(_slime.idleAnimName);
        if (!_jumpScheduled)
        {
            _jumpScheduled = true;
            Vector3 dirToPlayer = (target.transform.position - _slime.transform.position).normalized;
            Vector3 jumpTarget = _slime.transform.position + dirToPlayer * 2f;
            _slime.StartJump(jumpTarget, _slime.jumpHeight, _slime.jumpDuration);
        }
        return NodeState.Running;
    }
}

/// <summary>撞击攻击:蓄力跳向玩家,落地时造成伤害</summary>
public class SlimeRamAttackAction : BTNode
{
    private readonly SlimeEnemy _slime;
    private readonly float _cooldown;
    private float _nextAttackTime;
    private bool _attackJumpStarted;

    public SlimeRamAttackAction(SlimeEnemy slime, float cooldown = 1.5f)
    {
        _slime = slime;
        _cooldown = cooldown;
        NodeName = "撞击攻击";
    }

    public override NodeState Evaluate()
    {
        var target = _slime.perception != null ? _slime.perception.Target : null;
        if (target == null) return NodeState.Failure;

        _slime.currentPhase = EnemyPhase.Combat;
        ChaseAction.FaceTarget(_slime, target.transform.position);

        // 冷却中
        if (Time.time < _nextAttackTime)
        {
            _slime.PlayAnimationOnce(_slime.idleAnimName);
            return NodeState.Running;
        }

        // 开始蓄力跳(3米远、1.2米高)
        if (_slime.IsGrounded && !_attackJumpStarted)
        {
            _attackJumpStarted = true;
            _slime.PlayAnimationOnce("Attack01");
            _slime.StartJump(target.transform.position, 1.2f, 0.7f);
        }

        // 落地:目标还在附近就造成伤害
        if (_attackJumpStarted && _slime.IsGrounded)
        {
            float dist = Vector3.Distance(_slime.transform.position, target.transform.position);
            if (dist <= 3f)
            {
                GameManager.INSTANCE?.ApplyDamageToActiveRole(_slime.stats.attackDamage);
            }
            _attackJumpStarted = false;
            _nextAttackTime = Time.time + _cooldown;
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}

/// <summary>元素自爆:前摇1.5秒,玩家离开4米取消;爆炸3米范围25伤害,自爆后死亡</summary>
public class SelfDestructAction : BTNode
{
    private readonly SlimeEnemy _slime;
    private readonly float _windup = 1.5f;
    private readonly float _radius = 3f;
    private readonly int _damage = 25;
    private float _endTime;
    private bool _started;

    public SelfDestructAction(SlimeEnemy slime) { _slime = slime; NodeName = "元素自爆"; }

    public override NodeState Evaluate()
    {
        var target = _slime.perception != null ? _slime.perception.Target : null;
        if (target == null) return NodeState.Failure;

        float dist = Vector3.Distance(_slime.transform.position, target.transform.position);

        // 前摇期间玩家离开 4 米:取消自爆,回去继续打
        if (dist > 4f)
        {
            _started = false;
            return NodeState.Failure;
        }

        if (!_started)
        {
            _started = true;
            _slime.currentPhase = EnemyPhase.Dead;
            _slime.ResetAnimationOnceCache();
            _slime.animator.CrossFadeInFixedTime("Taunt", 0.1f);   // 膨胀闪烁警告
            _endTime = Time.time + _windup;
        }

        if (Time.time >= _endTime)
        {
            // 爆炸:只判定一次(性能:不做持续检测)
            if (dist <= _radius)
            {
                GameManager.INSTANCE?.ApplyDamageToActiveRole(_damage);
            }
            _slime.Kill();   // 自爆后死亡,死亡节点负责播 Die + 销毁
            _started = false;
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}

/// <summary>缩壳防御:低血时缩进壳里减伤,持续几秒后恢复</summary>
public class DefendAction : BTNode
{
    private readonly SlimeEnemy _slime;
    private readonly float _duration;
    private readonly float _cooldown;
    private float _endTime;
    private float _nextDefendTime;
    private bool _started;

    public DefendAction(SlimeEnemy slime, float duration = 2f, float cooldown = 5f)
    {
        _slime = slime;
        _duration = duration;
        _cooldown = cooldown;
        NodeName = "缩壳防御";
    }

    public override NodeState Evaluate()
    {
        var target = _slime.perception != null ? _slime.perception.Target : null;
        if (target == null) return NodeState.Failure;

        // 冷却中:不防御,回去战斗
        if (Time.time < _nextDefendTime)
        {
            _slime.IsDefending = false;
            return NodeState.Failure;
        }

        if (!_started)
        {
            _started = true;
            _slime.IsDefending = true;
            _slime.currentPhase = EnemyPhase.Alert;
            _slime.ResetAnimationOnceCache();
            _slime.animator.CrossFadeInFixedTime("Defend", 0.1f);
            _endTime = Time.time + _duration;
        }

        if (Time.time >= _endTime)
        {
            _started = false;
            _slime.IsDefending = false;
            _nextDefendTime = Time.time + _cooldown;
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}