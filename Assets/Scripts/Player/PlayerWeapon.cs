using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    [Tooltip("子弹生成的位置")]
    public Transform bullletSpawnPoint;
    [Tooltip("子弹预制体")]
    public PlayerWeaponBullet bulletEffectPrefab;
    [Tooltip("枪管火花预制体")]
    public GameObject bulletSparkPrefab;
    [Tooltip("子弹发射间隔")]
    public float bulletInterval = 0.15f;
    private float lastFireTime;//上一次子弹发射的时间

    /// <summary>
    /// 朝着targetPos方向发射子弹
    /// </summary>
    /// <param name="targetPos"></param>
    public void Fire(Vector3 targetPos)
    {
        //检查发射间隔
        if (Time.time - lastFireTime < bulletInterval)
        {
            return;
        }
        lastFireTime = Time.time;

        //计算发射方向
        Vector3 direction = targetPos - bullletSpawnPoint.position;
        direction.Normalize();//归一化
        //实例化子弹预制体
        PlayerWeaponBullet bulletEffect = Instantiate(bulletEffectPrefab, bullletSpawnPoint.position, Quaternion.identity);

        //实例化火花的预制体
        GameObject spark = Instantiate(bulletSparkPrefab, bullletSpawnPoint.position, Quaternion.identity);
        spark.transform.forward = direction;

        //设置子弹的朝向
        bulletEffect.transform.forward = direction;

    }

}
