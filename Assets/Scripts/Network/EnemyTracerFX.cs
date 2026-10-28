using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 远程敌人弹道: 对象池 + LineRenderer 短暂显示
/// </summary>
public class EnemyTracerFX : MonoBehaviour
{
    public int poolSize = 8;
    public float lifetime = 0.08f;
    public float width = 0.06f;
    public Color hitColor = new Color(1f, 0.55f, 0.2f);
    public Color missColor = new Color(1f, 1f, 1f, 0.35f);

    private class Tracer
    {
        public LineRenderer Line;
        public float Life;
    }

    private readonly List<Tracer> _pool = new List<Tracer>();
    private Material _material;
    private int _next;

    private void Awake()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) _material = new Material(shader);

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Tracer_" + i);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = width;
            lr.numCapVertices = 2;
            if (_material != null) lr.material = _material;
            lr.enabled = false;

            _pool.Add(new Tracer { Line = lr, Life = 0f });
        }
    }

    private void OnEnable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnEnemyShot += HandleShot;
    }

    private void OnDisable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnEnemyShot -= HandleShot;
    }

    private void HandleShot(EnemyShotDto dto)
    {
        if (_pool.Count == 0) return;

        var t = _pool[_next];
        _next = (_next + 1) % _pool.Count;

        t.Line.SetPosition(0, new Vector3(dto.fx, dto.fy, dto.fz));
        t.Line.SetPosition(1, new Vector3(dto.tx, dto.ty, dto.tz));

        Color c = dto.hit ? hitColor : missColor;
        t.Line.startColor = c;
        t.Line.endColor = c;
        t.Line.enabled = true;
        t.Life = lifetime;
    }

    private void Update()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            var t = _pool[i];
            if (t.Life <= 0f) continue;

            t.Life -= Time.deltaTime;
            if (t.Life <= 0f) t.Line.enabled = false;
        }
    }
}