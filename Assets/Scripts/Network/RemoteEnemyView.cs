using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 把快照里的敌人画成真实模型并同步动画
/// </summary>
public class RemoteEnemyView : MonoBehaviour
{
    [Serializable]
    public class EnemyVisual
    {
        public GameObject prefab;

        [Header("状态名(要和 Animator 里的 State 名一致)")]
        public string idleState = "Idle";
        public string moveState = "Move";
        public string attackState = "Attack";
        public string deadState = "Dead";

        [Header("移动速度参数(控制器没有就留空)")]
        public string moveSpeedParam = "MoveSpeed";
    }

    [Tooltip("按 kind 索引: 0=近战 1=远程 2=史莱姆")]
    public EnemyVisual[] visuals;

    public float smoothSpeed = 20f;

    [Tooltip("死亡动画播完后多久移除")]
    public float deadHideDelay = 1.5f;

    private class View
    {
        public Transform Root;
        public Animator Animator;
        public EnemyVisual Config;
        public int LastState = -1;
        public bool Dying;
        public float DeadTimer;
    }

    private readonly Dictionary<int, View> _views = new Dictionary<int, View>();
    private readonly List<int> _alive = new List<int>();

    private void OnEnable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnRoomChanged += HandleRoomChanged;
    }

    private void OnDisable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnRoomChanged -= HandleRoomChanged;
    }

    private void HandleRoomChanged()
    {
        ClearAll();
    }

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
            bool dead = e.state == 3 || e.hp <= 0;

            if (!_views.TryGetValue(e.id, out var v) || v.Root == null)
            {
                if (dead) continue;
                v = CreateView(e);
                if (v == null) continue;
            }

            if (dead)
            {
                if (!v.Dying)
                {
                    v.Dying = true;
                    v.DeadTimer = deadHideDelay;
                    PlayState(v, 3);
                }

                v.DeadTimer -= Time.deltaTime;
                if (v.DeadTimer <= 0f)
                {
                    RemoveView(e.id);
                    continue;
                }

                _alive.Add(e.id);
                continue;
            }

            _alive.Add(e.id);

            Vector3 target = new Vector3(e.x, e.y, e.z);
            v.Root.position = Vector3.Lerp(v.Root.position, target, smoothSpeed * Time.deltaTime);

            Vector3 scale = Vector3.one * (e.elite == 1 ? 1.35f : 1f);
            v.Root.localScale = scale;

            v.Root.rotation = Quaternion.Slerp(v.Root.rotation,
                Quaternion.Euler(0f, e.yaw, 0f), smoothSpeed * Time.deltaTime);

            PlayState(v, e.state);
        }

        List<int> toRemove = null;
        foreach (var kv in _views)
            if (!_alive.Contains(kv.Key))
                (toRemove ??= new List<int>()).Add(kv.Key);

        if (toRemove != null)
            foreach (int id in toRemove) RemoveView(id);
    }

    private View CreateView(EnemyStateDto e)
    {
        var cfg = PickConfig(e.kind);
        if (cfg == null || cfg.prefab == null) return null;

        var go = Instantiate(cfg.prefab, transform);
        go.name = "Enemy_" + e.id + "_kind" + e.kind;
        go.transform.position = new Vector3(e.x, e.y, e.z);

        // 动画事件接收者(展示版删掉了原 AI 脚本)
        if (go.GetComponent<EnemyAnimEventProxy>() == null)
            go.AddComponent<EnemyAnimEventProxy>();

        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null)
            anim.applyRootMotion = false;

        var v = new View
        {
            Root = go.transform,
            Animator = anim,
            Config = cfg
        };

        _views[e.id] = v;
        return v;
    }

    private void PlayState(View v, int state)
    {
        if (v.LastState == state) return;
        v.LastState = state;

        var cfg = v.Config;
        if (cfg == null) return;

        string stateName = null;
        switch (state)
        {
            case 0: stateName = cfg.idleState; break;
            case 1: stateName = cfg.moveState; break;
            case 2: stateName = cfg.attackState; break;
            case 3: stateName = cfg.deadState; break;
        }

        var anim = v.Animator;
        if (anim != null && !string.IsNullOrEmpty(stateName))
            anim.CrossFadeInFixedTime(stateName, 0.15f, 0);

        if (anim != null && !string.IsNullOrEmpty(cfg.moveSpeedParam)
            && HasParam(anim, Animator.StringToHash(cfg.moveSpeedParam)))
        {
            anim.SetFloat(Animator.StringToHash(cfg.moveSpeedParam), state == 1 ? 1f : 0f);
        }
    }

    private static bool HasParam(Animator anim, int hash)
    {
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].nameHash == hash) return true;
        return false;
    }

    private EnemyVisual PickConfig(int kind)
    {
        if (visuals == null || visuals.Length == 0) return null;
        if (kind >= 0 && kind < visuals.Length && visuals[kind] != null
            && visuals[kind].prefab != null)
            return visuals[kind];
        return visuals[0];
    }

    private void RemoveView(int id)
    {
        if (_views.TryGetValue(id, out var v))
        {
            if (v.Root != null) Destroy(v.Root.gameObject);
            _views.Remove(id);
        }
    }

    private void OnDestroy()
    {
        ClearAll();
    }

    private void ClearAll()
    {
        foreach (var kv in _views)
            if (kv.Value.Root != null) Destroy(kv.Value.Root.gameObject);
        _views.Clear();
    }
}
