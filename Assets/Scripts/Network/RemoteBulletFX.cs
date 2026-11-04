using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 其他玩家开火: 用真实子弹预制体做纯视觉飞行(不结算伤害)
/// </summary>
public class RemoteBulletFX : MonoBehaviour
{
    [Tooltip("每种武器预生成多少颗视觉子弹")]
    public int prewarmPerWeapon = 12;

    private class Pool
    {
        public readonly Queue<PlayerWeaponBullet> Free = new Queue<PlayerWeaponBullet>();
    }

    private readonly Dictionary<int, Pool> _pools = new Dictionary<int, Pool>();
    private Transform _container;
    private bool _subscribed;

    private void Awake()
    {
        var go = new GameObject("RemoteBulletContainer");
        go.transform.SetParent(transform, false);
        _container = go.transform;
    }

    private void OnEnable() { TrySubscribe(); }
    private void Start() { TrySubscribe(); }
    private void OnDisable() { Unsubscribe(); }
    private void OnDestroy() { Unsubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;

        BattleClient.Instance.OnPlayerShot += HandleShot;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (BattleClient.Instance != null)
            BattleClient.Instance.OnPlayerShot -= HandleShot;

        _subscribed = false;
    }

    private void HandleShot(PlayerShotDto dto)
    {
        // 自己的子弹由本地射击逻辑生成, 这里只表现别人的
        if (BattleClient.Instance != null && dto.shooterId == BattleClient.Instance.MyPlayerId)
            return;

        var prefab = ResolveBulletPrefab(dto.weaponId);
        if (prefab == null) return;

        Vector3 from = new Vector3(dto.ox, dto.oy, dto.oz);
        Vector3 to = new Vector3(dto.ex, dto.ey, dto.ez);

        Vector3 dir = to - from;
        float distance = dir.magnitude;
        if (distance < 0.05f) return;
        dir /= distance;

        var pool = GetPool(prefab);
        var bullet = pool.Free.Count > 0
            ? pool.Free.Dequeue()
            : CreateBullet(prefab, pool);

        bullet.visualOnly = true;
        bullet.maxDistance = distance;
        bullet.transform.SetPositionAndRotation(from, Quaternion.LookRotation(dir));

        // 每次发射都要重新挂回调(Finish 后会被清空)
        bullet.onFinished = b =>
        {
            b.gameObject.SetActive(false);
            pool.Free.Enqueue(b);
        };

        if (bullet.rb != null)
            bullet.rb.useGravity = false;

        bullet.gameObject.SetActive(true);
        bullet.Launch(dir);
    }

    private Pool GetPool(PlayerWeaponBullet prefab)
    {
        int key = prefab.GetInstanceID();
        if (_pools.TryGetValue(key, out var pool)) return pool;

        pool = new Pool();
        _pools[key] = pool;

        for (int i = 0; i < prewarmPerWeapon; i++)
            pool.Free.Enqueue(CreateBullet(prefab, pool));

        return pool;
    }

    private PlayerWeaponBullet CreateBullet(PlayerWeaponBullet prefab, Pool pool)
    {
        var bullet = Instantiate(prefab, _container);
        bullet.visualOnly = true;
        bullet.onFinished = b =>
        {
            b.gameObject.SetActive(false);
            pool.Free.Enqueue(b);
        };

        // 纯视觉子弹不参与任何物理碰撞
        foreach (var col in bullet.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        bullet.gameObject.SetActive(false);
        return bullet;
    }

    private static PlayerWeaponBullet ResolveBulletPrefab(int weaponId)
    {
        if (weaponId < 0) return null;

        var inv = InventoryManager.INSTANCE;
        if (inv == null || inv.weaponData == null) return null;

        var template = inv.weaponData.GetWeaponByID(weaponId);
        return template != null ? template.bulletPrefab : null;
    }
}
