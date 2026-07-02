using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Idle,
    Move,
    Attack,
    Dead
}

/// <summary>
/// 敌人基类
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IStateMachineOwner
{
    [HideInInspector]
    public Animator animator;
    protected StateMachine stateMachine;

    #region 寻路相关
    [HideInInspector]
    public NavMeshAgent navMeshAgent;//寻路代理
    [Tooltip("转向速度")]
    public float rotationSpeed = 300f;
    [Tooltip("最小攻击距离")]
    public float minAttackDistance = 1f;
    [HideInInspector]
    public PlayerModel attackTarget;//攻击目标
    #endregion
    protected virtual void Awake()
    {
        stateMachine = new StateMachine(this);
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = minAttackDistance;
        navMeshAgent.angularSpeed = rotationSpeed;
    }

    protected virtual void Start()
    {
        SwitchState(EnemyState.Idle);
        FindAttackTarget();
    }

    /// <summary>
    /// 寻找离自身最近的PlayerModel
    /// </summary>
    public virtual void FindAttackTarget()
    {
        PlayerModel[] playerModels = GameManager.INSTANCE.playerModels;
        if (playerModels != null && playerModels.Length > 0)
        {
            PlayerModel closestPlayer = null;
            float minDistance = float.MaxValue;
            foreach (PlayerModel player in playerModels)
            {
                if (player != null)
                {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPlayer = player;
                    }
                }
            }
            //设置攻击目标
            attackTarget = closestPlayer;
        }
    }

    /// <summary>
    /// 是否存在攻击目标
    /// </summary>
    /// <returns></returns>
    public virtual bool HasAttackTarget()
    {
        return attackTarget != null;
    }

    /// <summary>
    /// 攻击目标是否在最短攻击范围内
    /// </summary>
    /// <returns></returns>
    public virtual bool IsAttackTargetInAttackRange()
    {
        if (HasAttackTarget())
        {
            return Vector3.Distance(transform.position, attackTarget.transform.position) <= minAttackDistance;
        }
        return false;
    }

    /// <summary>
    /// 追击目标
    /// </summary>
    public virtual void chaseTarget()
    {
        if (HasAttackTarget())
        {
            navMeshAgent.SetDestination(attackTarget.transform.position);
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="state"></param>
    public abstract void SwitchState(EnemyState state);

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="animationName">动画名称</param>
    /// <param name="transition">过渡时间</param>
    /// <param name="layer">动画层级</param>
    public void PlayStateAnimation(string animationName, float transition = 0.25f, int layer = 0)
    {
        animator.CrossFadeInFixedTime(animationName, transition, layer);
    }
}
