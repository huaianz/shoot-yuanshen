using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 全局消息提示
/// </summary>
public class ToastUI : MonoBehaviour
{
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

    //字体缓存
    private static TMP_FontAsset _font;

    /// <summary>
    /// 自动创建ToastUI
    /// </summary>
    /// <returns></returns>
    public static ToastUI CreateNew()
    {
        GameObject go = new GameObject("ToastUI");
        DontDestroyOnLoad(go);
        ToastUI toast = go.AddComponent<ToastUI>();
        toast.BuildUI();
        return toast;
    }

    /// <summary>
    /// 运行时创建UI结构
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

        //anvasGroup：用来整体淡出
        _canvasGroup = textGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 从Resources加载一个支持中文的 SDF 字体
    /// </summary>
    /// <returns></returns>
    private static TMP_FontAsset LoadFont()
    {
        if (_font != null)
            return _font;
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

        //没有找到可用的字体
        return null;
    }

    /// <summary>
    /// 显示一条提示
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="color"></param>
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
        _text.text = msg;
        if (color.HasValue)
        {
            _text.color = color.Value;
        }
        //复位
        _textRect.anchoredPosition = _startPos;
        _canvasGroup.alpha = 1f;
        // 连续消息以最后一条为准：先停掉旧动画
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
        }
        _animCoroutine = StartCoroutine(Animate());
    }

    /// <summary>
    /// 动画方式为先停留，然后上移加淡化，最后隐藏
    /// </summary>
    /// <returns></returns>
    private IEnumerator Animate()
    {
        //原地停留一小会儿
        yield return new WaitForSeconds(stayDuration);
        //上移加淡化
        float t = 0f;
        Vector2 from = _startPos;
        Vector2 to = from + Vector2.up * floatUpDistance;

        while (t < floatDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / floatDuration);
            //先快后慢
            float eased = 1f - Mathf.Pow(1f - p, 2f);

            _textRect.anchoredPosition = Vector2.Lerp(from, to, eased);
            _canvasGroup.alpha = 1f - p;
            yield return null;
        }

        //清空文字并隐藏
        _canvasGroup.alpha = 0f;
        _text.text = "";
        _animCoroutine = null;
    }

    /// <summary>
    /// 静态快捷入口：任何脚本直接 ToastUI.ShowMessage("文字")即可
    /// </summary>
    public static void ShowMessage(string msg, Color? color = null)
    {
        Instance.Show(msg, color);
    }
}


