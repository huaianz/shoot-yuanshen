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

    #region 流血相关预制体
    [Tooltip("喷血溅射特效")]
    public GameObject bloodSmashPrefab;
    [Tooltip("滴血特效")]
    public GameObject bloodDrippingPrefab;
    #endregion

    #region 受击相关
    protected int hitHash;
    protected int moveSpeedHash;
    protected float normalMoveSpeed = 1;
    protected float slowMoveSpeed = 0.5f;
    protected Coroutine recoverSpeedCoroutine;//恢复速度的协程
    #endregion

    #region 血条相关
    [Tooltip("生命值")]
    public int health = 100;
    private float currentHealth;
    private bool isDead = false;
    [Tooltip("血条预制体")]
    public GameObject healthBarPrefab;
    [Tooltip("血条的位置")]
    public Transform healthBarPos;
    [HideInInspector]
    public GameObject healthBar;//实例化后的血条
    [Tooltip("血条框显示时间")]
    public float healthBarShowTime = 4f;
    private float healthBarShow_timer;
    #endregion

    #region 攻击力
    [Tooltip("攻击力")]
    public int attackDamage = 10;
    #endregion

    #region 行为树支持(新敌人使用,旧僵尸不受影响)
    [Header("行为树敌人属性")]
    public EnemyStats stats = new EnemyStats();
    [HideInInspector]
    public EnemyPhase currentPhase = EnemyPhase.Patrol;
    [HideInInspector]
    public EnemyPerception perception;
    protected BTNode behaviorTree;
    protected Vector3 spawnPoint;
    public bool IsDead => isDead;
    public float HealthRatio => health > 0 ? currentHealth / health : 0f;
    #endregion
    private string _lastPlayedAnim = "";

    protected virtual void Awake()
    {
        stateMachine = new StateMachine(this);
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.stoppingDistance = minAttackDistance;
        navMeshAgent.angularSpeed = rotationSpeed;

        hitHash = Animator.StringToHash("Hit");
        moveSpeedHash = Animator.StringToHash("MoveSpeed");
        currentHealth = health;
        healthBarShow_timer = healthBarShowTime;

        perception = GetComponent<EnemyPerception>();
        spawnPoint = transform.position;
    }

    protected virtual void Start()
    {
        SwitchState(EnemyState.Idle);
        FindAttackTarget();
        #region 实例化血条框
        if (healthBarPrefab != null)
        {
            healthBar = Instantiate(healthBarPrefab, healthBarPos.position, Quaternion.identity);
            healthBar.transform.SetParent(UIManager.INSTANCE.WorldSpaceCanvas.transform);
        }
        #endregion
    }

    protected virtual void Update()
    {
        //行为树驱动
        if (behaviorTree != null)
        {
            behaviorTree.Evaluate();
        }

        if (isDead)
            return;
        #region 血条框显示
        if (healthBar != null)
        {
            if (healthBarShow_timer < healthBarShowTime)
            {
                healthBar.SetActive(true);
                healthBar.transform.position = healthBarPos.position;
                healthBarShow_timer += Time.deltaTime;
            }
            else
            {
                healthBar.SetActive(false);
            }
        }
        #endregion

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
    /// 减慢移动动画播放速度，持续一段时间后恢复
    /// </summary>
    protected virtual void SlowMoveAnimation()
    {
        animator.SetFloat("MoveSpeed", slowMoveSpeed);
        if (recoverSpeedCoroutine != null)
        {
            StopCoroutine(recoverSpeedCoroutine);
        }
        recoverSpeedCoroutine = StartCoroutine(RecoverMoveSpeed(0.5f));

    }

    protected IEnumerator RecoverMoveSpeed(float delay)
    {
        //等待指定时间
        yield return new WaitForSeconds(delay);
        //恢复移动动画播放速度
        animator.SetFloat("MoveSpeed", normalMoveSpeed);
        recoverSpeedCoroutine = null;
    }

    /// <summary>
    /// 受击
    /// </summary>
    /// <param name="bullet">玩家子弹</param>
    /// <param name="damageMultiplier">伤害倍率</param>
    public virtual void Hurt(PlayerWeaponBullet bullet, float damageMultiplier = 1)
    {
        #region 受击动画相关
        animator.SetTrigger(hitHash);
        SlowMoveAnimation();
        #endregion
        #region 生成喷血特效
        //计算子弹的方向
        Vector3 bulletDir = bullet.transform.forward;
        //根据子弹的方向计算旋转
        Quaternion rotation = Quaternion.LookRotation(-bulletDir);
        //生成喷血特效
        Destroy(Instantiate(bloodSmashPrefab, bullet.transform.position, rotation), 3);
        #endregion

        #region 生成流血滴落特效
        Destroy(Instantiate(bloodDrippingPrefab, transform.position + Vector3.up * 0.1f, Quaternion.Euler(0, 0, 0)), 3);
        #endregion

        #region 血条相关
        currentHealth -= bullet.damage * damageMultiplier;
        if (currentHealth > 0)
        {
            healthBarShow_timer = 0;
            healthBar.GetComponent<EnemyHealthBarUI>().UpdateHealthBar(currentHealth / health);
        }
        else
        {
            SwitchState(EnemyState.Dead);
            navMeshAgent.enabled = false;
            GetComponent<BoxCollider>().enabled = false;
            currentHealth = 0;
            isDead = true;
            Destroy(healthBar);//销毁血条
        }
        #endregion
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

    /// <summary>
    /// 只在动画切换时播放一次(避免每帧重复 CrossFade 导致卡动画/滑冰)
    /// </summary>
    public void PlayAnimationOnce(string animName, float transition = 0.25f)
    {
        if (_lastPlayedAnim == animName) return;
        _lastPlayedAnim = animName;
        PlayStateAnimation(animName, transition);
    }

    /// <summary>
    /// 清除动画缓存:被攻击/攻击/死亡打断后,下一个动作要能重新播放自己的动画
    /// </summary>
    public void ResetAnimationOnceCache()
    {
        _lastPlayedAnim = "";
    }

    /// <summary>
    /// 设置移动动画速度参数(和僵尸一致的 MoveSpeed)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        animator.SetFloat(moveSpeedHash, speed);
    }

    /// <summary>
    /// 销毁敌人
    /// </summary>
    public void Clear()
    {
        stateMachine.Stop();
        Destroy(gameObject);
    }

    //便捷属性
    public Vector3 SpawnPoint => spawnPoint;
    public bool HasTarget => perception != null && perception.Target != null;
}
