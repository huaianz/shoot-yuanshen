using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.AI;
public enum PlayerState
{
    Idle,
    Move,
    Hover,
    Aiming
}
/// <summary>
/// 角色模型
/// </summary>
public class PlayerModel : MonoBehaviour, IStateMachineOwner
{
    [Tooltip("角色武器")]
    public PlayerWeapon weapon;

    //[HideInInspector]的作用是隐藏，因为在Awake中有获取组件
    [HideInInspector]
    public Animator animtor;
    [HideInInspector]
    public CharacterController cc;
    private StateMachine stateMachine;//动画状态机
    private PlayerState currentState;//当前状态

    #region 约束相关
    public MultiAimConstraint rightHandAimConstraint;//正常状态下的右手约束
    public TwoBoneIKConstraint rightHandConstraint;//瞄准状态下的右手约束
    public MultiAimConstraint BodyAimConstraint;//身体约束
    #endregion

    #region 重力速度相关
    [Tooltip("重力")]
    public float gravity = -15;
    [Tooltip("跳跃高度")]
    public float jumpHeight = 1.5f;
    [HideInInspector]
    public float verticalSpeed;//当前垂直方向的速度
    [Tooltip("悬空的判断高度")]
    public float fallHeight = 0.2f;
    #endregion

    #region 玩家在地面时前三帧速度的缓存
    private static readonly int CACHE_SIZE = 3;
    Vector3[] speedCache = new Vector3[CACHE_SIZE];//动画前三帧的玩家速度
    private int speedCache_index = 0;//缓存保存的位置
    private Vector3 averageDeltaMovement;//平均速度
    #endregion

    #region 人机相关
    [HideInInspector]
    public NavMeshAgent navMeshAgent;
    public float stoppingDistance = 2f;//停止跟随距离
    #endregion
    private void Awake()
    {
        stateMachine = new StateMachine(this);
        animtor = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = stoppingDistance;
        navMeshAgent.angularSpeed = PlayerController.INSTANCE.rotationSpeed;
    }

    void Start()
    {
        SwitchState(PlayerState.Idle);
        ExitAim();
    }


    void Update()
    {

    }


    /// <summary>
    /// 进入模型
    /// </summary>
    public void Enter()
    {
        navMeshAgent.enabled = false;
    }

    /// <summary>
    /// 退出模型
    /// </summary>
    public void Exit()
    {
        navMeshAgent.enabled = true;
        SwitchState(PlayerState.Idle);
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    public void SwitchState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                stateMachine.EnterState<PlayerIdleState>();
                break;
            case PlayerState.Move:
                stateMachine.EnterState<PlayerMoveState>();
                break;
            case PlayerState.Hover:
                stateMachine.EnterState<PlayerHoverState>();
                break;
            case PlayerState.Aiming:
                stateMachine.EnterState<PlayerAimingState>();
                break;
        }
        currentState = state;
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="animationName">动画名称</param>
    /// <param name="transition">过渡时间</param>
    /// <param name="layer">动画层级</param>
    public void PlayStateAnimation(string animationName, float transition = 0.25f, int layer = 0)
    {
        animtor.CrossFadeInFixedTime(animationName, transition, layer);
    }
    /// <summary>
    /// 是否悬空
    /// </summary>
    /// <returns></returns>
    public bool IsHover()
    {
        return !Physics.Raycast(transform.position, Vector3.down, fallHeight);
    }

    /// <summary>
    /// 计算模型前三帧的平均速度
    /// </summary>
    /// <param name="newSpeed">当前速度</param>
    private void UpdateAverageCacheSpeed(Vector3 newSpeed)
    {
        speedCache[speedCache_index++] = newSpeed;
        speedCache_index %= CACHE_SIZE;
        //计算缓存池中的平均速度
        Vector3 sum = Vector3.zero;
        foreach (Vector3 cache in speedCache)
        {
            sum += cache;
        }
        averageDeltaMovement = sum / CACHE_SIZE;
    }
    private void OnAnimatorMove()
    {
        Vector3 playerDeltaMovement = animtor.deltaPosition;//获取动画控制器当前帧的位置信息
        if (currentState != PlayerState.Hover)
        {
            UpdateAverageCacheSpeed(animtor.velocity);
        }
        else
        {
            playerDeltaMovement = averageDeltaMovement * Time.deltaTime;
        }
        playerDeltaMovement.y = verticalSpeed * Time.deltaTime;
        cc.Move(playerDeltaMovement);
    }

    /// <summary>
    /// 进入瞄准
    /// </summary>
    public void EnterAim()
    {
        //启动瞄准约束
        rightHandAimConstraint.weight = 1;
        BodyAimConstraint.weight = 1;
        rightHandConstraint.weight = 0;
    }

    /// <summary>
    /// 退出瞄准
    /// </summary>
    public void ExitAim()
    {
        rightHandAimConstraint.weight = 0;
        BodyAimConstraint.weight = 0;
        rightHandConstraint.weight = 1;
    }

    /// <summary>
    /// 计算该模型与玩家当前所控制的模型的距离
    /// </summary>
    /// <returns></returns>

    public float DistanceOfCurrentPlayerModel()
    {
        return Vector3.Distance(transform.position, PlayerController.INSTANCE.currentPlayerModel.transform.position);
    }
}
