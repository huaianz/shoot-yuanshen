using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家待机状态
/// </summary>
public class PlayerIdleState : PlayerStateBase
{
    public override void Enter()
    {
        base.Enter();
        playerModel.PlayStateAnimation("Idle");
    }

    public override void Update()
    {
        base.Update();
        if (IsBeControl())
        {
            #region 移动状态监听
            if (playerController.moveIput.magnitude != 0)
                playerModel.SwitchState(PlayerState.Move);
            #endregion

            #region 悬空状态监听
            if (playerController.isJumping)
                SwithToHover();
            #endregion

            //闪避监听(需要消耗体力, 体力不足无法闪避)
            if (playerController.isDodge && GameManager.INSTANCE != null &&
                GameManager.INSTANCE.TryConsumeStamina(GameManager.INSTANCE.dodgeStaminaCost))
            {
                playerModel.SwitchState(PlayerState.Dodge);
                return;
            }
            //攀爬监听: 按E且面前有墙
            if (playerController.isClimb && IsClimbableWall())
            {
                playerModel.SwitchState(PlayerState.Climb);
                return;
            }
            //下蹲监听
            if (playerController.isCrouch)
            {
                playerModel.SwitchState(PlayerState.Crouch);
                return;
            }
        }
        //人机模式
        else
        {
            if (playerModel.DistanceOfCurrentPlayerModel() > playerModel.stoppingDistance)
            {
                playerModel.SwitchState(PlayerState.Move);
            }
        }
    }
}
