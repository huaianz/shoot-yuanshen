using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家子弹
/// </summary>
public class PlayerWeaponBullet : MonoBehaviour
{
    [Tooltip("伤害")]
    public int damage = 10;
    [HideInInspector]
    public Rigidbody rb;
    [Tooltip("推力")]
    public float flyPower = 30f;
    [Tooltip("子弹存活时间")]
    public float lifeTime = 10f;

    private Vector3 prevposition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        rb.velocity = transform.forward * flyPower;//给子弹一个推力
        Destroy(gameObject, lifeTime);

        prevposition = transform.position;
    }

    private void Update()
    {
        CheckCollision();
        prevposition = transform.position;
    }

    void CheckCollision()
    {
        RaycastHit hit;
        Vector3 dir = transform.position - prevposition;// 子弹方向
        float distance = Vector3.Distance(transform.position, prevposition);// 两帧之间的子弹飞行距离

        //绘制线段检测碰撞
        if (Physics.Raycast(prevposition, dir.normalized, out hit, distance))
        {
            //检测是否为敌人
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                enemy.Hurt(this, 1);
            }
        }

    }


}
