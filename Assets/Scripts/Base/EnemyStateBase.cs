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
}
