using UnityEngine;

/// <summary>
/// 玩家下蹲状态: 按住下蹲键进入, 松开退出
/// 蹲下时可以慢速移动(动画用Crouch混合树: 不动<->蹲走)
/// </summary>
public class PlayerCrouchState : PlayerStateBase
{
    private static readonly int CrouchHash = Animator.StringToHash("Crouch");
    private static readonly int MoveBlendHash = Animator.StringToHash("MoveBlend");

    private float crouchMoveSpeed = 2.1f; //下蹲移动速度(米/秒), 想调快慢就改这个
    private float moveBlend;              //蹲走混合值(0=不动, 1=蹲走)
    private float transitionSpeed = 13;    //混合过渡速度

    public override void Enter()
    {
        base.Enter();
        playerModel.animtor.SetBool(CrouchHash, true);
        moveBlend = 0;
    }

    public override void Exit()
    {
        //任何方式退出下蹲都要把动画参数复位
        playerModel.animtor.SetBool(CrouchHash, false);
        base.Exit();
    }

    public override void Update()
    {
        base.Update();//保留重力/瞄准监听

        if (!IsBeControl()) return;

        bool moving = playerController.moveIput.magnitude > 0.1f;//是否有移动输入

        //蹲着移动: 有输入就慢慢走
        if (moving)
        {
            //旋转到移动方向(和站立移动同一个算法)
            float rad = Mathf.Atan2(playerController.localMovement.x, playerController.localMovement.z);
            playerModel.transform.Rotate(0, rad * playerController.rotationSpeed * Time.deltaTime, 0);

            //用CharacterController推动位移(蹲走动画本身不带位移)
            playerModel.cc.Move(playerController.worldMovement * crouchMoveSpeed * Time.deltaTime);
        }

        //动画混合: 蹲着不动 <-> 蹲着走, 平滑过渡
        moveBlend = Mathf.Lerp(moveBlend, moving ? 1 : 0, transitionSpeed * Time.deltaTime);
        playerModel.animtor.SetFloat(MoveBlendHash, moveBlend);

        //松开下蹲键 -> 退出(有移动输入回移动状态, 否则回待机)
        if (!playerController.isCrouch)
        {
            playerModel.SwitchState(playerController.moveIput.magnitude > 0 ? PlayerState.Move : PlayerState.Idle);
        }
    }
}