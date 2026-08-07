using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 等待指定秒数
/// </summary>
public class WaitAction : BTNode
{
    private readonly float _duration;
    private float _endTime;
    private bool _started;

    public WaitAction(float duration)
    {
        _duration = duration;
        NodeName = "等待";
    }
    public override NodeState Evaluate()
    {
        if (!_started)
        {
            _started = true;
            _endTime = Time.time + _duration;
        }
        if (Time.time >= _endTime)
        {
            _started = false;
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}

/// <summary>
/// 巡逻,出生点周围随机走，走两步发呆3到5秒
/// </summary>
public class PatrolAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _patrolRadius;
    private readonly string _victoryAnim;
    private Vector3 _targetPoint;
    private float _idleEndTime;
    private float _victoryEndTime;
    private bool _idling = true;
    private bool _idleStarted;
    private bool _doingVictory;

    public PatrolAction(EnemyBase enemy, float patrolRadius = 5f, string victoryAnim = null)
    {
        _enemy = enemy;
        _patrolRadius = patrolRadius;
        _victoryAnim = victoryAnim;
        _targetPoint = _enemy.SpawnPoint;
        _idleEndTime = Time.time + Random.Range(3f, 5f);
        NodeName = "巡逻";
    }

    public override NodeState Evaluate()
    {
        _enemy.currentPhase = EnemyPhase.Patrol;
        _enemy.navMeshAgent.speed = _enemy.stats.patrolSpeed;

        //胜利动作//播放完继续巡逻
        if (_doingVictory)
        {
            if (Time.time >= _victoryEndTime)
            {
                _doingVictory = false;
                _idling = false;
                _idleStarted = false;
                PickNewTarget();
            }
            return NodeState.Running;
        }
        if (_idling)
        {
            if (!_idleStarted)
            {
                _idleStarted = true;
                _enemy.navMeshAgent.isStopped = true;
                _enemy.navMeshAgent.ResetPath();
            }
            _enemy.PlayAnimationOnce(_enemy.idleAnimName);
            _enemy.SetMoveSpeed(0f);

            if (Time.time >= _idleEndTime)
            {
                //有胜利动画时，30%概率播放胜利动画
                if (!string.IsNullOrEmpty(_victoryAnim) && Random.value < 0.3f)
                {
                    _doingVictory = true;
                    _victoryEndTime = Time.time + 2f;
                    _enemy.ResetAnimationOnceCache();
                    _enemy.animator.CrossFadeInFixedTime(_victoryAnim, 0.1f);
                    _enemy.SetMoveSpeed(0f);
                }
                else
                {
                    _idling = false;
                    _idleStarted = false;
                    PickNewTarget();
                }
            }
            return NodeState.Running;
        }

        _enemy.PlayAnimationOnce(_enemy.walkAnimName);
        _enemy.SetMoveSpeed(0.5f);
        _enemy.navMeshAgent.isStopped = false;

        float dist = Vector3.Distance(_enemy.transform.position, _targetPoint);
        if (dist < 0.8f)
        {
            _idling = true;
            _idleEndTime = Time.time + Random.Range(3f, 5f);
        }
        return NodeState.Running;
    }

    private void PickNewTarget()
    {
        Vector2 circle = Random.insideUnitCircle * _patrolRadius;
        Vector3 candidate = _enemy.SpawnPoint + new Vector3(circle.x, 0f, circle.y);

        // 只在 NavMesh 上找巡逻点,找不到就原地发呆
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            _targetPoint = hit.position;
        }
        else
        {
            _targetPoint = _enemy.SpawnPoint;
        }

        _enemy.navMeshAgent.SetDestination(_targetPoint);
    }
}


/// <summary>
/// 追击，节流设置寻路目标
/// </summary>
public class ChaseAction : BTNode
{
    private readonly EnemyBase _enemy;
    private float _nextSetTime;

    public ChaseAction(EnemyBase enemy)
    {
        _enemy = enemy; NodeName = "追击";
    }

