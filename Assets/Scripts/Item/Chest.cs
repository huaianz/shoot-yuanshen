using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱：按F打开 → 优先播Animator开盖动画(方案A)，没有就转盖子(方案B) + 发光 + 生成掉落物
/// </summary>
public class Chest : MonoBehaviour
{
    [Header("宝箱奖励")]
    [Tooltip("打开后按概率生成的掉落物(和敌人掉落同一个配置类)")]
    public List<LootDrop> rewards;

    [Header("交互")]
    [Tooltip("提示文字")]
    public string openHint = "按 F 打开宝箱";

    [Header("打开动画(方案A: Animator)")]
    [Tooltip("模型自带的 Animator")]
    public Animator chestAnimator;
    [Tooltip("Animator 里的触发参数名")]
    public string openTrigger = "Open";

    [Header("打开动画(方案B: 直接转盖子)")]
    [Tooltip("盖子物体(不填就按名字自动找)")]
    public Transform lidBone;
    [Tooltip("盖子骨骼名")]
    public string lidBoneName = "LID";
    [Tooltip("盖子打开角度(绕X轴, 负值向后开)")]
    public float openAngle = -110f;
    [Tooltip("开盖动画时长(秒)")]
    public float openAnimTime = 1f;

    [Header("初始姿态")]
    [Tooltip("盖子默认关闭角度(模型初始是打开时填)")]
    public Vector3 closedRotation = Vector3.zero;

    [Header("发光")]
    [Tooltip("宝箱子物体上的点光源")]
    public Light glowLight;
    [Tooltip("发光颜色")]
    public Color glowColor = new Color(1f, 0.8f, 0.2f);
    [Tooltip("发光峰值强度")]
    public float glowMaxIntensity = 2.5f;
    [Tooltip("发光持续时间(秒)")]
    public float glowDuration = 1.5f;

    private InteractionUI _interactionUI;
    private bool _isInRange;
    private bool _opened;
    private bool _useAnimator;
    private List<Renderer> _renderers = new List<Renderer>();
    private MaterialPropertyBlock _propBlock;
    private float _baseLightIntensity;

    private void Awake()
    {
        _interactionUI = InteractionUI.INSTANCE;

        // 方案A: Animator 和控制器都有效才用动画
        if (chestAnimator == null)
        {
            chestAnimator = GetComponentInChildren<Animator>();
        }
        _useAnimator = chestAnimator != null && chestAnimator.runtimeAnimatorController != null;

        // 方案B: 找盖子骨骼(只在Awake找一次)
        if (lidBone == null && !string.IsNullOrEmpty(lidBoneName))
        {
            lidBone = FindChildByName(transform, lidBoneName);
            if (lidBone == null)
            {
                lidBone = FindChildByName(transform, "Chest_Top");
            }
            if (lidBone == null)
            {
                lidBone = FindChildByName(transform, "Top");
            }
        }

        // 初始姿态: 模型绑定姿态是打开时, 强制把盖子转回关闭
        if (lidBone != null)
        {
            lidBone.localRotation = Quaternion.Euler(closedRotation);
        }

        // 收集所有渲染器(箱体+盖子一起发光)
        GetComponentsInChildren<Renderer>(_renderers);
        _propBlock = new MaterialPropertyBlock();

        // 一次性打开共享材质的"自发光"开关(不复制材质, 省内存)
        foreach (Renderer r in _renderers)
        {
            Material m = r.sharedMaterial;
            if (m != null && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
            }
        }

        if (glowLight != null)
        {
            _baseLightIntensity = glowLight.intensity;
        }
    }

    private void Update()
    {
        // 任何UI打开时不能开箱(背包/商店/对话等)
        if (UIManager.IsAnyUIOpen) return;

        if (_isInRange && !_opened && Input.GetKeyDown(KeyCode.F))
        {
            Open();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isInRange = true;
            if (_interactionUI != null)
            {
                _interactionUI.ShowHint(openHint);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isInRange = false;
            if (_interactionUI != null)
            {
                _interactionUI.HideHint();
            }
        }
    }

    private void Open()
    {
        if (_opened) return;
        _opened = true;

        // 隐藏提示，关闭触发器（不能再开第二次）
        if (_interactionUI != null) _interactionUI.HideHint();
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 开盖动画: 有Animator用动画, 否则转盖子
        if (_useAnimator)
        {
            chestAnimator.SetTrigger(openTrigger);
        }
        else if (lidBone != null)
        {
            StartCoroutine(OpenLidAnimation());
        }
        else
        {
            Debug.LogWarning($"{name}: 既没有有效Animator, 也没找到盖子(LID), 只出奖励不播动画");
        }

        // 从箱口高度弹出奖励（复用敌人掉落逻辑）
        if (rewards != null)
        {
            foreach (LootDrop reward in rewards)
            {
                PickupItem.Spawn(transform.position + Vector3.up * 0.6f, reward);
            }
        }

        // 全局提示
        ToastUI.ShowMessage("打开了宝箱！", glowColor);

        // 发光动画
        StartCoroutine(GlowAnimation());
    }

    /// <summary>
    /// 方案B: 直接旋转盖子(缓出动画, 从关闭转到打开角度)
    /// </summary>
    private IEnumerator OpenLidAnimation()
    {
        float t = 0f;
        Quaternion from = lidBone.localRotation;
        Quaternion to = from * Quaternion.Euler(openAngle, 0f, 0f);

        while (t < openAnimTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / openAnimTime);
            float eased = 1f - Mathf.Pow(1f - p, 2f);

            lidBone.localRotation = Quaternion.Slerp(from, to, eased);
            yield return null;
        }

        lidBone.localRotation = to;
    }

    /// <summary>
    /// 递归按名字找子物体(盖子骨骼)
    /// </summary>
    private Transform FindChildByName(Transform root, string name)
    {
        string upper = name.ToUpperInvariant();
        if (root.name.ToUpperInvariant().Contains(upper))
        {
            return root;
        }
        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// 发光: 先快速变亮, 再缓缓熄灭(正弦波, 一次完成)
    /// </summary>
    private IEnumerator GlowAnimation()
    {
        float t = 0f;
        while (t < glowDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / glowDuration);

            // sin曲线: 0 → 峰值 → 0，看起来像"闪一下"
            float intensity = Mathf.Sin(p * Mathf.PI) * glowMaxIntensity;

            ApplyGlow(intensity);
            if (glowLight != null)
            {
                glowLight.intensity = intensity;
            }
            yield return null;
        }

        // 结束: 颜色归零, 灯光回到初始亮度(0)
        ApplyGlow(0f);
        if (glowLight != null)
        {
            glowLight.intensity = _baseLightIntensity;
        }
    }

    /// <summary>
    /// 用 MaterialPropertyBlock 设置自发光颜色(不修改、不复制材质资源)
    /// </summary>
    private void ApplyGlow(float intensity)
    {
        if (_renderers.Count == 0) return;

        Color c = glowColor * Mathf.Max(0f, intensity);
        _propBlock.SetColor("_EmissionColor", c);
        foreach (Renderer r in _renderers)
        {
            r.SetPropertyBlock(_propBlock);
        }
    }
}