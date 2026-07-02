using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieMoveState : EnemyStateBase
{
    public override void Enter()
    {
        base.Enter();
        enemyModel.PlayStateAnimation("Move");
        enemyModel.navMeshAgent.enabled = true;
    }
    public override void Update()
    {
        base.Update();
        if (!enemyModel.IsAttackTargetInAttackRange())
        {
            enemyModel.chaseTarget();
        }
        else
        {
            enemyModel.SwitchState(EnemyState.Idle);
        }
    }
}
