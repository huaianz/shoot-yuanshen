using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Tooltip("子弹生成的位置")]
    public Transform bullletSpawnPoint;
    [Tooltip("对象池")]
    public BulletPool bulletPool;
    [Tooltip("子弹发射间隔")]
    public float bulletInterval = 0.15f;
    private float lastFireTime;//上一次子弹发射的时间


    private void Awake()
    {
        // 没拖引用就自动找同物体上的对象池
        if (bulletPool == null)
        {
            bulletPool = GetComponent<BulletPool>();
        }
    }

    /// <summary>
    /// 朝targetPos方向发射,从对象池取子弹和火花
    /// </summary>
    public void Fire(Vector3 targetPos)
    {
        if (Time.time - lastFireTime < bulletInterval)
        {
            return;
        }
        lastFireTime = Time.time;

        Vector3 direction = (targetPos - bullletSpawnPoint.position).normalized;

        if (bulletPool != null)
        {
            bulletPool.SpawnBullet(bullletSpawnPoint.position, direction);
            bulletPool.SpawnSpark(bullletSpawnPoint.position, direction);
        }
    }

}
