using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 地区提示
/// </summary>
public class RegionBannerUI : MonoBehaviour
{
    //懒汉式单例模式
    private static RegionBannerUI _instance;
    public static RegionBannerUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<RegionBannerUI>();
                if (_instance == null) _instance = CreateNew();
            }
            return _instance;
        }
    }

    [Tooltip("显示时长")]
    public float showDuration = 3f;
    [Tooltip("淡出时长")]
    public float fadeDuration = 1f;

    private TextMeshProUGUI _text;
    private CanvasGroup _canvasGroup;
    private Coroutine _coroutine;

    /// <summary>
    /// 场景名
    /// </summary>
    private static readonly Dictionary<string, string> RegionNames = new Dictionary<string, string>
    {
        { "SampleScene", "安全区" },
        { "BattleMap", "战斗区域" }
    };

    /// <summary>
    /// 自动创建
    /// </summary>
    /// <returns></returns>
    public static RegionBannerUI CreateNew()
    {
        GameObject go = new GameObject("RegionBannerUI");
        DontDestroyOnLoad(go);
        RegionBannerUI ui = go.AddComponent<RegionBannerUI>();
        ui.BuildUI();
        return ui;
    }

    /// <summary>
    /// 构建UI
    /// </summary>
    private void BuildUI()
    {
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29500;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject textGo = new GameObject("RegionText");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(900f, 80f);

        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.font = UITextHelper.GetFont();
        _text.fontSize = 44;
        _text.color = new Color(1f, 0.9f, 0.5f); // 金色
        _text.alignment = TextAlignmentOptions.Center;
        _text.text = "";

        _canvasGroup = textGo.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f; // 初始隐藏

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载回调，根据场景名称显示对应的区域名称
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ShowRegionByScene(scene.name);
    }

    /// <summary>
    /// 按场景名显示
    /// </summary>
    /// <param name="sceneName"></param>
    public static void ShowRegionByScene(string sceneName)
    {
        string name = RegionNames.TryGetValue(sceneName, out string v) ? v : sceneName;
        Instance.Show(name);
    }

    /// <summary>
    /// 直接显示某个地区名
    /// </summary>
    /// <param name="regionName"></param>
    public static void ShowRegion(string regionName)
    {
        Instance.Show(regionName);
    }

    /// <summary>
    /// 显示并计时
    /// </summary>
    /// <param name="regionName"></param>
    public void Show(string regionName)
    {
        if (_text == null) BuildUI();

        _text.text = regionName;
        _canvasGroup.alpha = 1f;

        // 连续切换地区时，以最后一次为准：先停掉旧的隐藏协程
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(HideAfterDelay());
    }

    /// <summary>
    /// 先停留，再淡出
    /// </summary>
    /// <returns></returns>
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration); // 停留几秒

        // 逐渐透明
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = 1f - t / fadeDuration;
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _coroutine = null;
    }
}
