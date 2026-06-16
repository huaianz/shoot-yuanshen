using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家状态基类
/// </summary>
public class PlayerStateBase : StateBase
{
    protected PlayerController playerController;
    protected PlayerModel playerModel;//当前状态的角色模型
    public override void Destroy()
    {
        
    }

    public override void Enter()
    {
        
    }

    public override void Exit()
    {
        
    }

    public override void Init(IStateMachineOwner owner)
    {
        playerController=PlayerController.INSTANCE;
        playerModel=(PlayerModel) owner;
    }
}
