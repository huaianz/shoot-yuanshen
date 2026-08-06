using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人感知:视野(扇形+距离+遮挡,节流检测)+ 听觉(事件式声源)
/// </summary>
public class EnemyPerception : MonoBehaviour
{
    [Header("视野")]
    public float sightDistance = 15;
    public float sightAngle = 120;

    [Tooltip("检测频率(秒),节流降低开销")]
    public float checkInterval = 0.2f;
    [Tooltip("目标脱离视野后多久解除警戒")]
    public float lostTargetTime = 5f;
    [Tooltip("遮挡检测图层")]
    public LayerMask sightLayer = ~0;

    [Header("听觉")]
    [Tooltip("敌人听觉上限(实际范围取声源半径和它的较小值)")]
    public float hearingRadius = 15f;
    [Tooltip("距离小于 半径×此值 = 完全警觉;边缘 = 只警觉不锁定")]
    public float fullAlertFactor = 0.6f;
    [Header("调试")]
    public bool showGizmos = true;

    /// <summary>
    /// 当前锁定的玩家
    /// </summary>
    public PlayerModel Target;
    // {
    //     get;
    //     private set;
    // }
    /// <summary>
    /// 是否警戒
    /// </summary>
    public bool IsAlert;
    // {
    //     get;
    //     private set;
    // }
    /// <summary>
    /// 最后看到/听到的位置
    /// </summary>
    public Vector3 LastKnownPosition;
    // {
    //     get;
    //     private set;
    // }

    private float _checkTimer;
    private float _lostTimer;

    private void OnEnable()
    {
        //订阅全局声音事件
        EventHandler.SoundEvent += OnSoundEvent;
    }

    private void OnDisable()
    {
        EventHandler.SoundEvent -= OnSoundEvent;
    }

    private void Update()
    {
        //视野按节流频率检测
        _checkTimer -= Time.deltaTime;
        if (_checkTimer <= 0f)
        {
            _checkTimer = checkInterval;
            CheckVision();
        }

        //丢失计时：5秒没看到目标就接触锁定
        if (Target != null)
        {
            _lostTimer += Time.deltaTime;
            if (_lostTimer >= lostTargetTime)
            {
                ClearTarget();
            }
        }
    }

    private void CheckVision()
    {
        if (GameManager.INSTANCE == null)
        {
            return;
        }
        PlayerModel[] players = GameManager.INSTANCE.playerModels;
        if (players == null || players.Length == 0)
        {
            return;
        }
        PlayerModel best = null;
        float bestDist = float.MaxValue;
        foreach (PlayerModel p in players)
        {
            if (p == null)
            {
                continue;
            }

            Vector3 toPlayer = p.transform.position - transform.position;
            float dist = toPlayer.magnitude;

            if (dist > sightDistance)
            {
                continue;
            }
            // 不在扇形内
            if (Vector3.Angle(transform.forward, toPlayer) > sightAngle * 0.5f)
            {
                continue;
            }
            //遮挡检测
            if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hit, dist, sightLayer))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    continue;
                }
            }
            //多个玩家时选最近的
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }

        if (best != null)
        {
            Target = best;
            LastKnownPosition = best.transform.position;
            IsAlert = true;
            _lostTimer = 0f;
        }
        else if (Target != null)
        {
            LastKnownPosition = Target.transform.position;//记住最后看到的位置
        }
    }

    /// <summary>
    /// 听到声音,转向声源,进入警戒;近处完全警觉,边缘只轻微警觉
    /// </summary>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    private void OnSoundEvent(Vector3 position, float radius)
    {
        //实际范围=声源半径和敌人听觉上限的较小值
        float effectiveRadius = Mathf.Min(radius, hearingRadius);
        float dist = Vector3.Distance(transform.position, position);
        if (dist > effectiveRadius) return;//超出范围

        // 转向声源(隔墙也能听到,不做遮挡检测)
        Vector3 dir = position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }


        LastKnownPosition = position;

        //近处完全警戒;边缘只警觉不锁定目标
        IsAlert = dist <= effectiveRadius * fullAlertFactor;
    }

    public void ClearTarget()
    {
        Target = null;
        _lostTimer = 0f;
    }

    public void ClearAlert()
    {
        IsAlert = false;
    }

    //选中敌人时,在 Scene 视图画出视野和听觉范围,方便调参
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Vector3 left = Quaternion.Euler(0, -sightAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, sightAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, left * sightDistance);
        Gizmos.DrawRay(transform.position, right * sightDistance);

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }

}
