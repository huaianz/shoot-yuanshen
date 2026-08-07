using UnityEngine;

/// <summary>
/// 远程丢石头:播放 Attack01 动画,石头由动画事件在挥手帧生成;后摇→冷却
/// </summary>
public class RangedAttackAction : BTNode
{
    private readonly StoneGolem _golem;
    private readonly float _attackDuration;  // Attack01 动画时长
    private readonly float _recovery;        // 后摇
    private readonly float _cooldown;        // 射击间隔
    private float _phaseEndTime;
    private float _nextAttackTime;
    private int _phase; // 0=等待/冷却, 1=攻击动画, 2=后摇

    public RangedAttackAction(StoneGolem golem, float attackDuration = 1.5f, float recovery = 0.3f, float cooldown = 2f)
    {
        _golem = golem;
        _attackDuration = attackDuration;
        _recovery = recovery;
        _cooldown = cooldown;
        NodeName = "丢石头";
    }

    public override NodeState Evaluate()
    {
        var target = _golem.perception != null ? _golem.perception.Target : null;
        if (target == null) return NodeState.Failure;

        _golem.currentPhase = EnemyPhase.Combat;
        _golem.navMeshAgent.isStopped = true;
        ChaseAction.FaceTarget(_golem, target.transform.position);

        if (_phase == 0)
        {
            if (Time.time < _nextAttackTime)
            {
                _golem.PlayAnimationOnce("Idle");
                return NodeState.Running;
            }

            _phase = 1;
            _phaseEndTime = Time.time + _attackDuration;
            _golem.ResetAnimationOnceCache();
            _golem.animator.CrossFadeInFixedTime("Attack01", 0.1f);
            // 石头不在这里生成——由动画事件 OnThrowRock 在挥手帧生成
            return NodeState.Running;
        }

        if (_phase == 1 && Time.time >= _phaseEndTime)
        {
            _phase = 2;
            _phaseEndTime = Time.time + _recovery;
            return NodeState.Running;
        }

        if (_phase == 2 && Time.time >= _phaseEndTime)
        {
            _phase = 0;
            _nextAttackTime = Time.time + _cooldown;
            _golem.ResetAnimationOnceCache();
            return NodeState.Success;
        }

        return NodeState.Running;
    }
}