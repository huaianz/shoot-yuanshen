// using UnityEngine;

// public class PlayerWeapon : MonoBehaviour
// {
//     [Tooltip("子弹生成的位置(枪口)")]
//     public Transform bullletSpawnPoint;
//     [Tooltip("对象池")]
//     public BulletPool bulletPool;
//     [Tooltip("子弹发射间隔")]
//     public float bulletInterval = 0.15f;
//     private float lastFireTime;

//     private void Awake()
//     {
//         if (bulletPool == null)
//         {
//             bulletPool = GetComponent<BulletPool>();
//         }
//     }

//     public void Fire(Vector3 targetPos)
//     {
//         if (Time.time - lastFireTime < bulletInterval)
//         {
//             return;
//         }
//         lastFireTime = Time.time;

//         // 从枪口射出,保持手感
//         Vector3 origin = bullletSpawnPoint.position;
//         Vector3 direction = (targetPos - origin).normalized;

//         if (bulletPool != null)
//         {
//             bulletPool.SpawnBullet(origin, direction);
//             bulletPool.SpawnSpark(origin, direction);
//         }
//     }
// }

using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Tooltip("枪口(仅备用)")]
    public Transform bullletSpawnPoint;
    [Tooltip("对象池")]
    public BulletPool bulletPool;
    [Tooltip("子弹发射间隔")]
    public float bulletInterval = 0.15f;
    private float lastFireTime;

    private void Awake()
    {
        if (bulletPool == null)
        {
            bulletPool = GetComponent<BulletPool>();
        }
    }

    public void Fire(Vector3 targetPos)
    {
        if (Time.time - lastFireTime < bulletInterval)
        {
            return;
        }
        lastFireTime = Time.time;

        // 发射原点:相机中心(永远正确,不依赖骨骼)
        Vector3 origin = Camera.main != null
            ? Camera.main.transform.position
            : bullletSpawnPoint.position;

        Vector3 direction = (targetPos - origin).normalized;

        if (bulletPool != null)
        {
            // 子弹从相机中心出
            bulletPool.SpawnBullet(origin, direction);

            // 火花也从相机中心前方 0.6 米出,视觉上就在枪口位置
            bulletPool.SpawnSpark(origin + direction * 0.6f, direction);
        }
    }
}