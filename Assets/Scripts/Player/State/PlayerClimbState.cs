using UnityEngine;

/// <summary>
/// 玩家攀爬状态(原神式): 贴墙持续攀爬, WASD控制上下左右, 持续消耗体力
/// 体力耗尽/松开E/按跳跃 -> 从墙上掉落; 爬到墙顶 -> 自动翻上去
/// </summary>
public class PlayerClimbState : PlayerStateBase
{
    private static readonly int ClimbHash = Animator.StringToHash("Climb");

    private float climbSpeed = 2f;     //攀爬速度(米/秒)
    private float staminaCost = 8f;    //攀爬每秒消耗体力
    private float wallDistance = 0.7f; //贴墙距离

    private Vector3 wallNormal;    //墙面法线(水平方向, 指向玩家)
    private bool finishing;        //是否在翻越收尾
    private float finishTimer;     //翻越剩余时间
    private Vector3 finishStart;   //翻越起始位置
    private Vector3 finishTarget;  //翻越目标位置

    public override void Enter()
    {
        base.Enter();
        playerModel.animtor.SetBool(ClimbHash, true);
        playerModel.verticalSpeed = 0;//攀爬时关闭重力

        //检测墙面: 获取法线并面向墙
        Vector3 origin = playerModel.transform.position + Vector3.up * 1.0f;
        if (Physics.Raycast(origin, playerModel.transform.forward, out RaycastHit hit, 2f))
        {
            wallNormal = hit.normal;
            wallNormal.y = 0;
            wallNormal.Normalize();
            //面向墙(法线反方向)
            if (wallNormal.sqrMagnitude > 0.01f)
                playerModel.transform.rotation = Quaternion.LookRotation(-wallNormal);
        }

        finishing = false;
    }

    public override void Exit()
    {
        playerModel.animtor.SetBool(ClimbHash, false);
        base.Exit();
    }

    public override void Update()
    {
        //不调用base.Update: 攀爬期间关闭重力/瞄准切换
        if (!IsBeControl()) return;

        //翻越收尾: 平滑移动到墙顶
        if (finishing)
        {
            finishTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(finishTimer / 0.4f);
            playerModel.transform.position = Vector3.Lerp(finishStart, finishTarget, t);
            if (finishTimer <= 0f)
            {
                playerModel.SwitchState(playerController.moveIput.magnitude > 0 ? PlayerState.Move : PlayerState.Idle);
            }
            return;
        }

        //松开E 或 按跳跃 -> 掉落
        if (!playerController.isClimb || playerController.isJumping)
        {
            FallOffWall();
            return;
        }

        //体力耗尽 -> 掉落
        if (GameManager.INSTANCE != null && !GameManager.INSTANCE.ConsumeStamina(staminaCost * Time.deltaTime))
        {
            FallOffWall();
            return;
        }

        //检测墙: 胸口高度和头顶高度
        Vector3 chest = playerModel.transform.position + Vector3.up * 1.0f;
        bool hasChestWall = Physics.Raycast(chest, playerModel.transform.forward, out RaycastHit chestHit, 1.5f);
        Vector3 head = playerModel.transform.position + Vector3.up * 2.0f;
        bool hasHeadWall = Physics.Raycast(head, playerModel.transform.forward, 1.2f);

        //胸口和头顶都没墙 -> 已经离开墙面, 掉落
        if (!hasChestWall && !hasHeadWall)
        {
            FallOffWall();
            return;
        }
        //头顶没有墙 -> 到墙顶了, 开始翻越
        if (!hasHeadWall)
        {
            StartFinish(chestHit);
            return;
        }

        //保持贴墙
        if (chestHit.distance > wallDistance)
        {
            playerModel.cc.Move(playerModel.transform.forward * (chestHit.distance - wallDistance) * 5f * Time.deltaTime);
        }

        //WASD沿墙移动: W/S上下, A/D沿墙面左右
        Vector3 right = Vector3.Cross(-wallNormal, Vector3.up).normalized;//沿墙向右
        Vector3 moveDir = Vector3.up * playerController.moveIput.y + right * playerController.moveIput.x;
        playerModel.cc.Move(moveDir * climbSpeed * Time.deltaTime);
    }

    /// <summary>从墙上掉落(进入悬空状态, 靠重力落下)</summary>
    private void FallOffWall()
    {
        playerModel.SwitchState(PlayerState.Hover);
    }

    /// <summary>开始翻越: 平滑抬到墙顶并向前挪到墙面上</summary>
    private void StartFinish(RaycastHit chestHit)
    {
        finishing = true;
        finishTimer = 0.4f;
        finishStart = playerModel.transform.position;

        //找墙顶高度: 从墙面上方往下扫
        float topY = playerModel.transform.position.y + 1f;//兜底: 默认高1米
        if (Physics.Raycast(chestHit.point + Vector3.up * 3f, Vector3.down, out RaycastHit topHit, 6f))
        {
            topY = topHit.point.y;
        }

        //目标: 站到墙顶, 并向前挪1.2米(站上墙面)
        finishTarget = new Vector3(playerModel.transform.position.x, topY, playerModel.transform.position.z)
                       + playerModel.transform.forward * 1.2f;
    }
}