    public override NodeState Evaluate()
    {
        var target = _enemy.perception != null ? _enemy.perception.Target : null;
        if (target == null) return NodeState.Failure;

        _enemy.currentPhase = EnemyPhase.Combat;
        _enemy.navMeshAgent.isStopped = false;
        _enemy.navMeshAgent.speed = _enemy.stats.chaseSpeed;
        _enemy.PlayAnimationOnce(_enemy.walkAnimName);
        _enemy.SetMoveSpeed(1f);

        if (Time.time >= _nextSetTime)
        {
            _nextSetTime = Time.time + 0.3f;              // 性能:节流
            _enemy.navMeshAgent.SetDestination(target.transform.position);
        }

        FaceTarget(_enemy, target.transform.position);
        return NodeState.Running;
    }

    /// <summary>面朝一个位置(供其他动作复用)</summary>
    public static void FaceTarget(EnemyBase enemy, Vector3 lookPos)
    {
        Vector3 dir = lookPos - enemy.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            enemy.transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}

/// <summary>
/// 近战攻击,播攻击动画,播完造成伤害,进入冷却
/// </summary>
public class MeleeAttackAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly string _animName;    // 新增:攻击动画名
    private readonly float _cooldown;
    private readonly float _animTime;
    private float _attackEndTime;
    private float _nextAttackTime;
    private readonly float _attackRange;

    public MeleeAttackAction(EnemyBase enemy, string animName = "Attack",
                             float cooldown = 1.5f, float animTime = 1f, float attackRange = -1f)
    {
        _enemy = enemy;
        _animName = animName;
        _cooldown = cooldown;
        _animTime = animTime;
        //没传范围就用默认(近战距离+0.5),传了就用传的
        _attackRange = attackRange < 0f ? enemy.minAttackDistance + 0.5f : attackRange;
        NodeName = "近战攻击";
    }

