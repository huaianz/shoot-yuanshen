using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 已死亡?
/// </summary>
public class IsDeadCondition : BTNode
{
    private readonly EnemyBase _enemy;
    public IsDeadCondition(EnemyBase enemy)
    {
        _enemy = enemy;
        NodeName = "已死亡？";
    }
    public override NodeState Evaluate() => _enemy.IsDead ? NodeState.Success : NodeState.Failure;
}

/// <summary>
/// 有锁定目标？
/// </summary>
public class HasTargetCondition : BTNode
{
    private readonly EnemyBase _enemy;
    public HasTargetCondition(EnemyBase enemy)
    {
        _enemy = enemy;
        NodeName = "有目标？";
    }
    public override NodeState Evaluate() => _enemy.HasTarget ? NodeState.Success : NodeState.Failure;
}

/// <summary>
/// 目标在攻击范围内？
/// </summary>
public class InAttackRangeCondition : BTNode
{
    private readonly EnemyBase _enemy;
    public InAttackRangeCondition(EnemyBase enemy)
    {
        _enemy = enemy;
        NodeName = "在攻击范围内？";
    }
    public override NodeState Evaluate()
    {
        var target = _enemy.perception != null ? _enemy.perception.Target : null;
        if (target == null) return NodeState.Failure;
        float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
        return dist <= _enemy.minAttackDistance ? NodeState.Success : NodeState.Failure;
    }
}

/// <summary>
/// 警戒中？听到声音但是还没看到目标
/// </summary>
public class IsAlertCondition : BTNode
{
    private readonly EnemyBase _enemy;
    public IsAlertCondition(EnemyBase enemy)
    {
        _enemy = enemy;
        NodeName = "警戒中?";
    }
    public override NodeState Evaluate()
    {
        return _enemy.perception != null && _enemy.perception.IsAlert
            ? NodeState.Success : NodeState.Failure;
    }
}

/// <summary>
/// 受击硬直中？
/// </summary>
public class IsHitCondition : BTNode
{
    private readonly EnemyBase _enemy;
    public IsHitCondition(EnemyBase enemy)
    {
        _enemy = enemy;
        NodeName = "受击中?";
    }

    public override NodeState Evaluate()
    {
        return _enemy.currentPhase == EnemyPhase.Hit ? NodeState.Success : NodeState.Failure;
    }
}

/// <summary>
/// 血量低于比例？
/// </summary>
public class LowHealthCondition : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _ratio;
    public LowHealthCondition(EnemyBase enemy, float ratio = 0.3f)
    {
        _enemy = enemy;
        _ratio = ratio;
        NodeName = "低血量?";
    }
    public override NodeState Evaluate() => _enemy.HealthRatio <= _ratio ? NodeState.Success : NodeState.Failure;
}

/// <summary>
/// 玩家距离条件 max/min传-1表示不限
/// </summary>
public class PlayerDistanceCondition : BTNode
{
    private readonly EnemyBase _enemy;
    private readonly float _minDistance;
    private readonly float _maxDistance;

    public PlayerDistanceCondition(EnemyBase enemy, float minDistance = -1f, float maxDistance = -1f)
    {
        _enemy = enemy;
        _minDistance = minDistance;
        _maxDistance = maxDistance;
        NodeName = "玩家距离?";
    }

    public override NodeState Evaluate()
    {
        var target = _enemy.perception != null ? _enemy.perception.Target : null;
        if (target == null) return NodeState.Failure;

        float dist = Vector3.Distance(_enemy.transform.position, target.transform.position);
        if (_minDistance >= 0f && dist < _minDistance) return NodeState.Failure;
        if (_maxDistance >= 0f && dist > _maxDistance) return NodeState.Failure;
        return NodeState.Success;
    }
}