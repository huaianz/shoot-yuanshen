using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 全局消息提示：消息会排队显示，一条播完再播下一条，
/// 不会被紧接着的拾取提示立刻覆盖（比如金色"委托完成"不会被"获得金币"冲掉）。
/// </summary>
public class ToastUI : MonoBehaviour
{
    // ===== 一条待显示的消息 =====
    private struct ToastMessage
    {
        public string text;
        public Color color;

        public ToastMessage(string text, Color color)
        {
            this.text = text;
            this.color = color;
        }
    }

    //懒加载单例
    private static ToastUI _instance;
    public static ToastUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ToastUI>();
                if (_instance == null)
                {
                    _instance = CreateNew();
                }
            }
            return _instance;
        }
    }

    [Header("动画参数")]
    [Tooltip("上移距离")]
    public float floatUpDistance = 80f;
    [Tooltip("原地停留时间")]
    public float stayDuration = 0.6f;
    [Tooltip("上移+淡出时间(秒)")]
    public float floatDuration = 1.2f;
    [Tooltip("字体大小")]
    public int fontSize = 30;

    private TextMeshProUGUI _text;
    private RectTransform _textRect;
    private CanvasGroup _canvasGroup;
    private Vector2 _startPos;
    private Coroutine _animCoroutine;

    //消息队列：还没轮到的提示都存在这里
    private Queue<ToastMessage> _messageQueue = new Queue<ToastMessage>();
    //当前是否正在播放一条提示
    private bool _isShowing;

    //字体缓存
    private static TMP_FontAsset _font;

    /// <summary>
    /// 自动创建ToastUI
    /// </summary>
    public static ToastUI CreateNew()
    {
        GameObject go = new GameObject("ToastUI");
        DontDestroyOnLoad(go);
        ToastUI toast = go.AddComponent<ToastUI>();
        toast.BuildUI();
        return toast;
    }

    /// <summary>
    /// 运行时创建UI结构（只创建一次，之后全部复用）
    /// </summary>
    private void BuildUI()
    {
        //屏幕空间覆盖层，排序在最上面
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        //文字物体
        GameObject textGo = new GameObject("ToastText");
        textGo.transform.SetParent(canvasGo.transform, false);

        _textRect = textGo.AddComponent<RectTransform>();
        // 底部居中
        _textRect.anchorMin = new Vector2(0.5f, 0f);
        _textRect.anchorMax = new Vector2(0.5f, 0f);
        _textRect.pivot = new Vector2(0.5f, 0.5f);
        _textRect.anchoredPosition = new Vector2(0f, 120f);
        _textRect.sizeDelta = new Vector2(1000f, 60f);
        _startPos = _textRect.anchoredPosition;

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.font = LoadFont();
        _text.fontSize = fontSize;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.white;
        _text.text = "";

        //CanvasGroup：用来整体淡出
        _canvasGroup = textGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 从Resources加载一个支持中文的 SDF 字体（只加载一次）
    /// </summary>
    private static TMP_FontAsset LoadFont()
    {
        if (_font != null)
        {
            return _font;
        }
        string[] candidates =
        {
            "font/MSYH SDF",
            "font/汉仪文黑-85W SDF",
            "font/genshin-impact-font-regular SDF"
        };

        foreach (string path in candidates)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>(path);
            if (font != null)
            {
                _font = font;
                return _font;
            }
        }
        return null;
    }

    /// <summary>
    /// 显示一条提示：先放进队列，没在播就立刻开始播；
    /// 正在播就等它播完再播下一条（排队，不互相覆盖）
    /// </summary>
    public void Show(string msg, Color? color = null)
    {
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }
        if (_text == null)
        {
            BuildUI();
        }

        // 1. 把这条消息放进队列
        _messageQueue.Enqueue(new ToastMessage(msg, color ?? Color.white));

        // 2. 如果当前没有在播放，立刻开始播队列里的第一条
        if (!_isShowing)
        {
            ShowNext();
        }
    }

    /// <summary>
    /// 从队列取出一条消息并开始播放动画
    /// </summary>
    private void ShowNext()
    {
        // 队列空了：结束播放状态
        if (_messageQueue.Count == 0)
        {
            _isShowing = false;
            return;
        }

        _isShowing = true;
        ToastMessage m = _messageQueue.Dequeue();

        // 写入文字和颜色，并复位到起始位置
        _text.text = m.text;
        _text.color = m.color;
        _textRect.anchoredPosition = _startPos;
        _canvasGroup.alpha = 1f;

        _animCoroutine = StartCoroutine(Animate());
    }

    /// <summary>
    /// 动画：先停留，然后上移+淡出；播完自动播下一条
    /// </summary>
    private IEnumerator Animate()
    {
        // 原地停留一小会儿
        yield return new WaitForSeconds(stayDuration);

        // 上移+淡出
        float t = 0f;
        Vector2 from = _startPos;
        Vector2 to = from + Vector2.up * floatUpDistance;

        while (t < floatDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / floatDuration);
            // 先快后慢
            float eased = 1f - Mathf.Pow(1f - p, 2f);

            _textRect.anchoredPosition = Vector2.Lerp(from, to, eased);
            _canvasGroup.alpha = 1f - p;
            yield return null;
        }

        // 清空文字并隐藏
        _canvasGroup.alpha = 0f;
        _text.text = "";
        _animCoroutine = null;

        // 一条播完，播下一条（队列里还有就继续，没有就结束）
        ShowNext();
    }

    /// <summary>
    /// 静态快捷入口：任何脚本直接 ToastUI.ShowMessage("文字") 即可
    /// </summary>
    public static void ShowMessage(string msg, Color? color = null)
    {
        Instance.Show(msg, color);
    }
}