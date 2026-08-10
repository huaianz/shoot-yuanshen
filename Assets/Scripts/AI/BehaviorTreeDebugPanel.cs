using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 行为树调试面板:运行时显示每只敌人正在执行的节点(按 F12 开关)
/// 只在开启时绘制,平时零开销
/// </summary>
public class BehaviorTreeDebugPanel : MonoBehaviour
{
    [Tooltip("开关按键")]
    public KeyCode toggleKey = KeyCode.F12;

    [Tooltip("刷新敌人列表的频率(秒),避免每帧查找")]
    public float refreshInterval = 0.5f;

    private readonly List<EnemyBase> _enemies = new List<EnemyBase>();
    private float _refreshTimer;
    private bool _visible = true;
    private GUIStyle _labelStyle;

    private void Update()
    {
        // 按 F12 开关面板
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
        }

        if (!_visible) return;

        // 定时刷新敌人列表(0.5 秒一次,不是每帧)
        _refreshTimer -= Time.deltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = refreshInterval;
            _enemies.Clear();
            _enemies.AddRange(FindObjectsOfType<EnemyBase>());
        }
    }

    private void OnGUI()
    {
        if (!_visible) return;

        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
        }

        GUILayout.BeginArea(new Rect(10, 10, 420, Screen.height - 20));
        GUILayout.Label("行为树调试面板 (F12 开关)", _labelStyle);
        GUILayout.Space(5);

        foreach (EnemyBase enemy in _enemies)
        {
            if (enemy == null) continue;

            BTNode node = enemy.CurrentActiveNode;
            string nodeName = node != null ? node.NodeName : "(无行为树)";

            GUILayout.Label(
                $"{enemy.name} | 阶段:{enemy.currentPhase} | 节点:{nodeName}",
                _labelStyle
            );
        }

        GUILayout.EndArea();
    }
}