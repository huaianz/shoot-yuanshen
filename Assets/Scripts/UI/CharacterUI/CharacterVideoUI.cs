using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;

public class CharacterVideoUI : MonoBehaviour
{
    [Header("视频显示")]
    public RawImage videoDisplay;
    public RenderTexture renderTexture;

    private VideoPlayer _videoPlayer;
    private string _currentVideoPath;
    private bool _isPreparing = false;
    private Texture2D _transparentTexture;

    private void Awake()
    {
        //创建透明纹理作为默认背景
        _transparentTexture = new Texture2D(1, 1);
        _transparentTexture.SetPixel(0, 0, Color.clear);
        _transparentTexture.Apply();

        if (videoDisplay != null)
            videoDisplay.texture = _transparentTexture;

        //如果没有指定的渲染目标纹理，则自动创建
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(1920, 1080, 0);
        }

        //初始化VideoPlayer
        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = renderTexture;
        _videoPlayer.isLooping = true;          // 循环播放
        _videoPlayer.playOnAwake = false;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // 纯画面，无音频
        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.errorReceived += OnVideoError;
    }


    /// <summary>
    /// 播放指定角色的视频
    /// </summary>
    /// <param name="characterID"></param>
    public void PlayVideo(int characterID)
    {
        // 物体未激活时无法启动协程,直接跳过并提示
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[CharacterVideoUI] 物体未激活,无法播放视频", this);
            return;
        }
        var role = GameManager.INSTANCE.GetRoleData(characterID);

        if (role == null)
        {
            StopVideo();
            return;
        }

        string videoPath = role.baseData.videoPath;
        if (string.IsNullOrEmpty(videoPath))
        {
            StopVideo();
            return;
        }

        // 如果当前已经播放该视频，不重新加载
        if (_currentVideoPath == videoPath && _videoPlayer.isPrepared)
        {
            if (!_videoPlayer.isPlaying)
                _videoPlayer.Play();
            return;
        }
        //从Resources加载
        StartCoroutine(LoadVideoFromResources(videoPath));
    }

    /// <summary>
    /// 视频准备完成回调
    /// </summary>
    /// <param name="soure"></param>
    private void OnVideoPrepared(VideoPlayer soure)
    {
        _isPreparing = false;
        if (_videoPlayer.isPrepared)
        {
            _videoPlayer.Play();
        }
    }

    /// <summary>
    /// 视频错误回调
    /// </summary>
    /// <param name="soure"></param>
    /// <param name="message"></param>
    private void OnVideoError(VideoPlayer soure, string message)
    {
        Debug.LogError($"视频播放错误：{message}");
        _isPreparing = false;
    }

    /// <summary>
    /// 停止播放视频
    /// </summary>
    public void StopVideo()
    {
        if (_videoPlayer != null)
        {
            if (_videoPlayer.isPlaying)
            {
                _videoPlayer.Stop();
            }
            if (_videoPlayer.isPrepared)
            {
                //暂停并释放资源
                _videoPlayer.Pause();
            }
        }
        _currentVideoPath = null;
        _isPreparing = false;
        //清空显示
        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
        }
    }

    /// <summary>
    /// 暂停视频
    /// </summary>
    public void PauseVideo()
    {
        if (_videoPlayer != null && _videoPlayer.isPlaying)
        {
            _videoPlayer.Pause();
        }
    }

    /// <summary>
    /// 恢复视频
    /// </summary>
    public void ResumeVideo()
    {
        if (_videoPlayer != null && _videoPlayer.isPrepared && !_videoPlayer.isPlaying)
        {
            _videoPlayer.Play();
        }
    }


    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.errorReceived -= OnVideoError;
            _videoPlayer.Stop();
            _videoPlayer.targetTexture = null;
        }
        if (renderTexture != null)
            renderTexture.Release();
    }

    private System.Collections.IEnumerator LoadVideoFromResources(string videoPath)
    {
        // videoPath 例如 "Video/Furina"（不带 .mp4）
        ResourceRequest request = Resources.LoadAsync<VideoClip>(videoPath);
        yield return request;

        VideoClip clip = request.asset as VideoClip;
        if (clip == null)
        {
            Debug.LogWarning($"视频加载失败：{videoPath}");
            StopVideo();
            yield break;
        }

        // 切换到视频纹理
        if (videoDisplay != null && videoDisplay.texture != renderTexture)
            videoDisplay.texture = renderTexture;

        _currentVideoPath = videoPath;
        _videoPlayer.clip = clip;
        _videoPlayer.Prepare();
        _isPreparing = true;
    }
}
