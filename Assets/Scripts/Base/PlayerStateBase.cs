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
        playerController = PlayerController.INSTANCE;
        playerModel = (PlayerModel)owner;
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
        if (!playerModel.cc.isGrounded)//角色模型不在地面
        {
            playerModel.verticalSpeed += playerModel.gravity * Time.deltaTime;//施加重力
            if (playerModel.IsHover())
                playerModel.SwitchState(PlayerState.Hover);
        }
        else // 模型在地面
            playerModel.verticalSpeed = playerModel.gravity * Time.deltaTime;//重置垂直速度
        #endregion

        #region 瞄准状态监听
        if (IsBeControl() && (playerController.isAiming || playerController.isFire))
        {
            playerModel.SwitchState(PlayerState.Aiming);
        }

        #endregion

        #region 体力恢复
        //在地面且没有移动输入时恢复体力(奔跑/攀爬都会消耗体力)
        if (playerModel.cc.isGrounded && playerController.moveIput.magnitude <= 0.01f)
        {
            GameManager.INSTANCE?.RecoverStamina(10f * Time.deltaTime);
        }
        #endregion

    }

    /// <summary>
    /// 当前的模型是否被玩家所控制
    /// </summary>
    /// <returns></returns>
    public bool IsBeControl()
    {
        return playerModel == playerController.currentPlayerModel;
    }

    /// <summary>
    /// 切换到跳跃状态
    /// </summary>
    public void SwithToHover()
    {
        //计算跳跃力度
        playerModel.verticalSpeed = Mathf.Sqrt(-2 * playerModel.gravity * playerModel.jumpHeight);
        //切换到悬空状态
        playerModel.SwitchState(PlayerState.Hover);
    }

    /// <summary>
    /// 检测面前是否是可攀爬的墙: 站在地面、距离够近、墙至少0.8米高
    /// </summary>
    public bool IsClimbableWall()
    {
        //1. 必须站在地面才能爬
        if (!playerModel.cc.isGrounded) return false;

        //2. 胸口高度向前要有墙(距离1.5米内)
        Vector3 chest = playerModel.transform.position + Vector3.up * 1.0f;
        if (!Physics.Raycast(chest, playerModel.transform.forward, out _, 1.5f)) return false;

        //3. 墙至少0.8米高: 0.8米高度也要能碰到墙(矮台阶直接跳过去, 不让爬)
        Vector3 low = playerModel.transform.position + Vector3.up * 0.8f;
        return Physics.Raycast(low, playerModel.transform.forward, 1.5f);
    }
}
