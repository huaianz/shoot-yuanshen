using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 音频管理器：管理背景音乐 + 音量/开关设置（设置会存档，下次启动记住）
/// </summary>
public class AudioManager : SingleMonoBase<AudioManager>
{
    [Header("BGM音量(0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.8f;   // 当前音量

    [Header("BGM开关")]
    [SerializeField] private bool bgmEnabled = true;    // 音乐是否开着
    [Header("音效池")]
    [SerializeField] private int sfxSourceCount = 6;   // 预创建几个音效播放器

    private AudioSource _bgmSource;
    private string _currentClipName;
    private AudioSource[] _sfxSources;                 // 音效播放器池
    private int _sfxIndex;                             // 当前轮到第几个
    private readonly Dictionary<string, AudioClip> _sfxCache = new();  // 音效缓存

    // 给 UI 读取的只读属性
    public float BGMVolume => bgmVolume;
    public bool BGMEnabled => bgmEnabled;

    // 存档用的键名
    private const string KeyVolume = "BGM_Volume";
    private const string KeyEnabled = "BGM_Enabled";

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        // 读取上次保存的设置（没有存档就用默认值）
        bgmVolume = PlayerPrefs.GetFloat(KeyVolume, 0.8f);
        bgmEnabled = PlayerPrefs.GetInt(KeyEnabled, 1) == 1;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = 0f;              // 由淡入控制

        // 预创建音效播放器池: 连续开枪/受击时轮流使用, 互不打断
        _sfxSources = new AudioSource[sfxSourceCount];
        for (int i = 0; i < sfxSourceCount; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;   // 2D 音效, 不随距离衰减
            _sfxSources[i] = s;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>UI 调音量（滑块拖动时会持续调用，事件驱动，没有每帧轮询）</summary>
    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);                  // 限制在 0~1
        PlayerPrefs.SetFloat(KeyVolume, bgmVolume);    // 只改内存，不立刻写盘
        ApplyVolume();
    }

    /// <summary>UI 开关音乐</summary>
    public void SetBGMEnabled(bool on)
    {
        bgmEnabled = on;
        PlayerPrefs.SetInt(KeyEnabled, on ? 1 : 0);
        ApplyVolume();
    }

    /// <summary>把当前设置立刻作用到正在播的歌上</summary>
    private void ApplyVolume()
    {
        if (_bgmSource == null) return;
        // 关闭时音量归零（歌继续播但不发声），再打开时恢复原音量
        _bgmSource.volume = bgmEnabled ? bgmVolume : 0f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "LoginScene": PlayBGM("Audio/BGM/Login", bgmVolume, 1f); break;
            case "SampleScene": PlayBGM("Audio/BGM/SafeZone", bgmVolume, 1f); break;
            case "BattleMap": PlayBGM("Audio/BGM/Battle", bgmVolume, 1f); break;
        }
    }

    public void PlayBGM(string clipPath, float volume, float fadeTime = 1f)
    {
        if (clipPath == _currentClipName) return;

        AudioClip clip = Resources.Load<AudioClip>(clipPath);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] 找不到BGM: {clipPath}");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(CrossFadeTo(clip, volume, fadeTime));
        _currentClipName = clipPath;
    }

    private IEnumerator CrossFadeTo(AudioClip clip, float targetVolume, float fadeTime)
    {
        // 实际目标音量：如果音乐被关闭，就淡入到 0（歌继续播但不发声）
        float realTarget = bgmEnabled ? targetVolume : 0f;

        // 淡出当前
        float t = 0f;
        float from = _bgmSource.volume;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(from, 0f, t / fadeTime);
            yield return null;
        }
        _bgmSource.volume = 0f;

        // 换歌 + 淡入
        _bgmSource.clip = clip;
        _bgmSource.Play();
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, realTarget, t / fadeTime);
            yield return null;
        }
        _bgmSource.volume = realTarget;
    }

    /// <summary>
    /// 播放短音效: 第一次加载进缓存, 之后直接复用; 池内轮换播放, 无 GC 分配
    /// </summary>
    /// <param name="clipPath">Resources 里的路径, 如 "Audio/SFX/Gunshot"</param>
    /// <param name="volume">音量 0~1</param>
    public void PlaySFX(string clipPath, float volume = 1f)
    {
        // 缓存里没有就先加载并记住
        if (!_sfxCache.TryGetValue(clipPath, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] 找不到音效: {clipPath}");
                return;
            }
            _sfxCache[clipPath] = clip;   // 只加载一次, 之后永远复用
        }

        // 轮流用池里的播放器, PlayOneShot 支持连续触发
        _sfxSources[_sfxIndex].PlayOneShot(clip, volume);
        _sfxIndex = (_sfxIndex + 1) % _sfxSources.Length;
    }
    /// <summary>
    /// 直接播放已加载的 AudioClip(武器专属音效用, 跳过 Resources 查找, 更快)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _sfxSources == null || _sfxSources.Length == 0) return;
        _sfxSources[_sfxIndex].PlayOneShot(clip, volume);
        _sfxIndex = (_sfxIndex + 1) % _sfxSources.Length;
    }
}