using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        rb.velocity = transform.forward * flyPower;//给子弹一个推力
        Destroy(gameObject, lifeTime);
    }
}
