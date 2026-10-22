using System.Collections.Generic;
using UnityEngine;

/// <summary>把快照缓冲里插值出来的"其他玩家"显示成胶囊体</summary>
public class RemotePlayerView : MonoBehaviour
{
    [Tooltip("远端玩家的显示预制体(胶囊体)")]
    public GameObject remotePlayerPrefab;

    [Tooltip("额外平滑速度(在插值之上再抹一点抖动)")]
    public float smoothSpeed = 20f;

    private readonly Dictionary<int, Transform> _views = new Dictionary<int, Transform>();
    private readonly List<int> _aliveIds = new List<int>();

    private void Start()
    {
        if (BattleClient.Instance == null)
        {
            Debug.LogError("[RemotePlayerView] 找不到 BattleClient.Instance");
            return;
        }
        BattleClient.Instance.OnPlayerJoinLeave += HandleJoinLeave;
    }

    private void OnDestroy()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnPlayerJoinLeave -= HandleJoinLeave;
        ClearAll();
    }

    private void Update()
    {
        var bc = BattleClient.Instance;
        if (bc == null || remotePlayerPrefab == null) return;

        // 从缓冲里取插值后的位置(缓冲不足时先不动)
        if (!bc.Buffer.TrySample(out var players)) return;
        Apply(players);
    }

    private void Apply(List<PlayerStateDto> players)
    {
        _aliveIds.Clear();
        int myId = BattleClient.Instance.MyPlayerId;

        foreach (var p in players)
        {
            if (p.id == myId) continue;      // 自己由本地控制
            _aliveIds.Add(p.id);

            if (!_views.TryGetValue(p.id, out var t) || t == null)
            {
                var go = Instantiate(remotePlayerPrefab, transform);
                go.name = "RemotePlayer_" + p.id;
                t = go.transform;
                _views[p.id] = t;
                Debug.Log($"[RemotePlayerView] 出现玩家 {p.id}");
            }

            Vector3 target = new Vector3(p.x, p.y, p.z);
            t.position = Vector3.Lerp(t.position, target, smoothSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, p.yaw, 0f), smoothSpeed * Time.deltaTime);
        }

        // 快照里没有的玩家 → 销毁
        List<int> toRemove = null;
        foreach (var kv in _views)
            if (!_aliveIds.Contains(kv.Key))
                (toRemove ??= new List<int>()).Add(kv.Key);
        if (toRemove != null)
            foreach (int id in toRemove) RemoveView(id);
    }

    private void HandleJoinLeave(int playerId, bool joined)
    {
        if (!joined) RemoveView(playerId);
    }

    private void RemoveView(int id)
    {
        if (_views.TryGetValue(id, out var t))
        {
            if (t != null) Destroy(t.gameObject);
            _views.Remove(id);
            Debug.Log($"[RemotePlayerView] 移除玩家 {id}");
        }
    }

    private void ClearAll()
    {
        foreach (var kv in _views)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _views.Clear();
    }
}
