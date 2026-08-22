using UnityEngine;

/// <summary>
/// 玩家滑铲状态: 移动中按住Shift+Ctrl进入, 松开或速度耗尽退出
/// 动画: Slide_Start -> Slide_Loop(循环) -> Slide_Exit
/// </summary>
public class PlayerSlideState : PlayerStateBase
{
    private static readonly int SlideHash = Animator.StringToHash("Slide");

    private float slideSpeed = 7.5f;      //滑铲初速度(米/秒)
    private float decel = 4f;           //滑铲减速度(每秒减多少)
    private Vector3 slideDir;           //滑铲方向
    private bool exiting;               //是否进入收尾阶段
    private float exitTimer;            //收尾剩余时间
    private bool hasExitDuration;       //是否已读到起身动画时长

    public override void Enter()
    {
        base.Enter();
        playerModel.animtor.SetBool(SlideHash, true);//进入滑铲(Animator: Move->SlideStart)
        playerModel.verticalSpeed = 0;

        //滑铲方向 = 当前移动方向(监听保证进入时正在移动)
        slideDir = playerController.worldMovement.normalized;
        //面朝滑铲方向
        playerModel.transform.rotation = Quaternion.LookRotation(slideDir);

        slideSpeed = 9f;
        exiting = false;
        hasExitDuration = false;
    }

    public override void Exit()
    {
        //任何方式退出都要复位动画参数
        playerModel.animtor.SetBool(SlideHash, false);
        base.Exit();
    }

    public override void Update()
    {
        base.Update();//保留重力/瞄准(瞄准会打断滑铲)

        if (!IsBeControl()) return;

        //贴地吸附: 每帧向下拉一点, 防止滑铲动画让角色看起来飘在空中
        //(CharacterController碰到地面会自动停下, 不会穿地)
        playerModel.cc.Move(Vector3.down * 4f * Time.deltaTime);

        //滑铲位移: 速度持续衰减, 越来越慢
        playerModel.cc.Move(slideDir * slideSpeed * Time.deltaTime);
        slideSpeed = Mathf.Max(0f, slideSpeed - decel * Time.deltaTime);

        if (!exiting)
        {
            //按住Shift+Ctrl且还有速度 -> 继续滑铲(SlideLoop循环)
            bool holding = playerController.isSprint && playerController.isCrouch;
            if (holding && slideSpeed > 0.1f) return;

            //松开按键 或 速度耗尽 -> 收尾(Animator: SlideLoop->SlideExit)
            exiting = true;
            exitTimer = 0.6f;//兜底时长, 读到实际动画时长后会覆盖
            hasExitDuration = false;
            playerModel.animtor.SetBool(SlideHash, false);
        }
        else
        {
            //一旦动画切到SlideExit, 就用它的实际时长, 让起身动作完整播完
            if (!hasExitDuration)
            {
                AnimatorStateInfo info = playerModel.animtor.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("SlideExit") && info.length > 0.01f)
                {
                    exitTimer = info.length;
                    hasExitDuration = true;
                }
            }

            exitTimer -= Time.deltaTime;
            if (exitTimer <= 0)
            {
                playerModel.SwitchState(playerController.moveIput.magnitude > 0 ? PlayerState.Move : PlayerState.Idle);
            }
        }
    }
}
