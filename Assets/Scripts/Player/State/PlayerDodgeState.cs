using UnityEngine;

/// <summary>
/// 玩家闪避状态: 触发翻滚动画, 朝移动方向冲刺, 期间无敌
/// </summary>
public class PlayerDodgeState : PlayerStateBase
{
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");

    private float dodgeSpeed = 8f;      //闪避初速度(米/秒), 想冲更远就调大
    private float dodgeDuration = 0.7f; //默认时长(秒), 进状态后会优先用动画实际时长

    private float timer;         //剩余时间
    private float totalTime;     //总时长(用于速度衰减计算)
    private Vector3 dodgeDir;    //闪避方向
    private bool hasReadDuration;//是否已从动画读取实际时长

    public override void Enter()
    {
        base.Enter();
        playerModel.animtor.SetTrigger(DodgeHash);//触发翻滚动画
        playerModel.verticalSpeed = 0;//闪避期间不受重力影响

        //闪避方向 = 当前移动方向; 没有输入就朝角色面朝方向
        Vector3 dir = playerController.worldMovement;
        dodgeDir = (dir.magnitude > 0.01f ? dir : playerModel.transform.forward).normalized;

        //转身面向闪避方向(翻滚是朝正面滚的)
        playerModel.transform.rotation = Quaternion.LookRotation(dodgeDir);

        //开启无敌帧: 闪避期间不受伤害
        GameManager.INSTANCE?.SetActiveRoleInvincible(dodgeDuration);

        timer = dodgeDuration;
        totalTime = dodgeDuration;
        hasReadDuration = false;
    }

    public override void Update()
    {
        //不调用base.Update: 闪避期间不响应瞄准切换, 保证动画不被打断
        if (!IsBeControl()) return;

        //第一次Update时读取动画实际时长, 比写死的更准(和你的Roll动画长度对齐)
        if (!hasReadDuration)
        {
            AnimatorStateInfo info = playerModel.animtor.GetCurrentAnimatorStateInfo(0);
            if (info.IsName("Dodge") && info.length > 0.01f)
            {
                dodgeDuration = info.length;
                totalTime = dodgeDuration;
                timer = dodgeDuration;
                GameManager.INSTANCE?.SetActiveRoleInvincible(dodgeDuration);//无敌时间跟动画走
                hasReadDuration = true;
            }
        }

        //速度衰减: 开头快、结尾慢, 有"冲出去再收住"的手感
        float t = totalTime > 0 ? timer / totalTime : 0;
        float speed = dodgeSpeed * t;
        playerModel.cc.Move(dodgeDir * speed * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            playerModel.SwitchState(playerController.moveIput.magnitude > 0 ? PlayerState.Move : PlayerState.Idle);
        }
    }
}
