using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 快照缓冲: 保存最近若干帧快照, 按"渲染时间 - 插值延迟"在两个快照之间插值取样,
/// 消除 20Hz 数据直接赋值造成的抖动。
/// </summary>
public class SnapshotBuffer
{
    private class Entry
    {
        public float RecvTime;   // 本地收到时间(Time.unscaledTime)
        public int Tick;
        public SnapshotDto Snap;
    }

    [Tooltip("插值延迟(秒): 越大越平滑但越滞后, 一般取 1~2 个快照间隔")]
    public float InterpolationDelay = 0.1f;
    public int MaxEntries = 32;

    private readonly List<Entry> _entries = new List<Entry>();
    public int Count => _entries.Count;

    public void Push(SnapshotDto snap)
    {
        _entries.Add(new Entry { RecvTime = Time.unscaledTime, Tick = snap.tick, Snap = snap });
        while (_entries.Count > MaxEntries) _entries.RemoveAt(0);
    }

    public void Clear() => _entries.Clear();

    /// <summary>取样: 返回插值后的玩家列表; 缓冲不足时返回 false</summary>
    public bool TrySample(out List<PlayerStateDto> result)
    {
        result = null;
        if (_entries.Count < 2) return false;

        float renderTime = Time.unscaledTime - InterpolationDelay;
        for (int i = _entries.Count - 1; i > 0; i--)
        {
            Entry newer = _entries[i];
            Entry older = _entries[i - 1];
            if (renderTime >= older.RecvTime && renderTime <= newer.RecvTime)
            {
                float span = newer.RecvTime - older.RecvTime;
                float t = span > 0.0001f ? (renderTime - older.RecvTime) / span : 0f;
                result = Interpolate(older.Snap, newer.Snap, t);
                return true;
            }
        }
        return false;
    }

    private static List<PlayerStateDto> Interpolate(SnapshotDto a, SnapshotDto b, float t)
    {
        var list = new List<PlayerStateDto>();
        foreach (var pa in a.players)
        {
            PlayerStateDto pb = null;
            for (int i = 0; i < b.players.Count; i++)
                if (b.players[i].id == pa.id) { pb = b.players[i]; break; }
            if (pb == null) continue;   // 这一帧他已经不在(刚离开)

            list.Add(new PlayerStateDto
            {
                id = pa.id,
                x = Mathf.Lerp(pa.x, pb.x, t),
                y = Mathf.Lerp(pa.y, pb.y, t),
                z = Mathf.Lerp(pa.z, pb.z, t),
                yaw = Mathf.LerpAngle(pa.yaw, pb.yaw, t),
                animId = pb.animId,
                hp = pb.hp
            });
        }
        return list;
    }
}
