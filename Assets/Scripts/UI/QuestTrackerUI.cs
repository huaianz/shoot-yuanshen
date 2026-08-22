using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 委托追踪
/// </summary>
public class QuestTrackerUI : MonoBehaviour
{
    //懒汉式单例模式
    private static QuestTrackerUI _instance;
    public static QuestTrackerUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<QuestTrackerUI>();
                if (_instance == null) _instance = CreateNew();
            }
            return _instance;
        }
    }

    private TextMeshProUGUI _text;
    private GameObject _canvasGo;   // 运行时创建的 Canvas, 用于整体隐藏
    /// <summary>
    /// 自动创建
    /// </summary>
    /// <returns></returns>
    public static QuestTrackerUI CreateNew()
    {
        GameObject go = new GameObject("QuestTrackerUI");
        DontDestroyOnLoad(go);
        QuestTrackerUI ui = go.AddComponent<QuestTrackerUI>();
        ui.BuildUI();
        return ui;
    }

    /// <summary>
    /// 搭建UI
    /// </summary>
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvasGo = canvasGo;   // 记住 Canvas, 供打开界面时隐藏
        canvas.sortingOrder = 29000;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        //文字：锚点定在左中 (0,0.5)，避免和小地图(左上角)重叠
        GameObject textGo = new GameObject("QuestText");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(24f, 60f);
        rt.sizeDelta = new Vector2(500f, 120f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.font = UITextHelper.GetFont();
        _text.fontSize = 26;
        _text.color = new Color(1f, 0.95f, 0.7f);
        _text.alignment = TextAlignmentOptions.TopLeft;
        _text.text = "";

        // 创建完立刻刷新一次
        Refresh();
    }

    private void OnEnable()
    {
        QuestManager.QuestUpdatedEvent += OnQuestUpdated;
        EventHandler.UIStateChangedEvent += OnUIStateChanged;
        ApplyUIState(UIManager.IsAnyUIOpen);
    }

    private void OnDisable()
    {
        QuestManager.QuestUpdatedEvent -= OnQuestUpdated;
        EventHandler.UIStateChangedEvent -= OnUIStateChanged;
    }

    private void OnUIStateChanged(bool isUIOpen)
    {
        ApplyUIState(isUIOpen);
    }

    /// <summary>
    /// 打开其他界面时隐藏委托追踪, 全部关闭后恢复
    /// (只隐藏子 Canvas, 根物体保持激活, 这样还能收到"界面全部关闭"事件)
    /// </summary>
    private void ApplyUIState(bool isUIOpen)
    {
        if (_canvasGo != null)
        {
            _canvasGo.SetActive(!isUIOpen);
        }
    }

    //事件回调：委托进度变了就刷新显示
    private void OnQuestUpdated()
    {
        Refresh();
    }

    /// <summary>
    /// 刷新文本
    /// </summary>
    public void Refresh()
    {
        if (_text == null) return;

        // 没有进行中的委托 -> 清空
        if (QuestManager.INSTANCE == null || !QuestManager.INSTANCE.IsQuestActive)
        {
            _text.text = "";
            return;
        }

        // 有委托 -> 显示 "委托: 名字" + "进度: 当前/目标"
        QuestData_SO q = QuestManager.INSTANCE.ActiveQuest;
        _text.text = $"委托: {q.questName}\n进度: {QuestManager.INSTANCE.CurrentProgress}/{QuestManager.INSTANCE.TargetCount}";
    }
}
