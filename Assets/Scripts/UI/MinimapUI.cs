using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小地图: 运行时创建俯视相机 + RenderTexture, 显示到 RawImage
/// 性能优化: 相机平时禁用, 只在节流帧手动 Render 一次(省约90%渲染开销)
/// 再配合低分辨率 + 小裁剪范围
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Tooltip("小地图分辨率(越小越省性能)")]
    public int textureSize = 256;

    [Tooltip("相机离地面高度(越大显示范围越大)")]
    public float cameraHeight = 18f;

    [Tooltip("相机跟随节流(秒), 越大越省")]
    public float followInterval = 0.1f;

    [Tooltip("正交相机显示范围(半宽)")]
    public float orthoSize = 10f;

    private Camera _miniCam;
    private RawImage _rawImage;
    private RenderTexture _rt;
    private float _timer;
    private bool _hidden;   // 其他界面打开时隐藏小地图

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        if (_rawImage == null) return;

        // 创建渲染纹理(低分辨率, 省性能)
        _rt = new RenderTexture(textureSize, textureSize, 16) { name = "MinimapRT" };
        _rt.Create();

        // 创建俯视相机
        GameObject camGo = new GameObject("MinimapCamera");
        _miniCam = camGo.AddComponent<Camera>();
        _miniCam.orthographic = true;          // 正交 = 平面地图效果
        _miniCam.orthographicSize = orthoSize; // 显示范围
        _miniCam.nearClipPlane = 0.1f;
        _miniCam.farClipPlane = 200f;          // 只渲染地图范围, 省性能
        _miniCam.clearFlags = CameraClearFlags.SolidColor;
        _miniCam.backgroundColor = new Color(0.15f, 0.2f, 0.15f);   // 地图底色(暗绿)
        _miniCam.targetTexture = _rt;
        _miniCam.enabled = false;              // 平时不自动渲染, 只在节流帧手动 Render

        _rawImage.texture = _rt;               // 纹理给 UI 显示
    }

    private void OnEnable()
    {
        // 监听界面开关: 有界面打开时隐藏小地图, 全部关闭后恢复
        EventHandler.UIStateChangedEvent += OnUIStateChanged;
        ApplyUIState(UIManager.IsAnyUIOpen);
    }

    private void OnDisable()
    {
        EventHandler.UIStateChangedEvent -= OnUIStateChanged;
    }

    private void OnUIStateChanged(bool isUIOpen)
    {
        ApplyUIState(isUIOpen);
    }

    /// <summary>
    /// 打开其他界面时隐藏小地图
    /// (只关 RawImage, 不关物体, 这样还能收到"界面全部关闭"事件恢复显示)
    /// </summary>
    private void ApplyUIState(bool isUIOpen)
    {
        _hidden = isUIOpen;
        if (_rawImage != null)
        {
            _rawImage.enabled = !isUIOpen;
        }
    }

    private void LateUpdate()
    {
        // 小地图隐藏时直接跳过, 省下相机移动和渲染
        if (_hidden) return;

        // 节流: 到时间才移动相机并渲染一次(其他帧完全零渲染)
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = followInterval;

        Transform target = GetPlayerTransform();
        if (target == null || _miniCam == null) return;

        Vector3 pos = target.position;
        pos.y = cameraHeight;                  // 从头顶俯视
        _miniCam.transform.position = pos;
        _miniCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // 朝下

        _miniCam.Render();                     // 手动渲染一次(这帧才画)
    }

    private Transform GetPlayerTransform()
    {
        if (PlayerController.INSTANCE != null && PlayerController.INSTANCE.currentPlayerModel != null)
        {
            return PlayerController.INSTANCE.currentPlayerModel.transform;
        }
        return null;
    }

    private void OnDestroy()
    {
        // 释放纹理和相机(防止内存泄漏)
        if (_rt != null) _rt.Release();
        if (_miniCam != null) Destroy(_miniCam.gameObject);
    }
}