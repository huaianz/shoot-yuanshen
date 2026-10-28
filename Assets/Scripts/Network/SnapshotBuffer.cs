using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 快照缓冲: 保存最近若干帧, 按"渲染时间 - 插值延迟"在两个快照之间插值取样。
/// </summary>
public class SnapshotBuffer
{
    private class Entry
    {
        public float RecvTime;   // 本地收到时间
        public int Tick;
        public SnapshotDto Snap;
    }

    [Tooltip("插值延迟(秒): 越大越平滑但越滞后")]
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

    /// <summary>取渲染时刻的前后两帧和插值系数(玩家/敌人共用)</summary>
    private bool TryGetPair(out Entry older, out Entry newer, out float t)
    {
        older = null;
        newer = null;
        t = 0f;
        if (_entries.Count < 2) return false;

        float renderTime = Time.unscaledTime - InterpolationDelay;
        for (int i = _entries.Count - 1; i > 0; i--)
        {
            Entry a = _entries[i - 1];
            Entry b = _entries[i];
            if (renderTime >= a.RecvTime && renderTime <= b.RecvTime)
            {
                float span = b.RecvTime - a.RecvTime;
                older = a;
                newer = b;
                t = span > 0.0001f ? (renderTime - a.RecvTime) / span : 0f;
                return true;
            }
        }
        return false;
    }

    public bool TrySample(out List<PlayerStateDto> result)
    {
        result = null;
        if (!TryGetPair(out var a, out var b, out float t)) return false;
        result = InterpolatePlayers(a.Snap, b.Snap, t);
        return true;
    }

    public bool TrySampleEnemies(out List<EnemyStateDto> result)
    {
        result = null;
        if (!TryGetPair(out var a, out var b, out float t)) return false;
        result = InterpolateEnemies(a.Snap, b.Snap, t);
        return true;
    }

    private static List<PlayerStateDto> InterpolatePlayers(SnapshotDto a, SnapshotDto b, float t)
    {
        var list = new List<PlayerStateDto>();
        foreach (var pa in a.players)
        {
            PlayerStateDto pb = null;
            for (int i = 0; i < b.players.Count; i++)
                if (b.players[i].id == pa.id) { pb = b.players[i]; break; }
            if (pb == null) continue;

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

    private static List<EnemyStateDto> InterpolateEnemies(SnapshotDto a, SnapshotDto b, float t)
    {
        var list = new List<EnemyStateDto>();
        foreach (var ea in a.enemies)
        {
            EnemyStateDto eb = null;
            for (int i = 0; i < b.enemies.Count; i++)
                if (b.enemies[i].id == ea.id) { eb = b.enemies[i]; break; }
            if (eb == null) continue;

            list.Add(new EnemyStateDto
            {
                id = ea.id,
                kind = eb.kind,
                x = Mathf.Lerp(ea.x, eb.x, t),
                y = Mathf.Lerp(ea.y, eb.y, t),
                z = Mathf.Lerp(ea.z, eb.z, t),
                yaw = Mathf.LerpAngle(ea.yaw, eb.yaw, t),
                hp = eb.hp,
                maxHp = eb.maxHp,
                state = eb.state
            });
        }
        return list;
    }
}