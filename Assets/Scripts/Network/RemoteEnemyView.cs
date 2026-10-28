using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 把快照里的敌人画出来
/// </summary>
public class RemoteEnemyView : MonoBehaviour
{
    [Tooltip("按 kind 索引: 0=近战 1=远程 2=史莱姆, 没配的用第 0 个")]
    public GameObject[] enemyPrefabs;

    [Tooltip("额外平滑速度")]
    public float smoothSpeed = 20f;

    private readonly Dictionary<int, Transform> _views = new Dictionary<int, Transform>();
    private readonly List<int> _alive = new List<int>();

    private void Update()
    {
        var bc = BattleClient.Instance;
        if (bc == null) return;
        if (!bc.Buffer.TrySampleEnemies(out var list)) return;
        Apply(list);
    }

    private void Apply(List<EnemyStateDto> list)
    {
        _alive.Clear();

        foreach (var e in list)
        {
            if (e.state == 3 || e.hp <= 0) continue;   // 死亡不显示
            _alive.Add(e.id);

            if (!_views.TryGetValue(e.id, out var t) || t == null)
            {
                GameObject prefab = PickPrefab(e.kind);
                if (prefab == null) continue;

                var go = Instantiate(prefab, transform);
                go.name = "Enemy_" + e.id + "_kind" + e.kind;
                t = go.transform;
                t.position = new Vector3(e.x, e.y, e.z);
                _views[e.id] = t;
                Debug.Log($"[RemoteEnemyView] 出现敌人 {e.id} kind={e.kind}");
            }

            Vector3 target = new Vector3(e.x, e.y, e.z);
            t.position = Vector3.Lerp(t.position, target, smoothSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation,
                Quaternion.Euler(0f, e.yaw, 0f), smoothSpeed * Time.deltaTime);
            float scale = e.elite == 1 ? 1.35f : 1f;
            t.localScale = new Vector3(scale, scale, scale);
        }

        List<int> toRemove = null;
        foreach (var kv in _views)
            if (!_alive.Contains(kv.Key))
                (toRemove ??= new List<int>()).Add(kv.Key);

        if (toRemove != null)
            foreach (int id in toRemove) RemoveView(id);
    }

    private GameObject PickPrefab(int kind)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        if (kind >= 0 && kind < enemyPrefabs.Length && enemyPrefabs[kind] != null)
            return enemyPrefabs[kind];
        return enemyPrefabs[0];
    }

    private void RemoveView(int id)
    {
        if (_views.TryGetValue(id, out var t))
        {
            if (t != null) Destroy(t.gameObject);
            _views.Remove(id);
        }
    }

    private void OnDestroy()
    {
        foreach (var kv in _views)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _views.Clear();
    }
}