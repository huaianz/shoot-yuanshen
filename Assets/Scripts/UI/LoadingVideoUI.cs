using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 地图切换加载视频: 传送/死亡回城时全屏播放 Resources/Video/loading.mp4
/// 懒加载单例: 只有用到时才创建, 平时零开销
/// </summary>
public class LoadingVideoUI : MonoBehaviour
{
    // 懒汉式单例
    private static LoadingVideoUI _instance;
    public static LoadingVideoUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LoadingVideoUI>();
                if (_instance == null) _instance = CreateNew();
            }
            return _instance;
        }
    }

    public static LoadingVideoUI CreateNew()
    {
        GameObject go = new GameObject("LoadingVideoUI");
        DontDestroyOnLoad(go);
        return go.AddComponent<LoadingVideoUI>();
    }

    [Tooltip("视频播放速度(1=正常速度, 2=两倍速...), 视频必须播完才关闭")]
    public float playbackSpeed = 2f;

    private Canvas _canvas;
    private RawImage _rawImage;
    private VideoPlayer _videoPlayer;
    private RenderTexture _rt;
    private bool _videoFinished;   // 视频是否已播完
    private bool _hideRequested;   // 是否已请求关闭(地图切换完成)
    private bool _shown;            // 当前是否正在显示(防止重复锁定/解锁输入)
    private Coroutine _safetyTimer; // 兜底超时: 视频卡住时强制结束

    private void Awake()
    {
        try
        {
            BuildUI();
        }
        catch (System.Exception e)
        {
            // 即使创建失败也不能让传送卡死: 打印错误, 并保证 Canvas 处于隐藏状态
            Debug.LogError($"[LoadingVideoUI] 创建失败: {e.Message}\n{e.StackTrace}");
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 搭建UI: 全屏 Canvas + 黑色底 + RawImage + 视频播放器
    /// (黑色底和视频画面分成两个子物体, 避免组件互相干扰)
    /// </summary>
    private void BuildUI()
    {
        // 全屏 Canvas, 排序层级最高(盖住所有游戏UI)
        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 30000;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 黑色背景(独立子物体)
        GameObject bgGo = new GameObject("BlackBackground");
        bgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        Image bg = bgGo.AddComponent<Image>();
        bg.color = Color.black;              // 视频没出画面时先黑屏

        // 视频画面(独立子物体)
        GameObject videoGo = new GameObject("VideoImage");
        videoGo.transform.SetParent(canvasGo.transform, false);
        RectTransform rt = videoGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _rawImage = videoGo.AddComponent<RawImage>();
        if (_rawImage != null)
        {
            _rawImage.raycastTarget = true;  // 挡住点击, 避免加载时误操作
            _rt = new RenderTexture(1920, 1080, 16) { name = "LoadingVideoRT" };
            _rt.Create();
            _rawImage.texture = _rt;
        }
        else
        {
            Debug.LogError("[LoadingVideoUI] RawImage 创建失败, 只显示黑屏");
        }

        // 视频播放器
        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        if (_videoPlayer != null)
        {
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = false;      // 播一遍即可
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _rt;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;   // 加载视频不需要声音
            _videoPlayer.clip = Resources.Load<VideoClip>("Video/loading");
            _videoPlayer.loopPointReached += OnVideoFinished;   // 视频播完回调
        }

        // 默认隐藏
        canvasGo.SetActive(false);
    }
    /// <summary>显示并播放加载视频(倍速播放)</summary>
    public void Show()
    {
        if (_canvas == null) return;
        // 首次显示时锁定玩家输入, 防止加载视频期间乱动/开枪
        if (!_shown)
        {
            _shown = true;
            UIManager.EnterUIBlock();
        }
        _canvas.gameObject.SetActive(true);
        _videoFinished = false;
        _hideRequested = false;
        // 兜底: 万一视频卡住不播完, 12秒后强制结束, 防止一直锁住
        if (_safetyTimer != null) StopCoroutine(_safetyTimer);
        _safetyTimer = StartCoroutine(SafetyTimeoutRoutine());
        if (_videoPlayer != null && _videoPlayer.clip != null)
        {
            _videoPlayer.playbackSpeed = playbackSpeed;   // 倍速播放
            _videoPlayer.Play();
        }
        else
        {
            // 视频资源没加载出来时, 显示一小会儿再关, 防止一直卡住
            StartCoroutine(ShowFallbackRoutine());
        }
    }

    /// <summary>请求隐藏(切换流程结束时调用): 视频没播完会先等它播完</summary>
    public void Hide()
    {
        if (_canvas == null) return;
        _hideRequested = true;
        if (!_videoFinished) return;
        DoHide();
    }

    // VideoPlayer 播完回调: 视频播完且地图切换也完成时才真正关闭
    private void OnVideoFinished(VideoPlayer vp)
    {
        _videoFinished = true;
        if (_hideRequested) DoHide();
    }

    // 视频资源缺失时的兜底: 至少显示 2 秒再关
    private IEnumerator ShowFallbackRoutine()
    {
        yield return new WaitForSeconds(2f);
        _videoFinished = true;
        if (_hideRequested) DoHide();
    }

    // 兜底超时: 视频播不出来也不能让加载界面一直锁住
    private IEnumerator SafetyTimeoutRoutine()
    {
        yield return new WaitForSeconds(12f);
        _videoFinished = true;
        if (_hideRequested) DoHide();
    }

    private void DoHide()
    {
        if (!_shown) return;
        _shown = false;
        if (_safetyTimer != null)
        {
            StopCoroutine(_safetyTimer);
            _safetyTimer = null;
        }
        if (_canvas != null) _canvas.gameObject.SetActive(false);
        if (_videoPlayer != null && _videoPlayer.isPlaying) _videoPlayer.Stop();
        UIManager.ExitUIBlock();   // 解锁玩家输入
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null) _videoPlayer.loopPointReached -= OnVideoFinished;
        // 释放渲染纹理, 防止内存泄漏
        if (_rt != null) _rt.Release();
    }
}