    public override NodeState Evaluate()
    {
        var target = _enemy.perception != null ? _enemy.perception.Target : null;
        if (target == null) return NodeState.Failure;

        _enemy.currentPhase = EnemyPhase.Combat;

        if (Time.time < _nextAttackTime)
        {
            ChaseAction.FaceTarget(_enemy, target.transform.position);
            return NodeState.Running;
        }

        if (_attackEndTime <= 0f)
        {
            _enemy.ResetAnimationOnceCache();
            _enemy.animator.CrossFadeInFixedTime(_animName, 0.1f);   // 用 _animName 代替写死的 "Attack"
            _attackEndTime = Time.time + _animTime;
        }

        if (Time.time >= _attackEndTime)
        {
            float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
            if (dist <= _attackRange)
            {
                GameManager.INSTANCE?.ApplyDamageToActiveRole(_enemy.stats.attackDamage);
            }
            _nextAttackTime = Time.time + _cooldown;
            _attackEndTime = 0f;
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}

/// <summary>
/// 死亡:播死亡动画,1.5 秒后销毁
/// </summary>
public class DeathAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly string _animName;
    private float _endTime;
    private bool _started;

    public DeathAction(EnemyBase enemy, string animName = "Dead")
    {
        _enemy = enemy;
        _animName = animName;
        NodeName = "死亡";
    }

    public override NodeState Evaluate()
    {
        if (!_started)
        {
            _started = true;
            _enemy.currentPhase = EnemyPhase.Dead;
            _enemy.ResetAnimationOnceCache();
            _enemy.animator.CrossFadeInFixedTime("Dead", 0.1f);
            _endTime = Time.time + 1.5f;
        }
        if (Time.time >= _endTime)
        {
            _enemy.Clear();
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}

/// <summary>
/// 受击硬直,停 0.3 秒后恢复
/// </summary>
public class HitAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _stagger;
    private float _endTime;
    private bool _started;

    public HitAction(EnemyBase enemy, float stagger = 0.3f)
    {
        _enemy = enemy;
        _stagger = stagger;
        NodeName = "受击硬直";
    }

    public override NodeState Evaluate()
    {
        if (!_started)
        {
            _started = true;
            _endTime = Time.time + _stagger;
        }
        if (Time.time >= _endTime)
        {
            _started = false;
            _enemy.currentPhase = EnemyPhase.Patrol;
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}

/// <summary>
/// 警戒,转向最后听到/看到的位置,持续 1.5 秒后解除警戒
/// </summary>
public class FaceLastKnownAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _duration;
    private float _endTime;
    private bool _started;

    public FaceLastKnownAction(EnemyBase enemy, float duration = 1.5f)
    {
        _enemy = enemy;
        _duration = duration;
        NodeName = "警戒转向";
    }

    public override NodeState Evaluate()
    {
        if (!_started)
        {
            _started = true;
            _endTime = Time.time + _duration;
        }

        Vector3 pos = _enemy.perception != null
            ? _enemy.perception.LastKnownPosition
            : _enemy.transform.position;
        ChaseAction.FaceTarget(_enemy, pos);
        _enemy.currentPhase = EnemyPhase.Alert;
        _enemy.PlayAnimationOnce(_enemy.idleAnimName);
        _enemy.SetMoveSpeed(0f);
        if (Time.time >= _endTime)
        {
            _started = false;
            _enemy.perception?.ClearAlert();
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}

/// <summary>
/// 低血犹豫:停一下,冷却几秒后才可能再次犹豫(避免卡死)
/// </summary>
public class HesitateAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _duration;
    private readonly float _cooldown;
    private float _endTime;
    private float _nextHesitateTime;
    private bool _started;

    public HesitateAction(EnemyBase enemy, float duration = 0.8f, float cooldown = 5f)
    {
        _enemy = enemy;
        _duration = duration;
        _cooldown = cooldown;
        NodeName = "低血犹豫";
    }

    public override NodeState Evaluate()
    {
        // 冷却中:不犹豫,返回失败,让 Selector 继续往下走(追击/攻击)
        if (Time.time < _nextHesitateTime)
        {
            _started = false;
            return NodeState.Failure;
        }

        if (!_started)
        {
            _started = true;
            _endTime = Time.time + _duration;
        }

        if (Time.time >= _endTime)
        {
            _started = false;
            _nextHesitateTime = Time.time + _cooldown;
            return NodeState.Success;
        }
        return NodeState.Running;
    }
}

/// <summary>
/// 后退,面朝玩家倒退拉开距离;被挡就侧向横跳;距离够了返回成功
/// </summary>
public class RetreatAction : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _stopDistance;
    private float _nextSetTime;
    private bool _strafeRight = true;

    public RetreatAction(EnemyBase enemy, float stopDistance = 8f)
    {
        _enemy = enemy;
        _stopDistance = stopDistance;
        NodeName = "后退拉开距离";
    }

    public override NodeState Evaluate()
    {
        var target = _enemy.perception != null ? _enemy.perception.Target : null;
        if (target == null) return NodeState.Failure;

        _enemy.currentPhase = EnemyPhase.Combat;
        _enemy.navMeshAgent.speed = _enemy.stats.retreatSpeed;
        _enemy.navMeshAgent.isStopped = false;
        _enemy.PlayAnimationOnce(_enemy.walkAnimName);
        _enemy.SetMoveSpeed(0.8f);

        float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
        if (dist >= _stopDistance)
        {
            return NodeState.Success;   // 拉开到 8 米以上,交给远程攻击
        }

        // 后退方向 = 远离玩家
        Vector3 away = (_enemy.transform.position - target.transform.position).normalized;
        away.y = 0f;

        if (Time.time >= _nextSetTime)
        {
            _nextSetTime = Time.time + 0.3f;   // 节流

            // 后退被挡(身后 2 米内有东西)→ 侧向横跳
            if (Physics.Raycast(_enemy.transform.position, away, out RaycastHit hit, 2f))
            {
                _strafeRight = !_strafeRight;
                Vector3 side = _strafeRight
                    ? Vector3.Cross(Vector3.up, away)
                    : Vector3.Cross(away, Vector3.up);
                side.y = 0f;
                away = side.normalized;
            }

            _enemy.navMeshAgent.SetDestination(_enemy.transform.position + away * 3f);
        }

        ChaseAction.FaceTarget(_enemy, target.transform.position);
        return NodeState.Running;
    }
}