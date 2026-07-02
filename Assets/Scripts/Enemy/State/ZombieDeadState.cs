using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieDeadState : EnemyStateBase
{
    public override void Enter()
    {
        base.Enter();
        enemyModel.PlayStateAnimation("Dead");
    }
}
