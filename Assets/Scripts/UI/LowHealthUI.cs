using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 残血红闪警告: 血量低于阈值时, 屏幕边缘出现红色暗角(vignette)闪烁。
/// 暗角贴图在运行时生成, 不需要美术资源。
/// </summary>
public class LowHealthUI : MonoBehaviour
{
    // ---------- 懒加载单例 ----------
    private static LowHealthUI _instance;
    public static LowHealthUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LowHealthUI>();
                if (_instance == null) _instance = CreateNew();
            }
            return _instance;
        }
    }

    // ---------- 可调参数 ----------
    [Tooltip("血量低于这个比例开始闪烁")]
    public float threshold = 0.3f;      // 30% 血以下开始
    [Tooltip("闪烁周期(秒)")]
    public float flashInterval = 0.5f;  // 一明一暗的时长
    [Tooltip("暗角最低透明度")]
    public float minAlpha = 0.2f;
    [Tooltip("暗角最高透明度")]
    public float maxAlpha = 0.55f;
    [Tooltip("暗角从多远开始(0=中心, 1=边缘)")]
    public float vignetteStart = 0.55f; // 中心55%全透明, 往外渐变到边缘

    private CanvasGroup _canvasGroup;   // 控制暗角整体强度
    private Coroutine _flashCoroutine;  // 闪烁协程
    private bool _lowHealth;            // 当前是否在残血状态

    /// <summary>
    /// 自动创建(跨场景保留)
    /// </summary>
    public static LowHealthUI CreateNew()
    {
        GameObject go = new GameObject("LowHealthUI");
        DontDestroyOnLoad(go);
        LowHealthUI ui = go.AddComponent<LowHealthUI>();
        ui.BuildUI();
        return ui;
    }

    /// <summary>
    /// 搭建 UI: 全屏一张"红色暗角"图片
    /// </summary>
    private void BuildUI()
    {
        // Canvas: 屏幕空间覆盖层, 排序很高
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29999;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 图片: 铺满全屏
        GameObject imgGo = new GameObject("RedVignette");
        imgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = imgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 关键: 挂上运行时生成的"暗角贴图"(中心透明, 边缘不透明), 并染成红色
        Image image = imgGo.AddComponent<Image>();
        image.sprite = CreateVignetteSprite(256);
        image.color = new Color(1f, 0f, 0f, 1f); // 白色贴图染成红色

        _canvasGroup = imgGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;               // 初始完全隐藏
        _canvasGroup.blocksRaycasts = false;   // 不挡鼠标/点击
        _canvasGroup.interactable = false;
    }

    /// <summary>
    /// 生成暗角贴图: 中心透明(alpha=0), 边缘不透明(alpha=1)。
    /// 只有 256x256, 只生成一次, 之后全靠 CanvasGroup 控制强度, 零每帧开销。
    /// </summary>
    private Sprite CreateVignetteSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f; // 中心到边缘的最大距离

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // d: 0=中心, 1=边缘
                float d = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // 中心区域完全透明, 过了 vignetteStart 才逐渐变不透明
                float a = Mathf.Clamp01((d - vignetteStart) / (1f - vignetteStart));
                pixels[y * size + x] = new Color(1f, 1f, 1f, a); // 白色+透明度, 之后染色
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 转成 Sprite 给 Image 用
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    // 订阅血量事件(事件驱动, 平时零开销)
    private void OnEnable()
    {
        EventHandler.PlayerHealthChangedEvent += OnHealthChanged;
    }

    private void OnDisable()
    {
        EventHandler.PlayerHealthChangedEvent -= OnHealthChanged;
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
    }

    /// <summary>
    /// 血量变化回调
    /// </summary>
    private void OnHealthChanged(int roleID, float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f) return;
        bool low = currentHealth > 0f && currentHealth / maxHealth <= threshold;
        SetLowHealth(low);
    }

    /// <summary>
    /// 切换闪烁状态(只在状态变化时执行一次)
    /// </summary>
    private void SetLowHealth(bool low)
    {
        if (_lowHealth == low) return;
        _lowHealth = low;

        if (low)
        {
            if (_flashCoroutine == null)
            {
                _flashCoroutine = StartCoroutine(FlashLoop());
            }
        }
        else
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// 闪烁循环: 暗角透明度在 minAlpha~maxAlpha 之间来回
    /// </summary>
    private IEnumerator FlashLoop()
    {
        while (true)
        {
            float p = Mathf.PingPong(Time.time / flashInterval, 1f);
            _canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, p);
            yield return null;
        }
    }
}