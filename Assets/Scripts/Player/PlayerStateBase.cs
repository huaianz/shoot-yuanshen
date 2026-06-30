using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// 玩家状态基类
/// </summary>
public class PlayerStateBase : StateBase
{
    protected PlayerController playerController;
    protected PlayerModel playerModel;//当前状态的角色模型
    
    public override void Init(IStateMachineOwner owner)
    {
        playerController=PlayerController.INSTANCE;
        playerModel=(PlayerModel) owner;
    }
    
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

    public override void Update()
    {
        #region 重力计算
        if(!playerModel.cc.isGrounded)//角色模型不在地面
        {
            playerModel.verticalSpeed+=playerModel.gravity*Time.deltaTime;//施加重力
            if(playerModel.IsHover())
            playerModel.SwitchState(PlayerState.Hover);
        }
        else // 模型在地面
            playerModel.verticalSpeed=playerModel.gravity*Time.deltaTime;//重置垂直速度
        #endregion

        #region 瞄准状态监听
        if (playerController.isAiming)
        {
            playerModel.SwitchState(PlayerState.Aiming);
        }

        #endregion
    
    }

    /// <summary>
    /// 当前的模型是否被玩家所控制
    /// </summary>
    /// <returns></returns>
    public bool IsBeControl()
    {
        return playerModel==playerController.currentPlayerModel;
    }

    /// <summary>
    /// 切换到跳跃状态
    /// </summary>
    public void SwithToHover()
    {
        //计算跳跃力度
        playerModel.verticalSpeed=Mathf.Sqrt(-2*playerModel.gravity*playerModel.jumpHeight);
        //切换到悬空状态
        playerModel.SwitchState(PlayerState.Hover);
    }
}
