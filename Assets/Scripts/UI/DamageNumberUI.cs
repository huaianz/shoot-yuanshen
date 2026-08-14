using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 伤害数字
/// </summary>
public class DamageNumberUI : MonoBehaviour
{
    private static DamageNumberUI _instance;
    public static DamageNumberUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DamageNumberUI>();
                if (_instance == null) _instance = CreateNew();
            }
            return _instance;
        }
    }

    private const int PoolSize = 24;        // 预创建的文字数量
    private const float LifeTime = 0.8f;    // 每个数字存活秒数
    private const float RiseDistance = 70f; // 上飘距离
    private const float DriftX = 30f;       // 左右随机偏移, 让连续数字错开
    private const int SortingOrder = 20000; // 显示层级

    //对象池
    private readonly Stack<TextMeshProUGUI> _pool = new Stack<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> _active = new List<TextMeshProUGUI>();
    private readonly List<Vector2> _startPos = new List<Vector2>();
    private readonly List<float> _ages = new List<float>();

    private Camera _cam;
    private RectTransform _canvasRect;

    private void Awake()
    {
        _cam = Camera.main;
        BuildCanvasAndPool();
    }

    private void Update()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            _ages[i] += Time.deltaTime;
            float p = Mathf.Clamp01(_ages[i] / LifeTime); // 0~1 表示存活进度

            TextMeshProUGUI txt = _active[i];
            // 上飘: 从起始位置慢慢往上走
            txt.rectTransform.anchoredPosition = _startPos[i] + new Vector2(0f, RiseDistance * p);
            // 淡出: 透明度从 1 降到 0
            Color c = txt.color;
            c.a = 1f - p;
            txt.color = c;

            // 时间到, 回收进池
            if (p >= 1f)
            {
                txt.gameObject.SetActive(false);
                _pool.Push(txt);
                _active.RemoveAt(i);
                _startPos.RemoveAt(i);
                _ages.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 在敌人头顶显示伤害数字(世界坐标 -> 屏幕坐标)
    /// </summary>
    public void Show(Vector3 worldPos, int damage)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        // 把敌人世界坐标换算成屏幕像素坐标
        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) return; // 敌人在相机背后, 不显示

        TextMeshProUGUI txt = GetFromPool();
        if (txt == null) return; // 池子暂时满了, 这帧不显示(绝不创建新对象)

        // 屏幕像素坐标 -> 画布本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, null, out Vector2 local);
        // 加一点随机偏移, 让连续伤害数字错开
        Vector2 pos = local + new Vector2(Random.Range(-DriftX, DriftX), Random.Range(0f, 20f));

        txt.gameObject.SetActive(true);
        txt.rectTransform.anchoredPosition = pos;
        txt.text = damage.ToString();
        txt.color = Color.white;

        _active.Add(txt);
        _startPos.Add(pos);
        _ages.Add(0f);
    }

    /// <summary>
    /// 从池里拿一个文字, 没有就返回 null
    /// </summary>
    private TextMeshProUGUI GetFromPool()
    {
        return _pool.Count > 0 ? _pool.Pop() : null;
    }

    private void BuildCanvasAndPool()
    {
        GameObject canvasGo = new GameObject("DamageCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        _canvasRect = canvasGo.GetComponent<RectTransform>();

        // 预创建 PoolSize 个文字, 全部先隐藏
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject go = new GameObject("DamageText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(canvasGo.transform, false);
            TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
            txt.font = UITextHelper.GetFont(); // 复用你的中文字体工具
            txt.fontSize = 36;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
            txt.text = "";
            go.SetActive(false);
            _pool.Push(txt);
        }
    }

    public static DamageNumberUI CreateNew()
    {
        GameObject go = new GameObject("DamageNumberUI");
        DontDestroyOnLoad(go); // 跨场景保留, 换地图不用重新建
        return go.AddComponent<DamageNumberUI>();
    }
}
