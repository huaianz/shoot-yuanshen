using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 玩家子弹
/// </summary>
public class PlayerWeaponBullet : MonoBehaviour
{
    [Tooltip("伤害")]
    public int damage = 10;
    [Tooltip("暴击率(0~1)")]
    public float critRate;
    [Tooltip("暴击伤害加成(0.5=1.5倍)")]
    public float critDamage;
    [HideInInspector]
    public Rigidbody rb;
    [Tooltip("推力")]
    public float flyPower = 30f;
    [Tooltip("子弹存活时间")]
    public float lifeTime = 7f;

    [Header("远端表现")]
    [HideInInspector] public bool visualOnly;          // 纯视觉: 不检测碰撞, 不结算伤害
    [HideInInspector] public float maxDistance = -1f;  // 视觉子弹的飞行距离(到服务器给的落点)
    [HideInInspector] public System.Action<PlayerWeaponBullet> onFinished; // 回池回调

    [Tooltip("射线检测图层")]
    public LayerMask hitLayer = ~0;

    [HideInInspector]
    public BulletPool pool;

    private Vector3 _prevPosition;

    //复用缓冲
    private static readonly Collider[] _overlapBuffer = new Collider[8];
    private float _aliveTime;
    private float _travelled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _aliveTime = 0f;
        _travelled = 0f;
    }

    private void OnDisable()
    {
        //回池时清零
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// 发射(由 BulletPool 调用)
    /// </summary>
    public void Launch(Vector3 direction)
    {
        rb.velocity = direction * flyPower;
        _prevPosition = transform.position;
        _aliveTime = 0f;
        _travelled = 0f;

        if (!visualOnly) CheckInitialOverlap();
    }

    private void Update()
    {
        _aliveTime += Time.deltaTime;
        if (_aliveTime >= lifeTime)
        {
            Finish();
            return;
        }

        // 远端视觉子弹: 只飞行, 不做任何碰撞/伤害判定
        if (visualOnly)
        {
            _travelled += Vector3.Distance(transform.position, _prevPosition);
            _prevPosition = transform.position;

            if (maxDistance > 0f && _travelled >= maxDistance)
                Finish();

            return;
        }

        CheckCollision();
        _prevPosition = transform.position;
    }

    /// <summary>结束: 有回池回调就交给回调, 否则回默认池或销毁</summary>
    private void Finish()
    {
        if (onFinished != null)
        {
            var callback = onFinished;
            onFinished = null;
            callback(this);
            return;
        }

        if (pool != null)
        {
            pool.ReturnBullet(this);
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 生成时检查是否直接出现在敌人碰撞体内部
    /// </summary>
    private void CheckInitialOverlap()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, 0.1f, _overlapBuffer, hitLayer);
        for (int i = 0; i < count; i++)
        {
            EnemyBase enemy = _overlapBuffer[i].GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.Hurt(this, 1);
                Finish();
                return;
            }
        }
    }

    /// <summary>
    /// 逐帧射线检测,防止子弹穿模;无论打到敌人还是障碍物都回池
    /// </summary>
    private void CheckCollision()
    {
        Vector3 dir = transform.position - _prevPosition;
        float distance = dir.magnitude;

        if (Physics.Raycast(_prevPosition, dir.normalized, out RaycastHit hit, distance, hitLayer))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                enemy?.Hurt(this, 1);
            }
            //命中即回收,不再穿墙继续飞
            Finish();
        }
    }

}
