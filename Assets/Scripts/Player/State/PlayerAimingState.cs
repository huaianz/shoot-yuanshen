using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄准状态
/// </summary>
public class PlayerAimingState : PlayerStateBase
{
    #region 动画器相关
    private int aimingXHash;
    private int aimingYHash;
    private float aimingX=0;
    private float aimingY=0;
    private float transitionSpeed=5;
    #endregion

    public override void Init(IStateMachineOwner owner)
    {
        base.Init(owner);
        aimingXHash=Animator.StringToHash("AimingX");
        aimingYHash=Animator.StringToHash("AimingY");
    }
    public override void Enter()
    {
        base.Enter();
        playerModel.PlayStateAnimation("Aiming");
    }

    public override void Update()
    {
        base.Update();

        #region 待机监听
        if (!playerController.isAiming)
        {
            playerModel.SwitchState(PlayerState.Idle);
        }
        #endregion
        
        #region 处理移动输入
        aimingX=Mathf.Lerp(aimingX,playerController.moveIput.x,transitionSpeed*Time.deltaTime);
        aimingY=Mathf.Lerp(aimingY,playerController.moveIput.y,transitionSpeed*Time.deltaTime);
        playerModel.animtor.SetFloat(aimingXHash,aimingX);
        playerModel.animtor.SetFloat(aimingYHash,aimingY);
        #endregion
    }
}
