using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeHilichurl : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();
        navMeshAgent.speed = stats.patrolSpeed;
        BuildTree();
    }

    //行为树不用旧的状态机，这里留空就行
    public override void SwitchState(EnemyState state)
    {

    }

    public override void Hurt(PlayerWeaponBullet bullet, float damageMultiplier = 1)
    {
        base.Hurt(bullet, damageMultiplier);
        if (!IsDead)
        {
            //被打进硬直
            currentPhase = EnemyPhase.Hit;
            ResetAnimationOnceCache();
        }
    }

    private void BuildTree()
    {
        behaviorTree = new Selector(
            // 1. 死亡
            new Sequence(new IsDeadCondition(this), new DeathAction(this)),

            // 2. 受击硬直
            new Sequence(new IsHitCondition(this), new HitAction(this, 0.3f)),

            // 3. 低血量犹豫
            new Sequence(new LowHealthCondition(this, 0.3f), new HesitateAction(this, 0.8f, 5f)),
            // 4. 战斗，先攻击,攻击不了就追击
            new Sequence(new HasTargetCondition(this),
                new Selector(
                    new Sequence(new InAttackRangeCondition(this), new MeleeAttackAction(this)),
                    new ChaseAction(this)
                )
            ),

            // 5. 警戒:转向最后听到的位置
            new Sequence(new IsAlertCondition(this), new FaceLastKnownAction(this)),

            // 6. 巡逻
            new PatrolAction(this)
        );
    }
}
