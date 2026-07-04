using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人状态基类
/// </summary>
public class EnemyStateBase : StateBase
{

    protected EnemyBase enemyModel;
    public override void Destroy()
    {

    }

    public override void Enter()
    {
        MonoManager.INSTANCE.AddUpdateAction(Update);
    }

    public override void Exit()
    {
        MonoManager.INSTANCE.RemoveUpdateAction(Update);
    }

    public override void Init(IStateMachineOwner owner)
    {
        enemyModel = (EnemyBase)owner;
    }

    public override void Update()
    {

    }

    /// <summary>
    /// 动画是否播放完毕
    /// </summary>
    /// <param name="layer">动画层</param>
    /// <returns></returns>
    protected bool IsAnimationBreak(int layer)
    {
        AnimatorStateInfo Info = enemyModel.animator.GetCurrentAnimatorStateInfo(layer);
        return Info.normalizedTime >= 1f && !enemyModel.animator.IsInTransition(layer);
    }
}
