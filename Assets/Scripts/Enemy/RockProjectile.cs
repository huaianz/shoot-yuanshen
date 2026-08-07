using UnityEngine;

/// <summary>
/// 石头投射物:抛物线飞行;第一次碰到地面会弹跳一下,碰到玩家销毁
/// </summary>
public class RockProjectile : MonoBehaviour
{
    public int damage = 15;

    [Tooltip("弹跳后保留的垂直速度比例(越大弹得越高)")]
    public float bounceFactor = 0.6f;

    [Tooltip("超时兜底销毁(秒)")]
    public float lifeTime = 3f;

    private Rigidbody _rb;
    private Collider _col;
    private bool _hasBounced;
    private float _aliveTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        _aliveTime = 0f;
    }

    private void Update()
    {
        _aliveTime += Time.deltaTime;
        if (_aliveTime >= lifeTime)
        {
            Destroy(gameObject);   // 兜底:超时销毁
        }
    }

    /// <summary>
    /// 发射(由石头人调用):设置位置/速度,延迟 0.2 秒开启碰撞
    /// </summary>
    public void Launch(Vector3 position, Vector3 velocity)
    {
        transform.SetPositionAndRotation(position, Quaternion.identity);
        _hasBounced = false;
        _aliveTime = 0f;

        if (_col != null) _col.enabled = false;
        Invoke(nameof(EnableCollider), 0.2f);

        _rb.velocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 碰到玩家:造成伤害并销毁
        if (collision.collider.CompareTag("Player"))
        {
            GameManager.INSTANCE?.ApplyDamageToActiveRole(damage);
            Destroy(gameObject);
            return;
        }

        // 判断是不是地面(碰撞法线朝上 ≈ 踩在地面上)
        Vector3 normal = collision.contacts[0].normal;
        bool hitGround = normal.y > 0.5f;

        // 第一次碰到地面:弹跳一下(保留部分向上速度,水平速度也降一点)
        if (!_hasBounced && hitGround)
        {
            _hasBounced = true;

            Vector3 v = _rb.velocity;
            v.y = Mathf.Abs(v.y) * bounceFactor;
            v.x *= 0.7f;
            v.z *= 0.7f;
            _rb.velocity = v;
            return;
        }

        // 已经弹过一次,再碰地面/墙就销毁
        Destroy(gameObject);
    }

    private void EnableCollider()
    {
        if (_col != null) _col.enabled = true;
    }
}