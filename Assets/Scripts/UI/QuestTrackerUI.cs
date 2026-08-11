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
        canvas.sortingOrder = 29000;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        //文字：锚点定在左上角 (0,1)，从左上角往右下排布
        GameObject textGo = new GameObject("QuestText");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -24f);
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
    }

    private void OnDisable()
    {
        QuestManager.QuestUpdatedEvent -= OnQuestUpdated;
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
