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
