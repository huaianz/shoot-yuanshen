using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieAttackState : EnemyStateBase
{
    //缓存数组
    private List<AnimatorClipInfo> _clipInfos = new List<AnimatorClipInfo>(1);
    private float _attackEndTime;
    public override void Enter()
    {
        base.Enter();
        enemyModel.PlayStateAnimation("Attack");
        _clipInfos.Clear();
        enemyModel.animator.GetCurrentAnimatorClipInfo(0, _clipInfos);
        if (_clipInfos.Count > 0)
        {
            _attackEndTime = Time.time + _clipInfos[0].clip.length;
        }
        else
        {
            _attackEndTime = Time.time + 1f; // 读不到就兜底 1 秒
        }
    }

    public override void Update()
    {
        base.Update();

        // 每帧只比较一次时间,比查询动画状态便宜得多
        if (Time.time >= _attackEndTime)
        {
            GameManager.INSTANCE?.ApplyDamageToActiveRole(enemyModel.attackDamage);
            enemyModel.SwitchState(EnemyState.Idle);
        }
    }
}
