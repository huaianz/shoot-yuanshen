using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子弹/火花对象池
/// </summary>
public class BulletPool : MonoBehaviour
{
    [Header("预制体")]
    public PlayerWeaponBullet bulletPrefab;
    public GameObject sparkPrefab;

    [Header("预生成数量")]
    public int prewarmBullets = 40;
    //火花特效
    public int prewarmSparks = 10;

    [Tooltip("火花特效自动回池时间")]
    public float sparkLifeTime = 1.5f;

    [Tooltip("池对象存放的父物体")]
    public Transform storageParent;

    private Transform _container;

    private readonly Queue<PlayerWeaponBullet> _bullets = new Queue<PlayerWeaponBullet>();
    private readonly Queue<GameObject> _sparks = new Queue<GameObject>();

    private void Awake()
    {

        if (storageParent != null)
        {
            _container = storageParent;
        }
        else
        {
            var go = new GameObject("BulletPoolContainer");
            _container = go.transform;
            _container.SetParent(transform, false);
        }

        //预生成子弹
        for (int i = 0; i < prewarmBullets; i++)
        {
            var bullet = Instantiate(bulletPrefab, _container);
            bullet.pool = this;               // 告诉子弹它的池是谁
            bullet.gameObject.SetActive(false);
            _bullets.Enqueue(bullet);
        }

        // 预生成火花
        for (int i = 0; i < prewarmSparks; i++)
        {
            var spark = Instantiate(sparkPrefab, _container);
            spark.SetActive(false);
            _sparks.Enqueue(spark);
        }
    }

    /// <summary>
    /// 从池子里取一颗子弹并发射，池子不够就临时取一颗
    /// </summary>
    public PlayerWeaponBullet SpawnBullet(Vector3 position, Vector3 direction)
    {
        PlayerWeaponBullet bullet;
        if (_bullets.Count > 0)
        {
            bullet = _bullets.Dequeue();
        }
        else
        {
            bullet = Instantiate(bulletPrefab, _container);
            bullet.pool = this;
        }

        bullet.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        bullet.gameObject.SetActive(true);
        bullet.Launch(direction);
        return bullet;
    }

    /// <summary>
    /// 从池里取一个火花,自动在 sparkLifeTime 后回池
    /// </summary>
    public GameObject SpawnSpark(Vector3 position, Vector3 direction)
    {
        GameObject spark;
        if (_sparks.Count > 0)
        {
            spark = _sparks.Dequeue();
        }
        else
        {
            spark = Instantiate(sparkPrefab, _container);
        }

        spark.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        spark.SetActive(true);
        StartCoroutine(ReturnSparkAfter(spark));
        return spark;
    }


    /// <summary>
    /// 子弹回池
    /// </summary>
    public void ReturnBullet(PlayerWeaponBullet bullet)
    {
        if (bullet == null) return;
        bullet.gameObject.SetActive(false);
        _bullets.Enqueue(bullet);
    }

    private IEnumerator ReturnSparkAfter(GameObject spark)
    {
        yield return new WaitForSeconds(sparkLifeTime);

        if (spark != null && spark.activeSelf)
        {
            spark.SetActive(false);
            _sparks.Enqueue(spark);
        }
    }

    #region 换武器重建池
    /// <summary>
    /// 切换武器时调用,换子弹/火花预制体, 并重建池
    /// </summary>
    public void SetupWeapon(PlayerWeaponBullet bullet, GameObject spark)
    {
        if (bullet != null) bulletPrefab = bullet;
        if (spark != null) sparkPrefab = spark;
        RebuildPool();
    }

    /// <summary>
    /// 清掉旧池, 用新预制体重建
    /// </summary>
    private void RebuildPool()
    {
        // 清掉旧池里的子弹/火花
        while (_bullets.Count > 0)
        {
            PlayerWeaponBullet b = _bullets.Dequeue();
            if (b != null) Destroy(b.gameObject);
        }
        while (_sparks.Count > 0)
        {
            GameObject s = _sparks.Dequeue();
            if (s != null) Destroy(s.gameObject);
        }

        // 用新预制体重建
        if (bulletPrefab == null || sparkPrefab == null) return;
        for (int i = 0; i < prewarmBullets; i++)
        {
            var bullet = Instantiate(bulletPrefab, _container);
            bullet.pool = this;
            bullet.gameObject.SetActive(false);
            _bullets.Enqueue(bullet);
        }
        for (int i = 0; i < prewarmSparks; i++)
        {
            var spark = Instantiate(sparkPrefab, _container);
            spark.SetActive(false);
            _sparks.Enqueue(spark);
        }
    }
    #endregion
}
