using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开始界面 + 登录/注册界面(独立 LoginScene 使用)。
/// 动态星空 + 流星雨 + 标题呼吸 + 卡片入场 + 按钮悬停反馈。
/// 所有特效均为运行时生成, 零美术资源, 流星/星星全部预生成复用。
/// </summary>
public class LoginUI : MonoBehaviour
{
    private static LoginUI _instance;
    public static LoginUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LoginUI>();
                if (_instance == null) _instance = CreateNew();
            }
            return _instance;
        }
    }

    // ===== 场景元素 =====
    private GameObject _panel;
    private CanvasGroup _panelGroup;
    private RectTransform _panelRect;
    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private TextMeshProUGUI _statusText;
    private TextMeshProUGUI _title;
    private RectTransform _spinner;
    private bool _built;
    private bool _connecting;

    // ===== 开始界面元素 =====
    private GameObject _startPanel;
    private CanvasGroup _startGroup;
    private RectTransform _startRect;
    private TextMeshProUGUI _startTitle;
    private bool _switching;

    // ===== 加载界面元素 =====
    private GameObject _loadingPanel;
    private CanvasGroup _loadingGroup;
    private Image _loadingFill;
    private TextMeshProUGUI _loadingPercent;
    private TextMeshProUGUI _loadingTip;
    private GameObject _generatingGroup;    // "正在生成世界..."整组(文本+转圈)
    private RectTransform _loadingSpinner;  // 生成中的转圈
    private int _lastPercent = -1;          // 上次显示的百分比, 没变就不刷新文本

    // 加载时随机显示的提示语(只读数组, 不参与每帧运算)
    private static readonly string[] _loadingTips =
    {
        "提示：击败敌人可获得掉落物和素材",
        "提示：宝箱里藏着珍贵的武器和材料",
        "提示：委托任务可以提交给 NPC 换取奖励",
        "提示：商店里可以购买补给品",
        "提示：不同敌人有不同攻击方式，注意观察",
        "提示：弹药有限，战斗时注意补给",
    };

    // ===== 音效(Resources/Audio/UI 下的 wav, 只加载一次, 复用同一个 AudioSource) =====
    private AudioSource _sfx;
    private AudioClip _clickSfx;
    private AudioClip _hoverSfx;
    private AudioClip _successSfx;
    private AudioClip _errorSfx;

    // ===== 星空特效数据 =====
    private readonly List<RectTransform> _stars = new List<RectTransform>();
    private readonly List<float> _starPhases = new List<float>();
    private readonly List<RectTransform> _meteors = new List<RectTransform>();
    private readonly List<Vector2> _meteorVels = new List<Vector2>();
    private readonly Color _titleBase = new Color(1f, 0.85f, 0.45f);

    private void Awake()
    {
        if (!_built)
        {
            _built = true;
            BuildUI();
        }
    }

    private void OnEnable()
    {
        GameClient.Instance.OnLoginResult += OnLoginResult;
        GameClient.Instance.OnRegisterResult += OnRegisterResult;
        // 云存档管理器在登录阶段就要存在(负责下载/上传)
        _ = CloudSaveManager.Instance;
    }

    private void OnDisable()
    {
        GameClient.Instance.OnLoginResult -= OnLoginResult;
        GameClient.Instance.OnRegisterResult -= OnRegisterResult;
    }

    public static LoginUI CreateNew()
    {
        GameObject go = new GameObject("LoginUI");
        return go.AddComponent<LoginUI>(); // Awake 会搭建UI
    }

    // ==================== UI 搭建 ====================

    private void BuildUI()
    {
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasGo = new GameObject("LoginCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---- 背景: 优先用 LoginBackground.png, 没有就用程序渐变 ----
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        RectTransform brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        Sprite bgSprite = Resources.Load<Sprite>("Image/UI/LoginBackground");
        bg.GetComponent<Image>().sprite = bgSprite != null ? bgSprite : CreateGradientSprite();

        // ---- 星星(预生成60颗, 只改alpha) ----
        for (int i = 0; i < 60; i++)
        {
            RectTransform star = CreateDot(bg.transform, Random.Range(2f, 4f));
            star.anchoredPosition = new Vector2(Random.Range(0f, 1920f), Random.Range(0f, 1080f));
            star.gameObject.SetActive(Random.value > 0.2f); // 留些空白
            if (star.gameObject.activeSelf)
            {
                _stars.Add(star);
                _starPhases.Add(Random.Range(0f, 360f));
            }
        }

        // ---- 流星(预生成4条, 循环复用) ----
        Sprite streak = CreateStreakSprite();
        for (int i = 0; i < 6; i++)
        {
            GameObject m = new GameObject("Meteor", typeof(RectTransform), typeof(Image));
            m.transform.SetParent(bg.transform, false);
            RectTransform mrt = m.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.zero;
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.sizeDelta = new Vector2(420f, 30f);
            m.GetComponent<Image>().sprite = streak;
            m.GetComponent<Image>().color = new Color(1f, 0.95f, 0.8f, 0.95f);

            ResetMeteor(mrt, out Vector2 vel);
            if (i < 2) mrt.anchoredPosition = new Vector2(Random.Range(200f, 1900f), Random.Range(400f, 800f)); // 前两条先落在半空, 错开节奏
            _meteors.Add(mrt);
            _meteorVels.Add(vel);
        }

        // ---- 登录卡片 ----
        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _panel.transform.SetParent(canvasGo.transform, false);
        _panelRect = _panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        _panelRect.sizeDelta = new Vector2(720f, 680f);
        _panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.13f, 0.92f);
        _panelGroup = _panel.GetComponent<CanvasGroup>();

        // 顶部金色装饰条
        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(_panel.transform, false);
        RectTransform art = accent.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 1f);
        art.anchorMax = new Vector2(1f, 1f);
        art.sizeDelta = new Vector2(0f, 4f);
        art.anchoredPosition = new Vector2(0f, -2f);
        accent.GetComponent<Image>().color = new Color(1f, 0.8f, 0.3f);

        // 标题 + 副标题
        _title = CreateText(_panel.transform, "Title", new Vector2(0f, 250f), "原神 FPS", 60);
        _title.color = _titleBase;
        _title.outlineWidth = 0.25f;
        _title.outlineColor = new Color(0.2f, 0.1f, 0f);

        TextMeshProUGUI subtitle = CreateText(_panel.transform, "SubTitle", new Vector2(0f, 195f), "冒险者，请登录", 24);
        subtitle.color = new Color(0.7f, 0.75f, 0.85f);

        // 输入框
        _usernameInput = CreateInput(_panel.transform, "UsernameInput", new Vector2(0f, 80f), "用户名", false);
        _passwordInput = CreateInput(_panel.transform, "PasswordInput", new Vector2(0f, 20f), "密码", true);

        // 按钮(悬停放大 + 变色)
        CreateButton(_panel.transform, "LoginBtn", new Vector2(-120f, -100f), "登 录", OnLoginClicked);
        CreateButton(_panel.transform, "RegisterBtn", new Vector2(120f, -100f), "注 册", OnRegisterClicked);

        // 状态文本
        _statusText = CreateText(_panel.transform, "StatusText", new Vector2(0f, -160f), "", 22);

        // 登录转圈(圆环, 默认隐藏)
        GameObject spinnerGo = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
        spinnerGo.transform.SetParent(_panel.transform, false);
        _spinner = spinnerGo.GetComponent<RectTransform>();
        _spinner.anchoredPosition = new Vector2(0f, -110f);
        _spinner.sizeDelta = new Vector2(30f, 30f);
        Image spinnerImg = spinnerGo.GetComponent<Image>();
        spinnerImg.sprite = CreateRingSprite();
        spinnerImg.color = new Color(1f, 0.9f, 0.5f);
        spinnerImg.enabled = false;

        // 底部版本号
        TextMeshProUGUI footer = CreateText(canvasGo.transform, "Footer", new Vector2(0f, -40f), "Demo v1.0  |  服务器 127.0.0.1:8888", 16);
        footer.color = new Color(0.6f, 0.6f, 0.65f);

        // ---- 开始界面(StartPanel): 大标题 + 开始游戏/退出游戏 ----
        _startPanel = new GameObject("StartPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _startPanel.transform.SetParent(canvasGo.transform, false);
        _startRect = _startPanel.GetComponent<RectTransform>();
        _startRect.anchorMin = Vector2.zero;
        _startRect.anchorMax = Vector2.one;
        _startRect.offsetMin = Vector2.zero;
        _startRect.offsetMax = Vector2.zero;
        _startPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f); // 轻微暗化, 让标题更突出
        _startGroup = _startPanel.GetComponent<CanvasGroup>();

        // 大标题(呼吸效果在 Update 里)
        _startTitle = CreateText(_startPanel.transform, "StartTitle", new Vector2(0f, 150f), "原神 FPS", 96);
        _startTitle.color = _titleBase;
        _startTitle.outlineWidth = 0.3f;
        _startTitle.outlineColor = new Color(0.2f, 0.1f, 0f);

        TextMeshProUGUI tagline = CreateText(_startPanel.transform, "Tagline", new Vector2(0f, 70f), "欢迎来到提瓦特大陆", 26);
        tagline.color = new Color(0.75f, 0.8f, 0.9f);

        // 开始游戏(主按钮, 大一点)
        CreateButton(_startPanel.transform, "StartGameBtn", new Vector2(0f, -60f), "开 始 游 戏", OnStartClicked, 280f, 72f);

        // 退出游戏(副按钮, 暗灰色)
        Button exitBtn = CreateButton(_startPanel.transform, "ExitBtn", new Vector2(0f, -170f), "退 出 游 戏", OnExitClicked, 220f, 56f);
        ColorBlock eb = exitBtn.colors;
        eb.normalColor = new Color(0.4f, 0.43f, 0.5f);
        eb.highlightedColor = new Color(0.5f, 0.55f, 0.62f);
        eb.pressedColor = new Color(0.28f, 0.3f, 0.36f);
        eb.selectedColor = eb.highlightedColor;
        exitBtn.colors = eb;

        // ---- 加载界面(LoadingPanel): 登录成功后显示, 异步加载游戏场景 ----
        _loadingPanel = new GameObject("LoadingPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        _loadingPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform lrt = _loadingPanel.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        _loadingPanel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.07f, 0.97f); // 深色遮罩, 盖住下层界面
        _loadingGroup = _loadingPanel.GetComponent<CanvasGroup>();
        _loadingPanel.SetActive(false); // 默认隐藏

        // 加载标题
        TextMeshProUGUI loadingTitle = CreateText(_loadingPanel.transform, "LoadingTitle", new Vector2(0f, 130f), "正在进入提瓦特...", 40);
        loadingTitle.color = _titleBase;

        // 进度条背景
        GameObject barBg = new GameObject("ProgressBg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(_loadingPanel.transform, false);
        RectTransform barRt = barBg.GetComponent<RectTransform>();
        barRt.anchoredPosition = new Vector2(0f, 30f);
        barRt.sizeDelta = new Vector2(560f, 14f);
        barBg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);

        // 进度条填充(锚定左端, 靠改宽度实现从左往右增长)
        GameObject barFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barBg.transform, false);
        RectTransform frt = barFill.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(0f, 0.5f);
        frt.anchorMax = new Vector2(0f, 0.5f);
        frt.pivot = new Vector2(0f, 0.5f);
        frt.anchoredPosition = Vector2.zero;
        frt.sizeDelta = new Vector2(560f, 14f);
        _loadingFill = barFill.GetComponent<Image>();
        _loadingFill.color = new Color(1f, 0.85f, 0.45f);

        // 百分比文本
        _loadingPercent = CreateText(_loadingPanel.transform, "Percent", new Vector2(0f, -15f), "0%", 20);
        _loadingPercent.color = new Color(0.8f, 0.85f, 0.95f);

        // 随机提示语
        _loadingTip = CreateText(_loadingPanel.transform, "Tip", new Vector2(0f, -120f), "", 20);
        _loadingTip.color = new Color(0.6f, 0.65f, 0.75f);

        // 生成中提示(文本 + 转圈, 默认隐藏, 场景激活前才显示)
        _generatingGroup = new GameObject("Generating", typeof(RectTransform));
        _generatingGroup.transform.SetParent(_loadingPanel.transform, false);
        RectTransform grt = _generatingGroup.GetComponent<RectTransform>();
        grt.anchoredPosition = new Vector2(0f, -80f);
        grt.sizeDelta = new Vector2(500f, 40f);
        _generatingGroup.SetActive(false);

        TextMeshProUGUI generatingText = CreateText(_generatingGroup.transform, "GeneratingText", new Vector2(30f, 0f), "正在生成世界...", 24);
        generatingText.color = _titleBase;

        GameObject loadingSpinnerGo = new GameObject("Spinner", typeof(RectTransform), typeof(Image));
        loadingSpinnerGo.transform.SetParent(_generatingGroup.transform, false);
        _loadingSpinner = loadingSpinnerGo.GetComponent<RectTransform>();
        _loadingSpinner.anchoredPosition = new Vector2(-210f, 0f);
        _loadingSpinner.sizeDelta = new Vector2(28f, 28f);
        Image lspinImg = loadingSpinnerGo.GetComponent<Image>();
        lspinImg.sprite = CreateRingSprite();
        lspinImg.color = new Color(1f, 0.9f, 0.5f);

        // ---- 音效: 一个 AudioSource 复用; PlayOneShot 支持连续触发、互不打断 ----
        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
        _sfx.volume = 0.8f;
        _clickSfx = Resources.Load<AudioClip>("Audio/UI/click2");
        _hoverSfx = Resources.Load<AudioClip>("Audio/UI/rollover1");
        _successSfx = Resources.Load<AudioClip>("Audio/UI/switch16");
        _errorSfx = Resources.Load<AudioClip>("Audio/UI/switch17");

        // 登录期间锁定游戏操作(静态方法, 不依赖场景实例)
        UIManager.EnterUIBlock();

        // 先显示开始界面, 登录卡片默认隐藏, 点"开始游戏"后再显示
        _panel.SetActive(false);
        StartCoroutine(StartEntrance());
    }

    // ==================== 每帧特效 ====================

    private void Update()
    {
        float t = Time.time;

        // 星星闪烁(只改alpha)
        for (int i = 0; i < _stars.Count; i++)
        {
            float a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 0.6f + _starPhases[i]));
            Image img = _stars[i].GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, a);
        }

        // 流星移动 + 循环复用
        for (int i = 0; i < _meteors.Count; i++)
        {
            Vector2 pos = _meteors[i].anchoredPosition + _meteorVels[i] * Time.deltaTime;
            // 飞出屏幕就重置到顶部(复用, 不新建)
            if (pos.y < -60f || pos.x < -80f || pos.x > 2000f)
            {
                ResetMeteor(_meteors[i], out Vector2 vel);
                _meteorVels[i] = vel;
            }
            else
            {
                _meteors[i].anchoredPosition = pos;
            }
        }

        // 标题呼吸
        float breathe = 0.85f + 0.15f * (0.5f + 0.5f * Mathf.Sin(t * 1.8f));
        if (_startTitle != null && _startPanel.activeSelf) _startTitle.color = _titleBase * breathe; // 开始界面标题
        _title.color = _titleBase * breathe;

        // 登录转圈
        if (_connecting && _spinner != null)
        {
            _spinner.Rotate(0f, 0f, -320f * Time.deltaTime);
        }
    }

    /// <summary>
    /// 重置一条流星: 随机顶部位置 + 斜向下速度
    /// </summary>
    private void ResetMeteor(RectTransform mrt, out Vector2 vel)
    {
        mrt.anchoredPosition = new Vector2(Random.Range(200f, 1900f), Random.Range(900f, 1180f));
        float speed = Random.Range(700f, 1200f);
        vel = new Vector2(-speed, -speed * 0.35f); // 斜向左下
        float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
        mrt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 卡片入场: 从下方淡入上移
    /// </summary>
    private IEnumerator CardEntrance()
    {
        _panelGroup.alpha = 0f;
        Vector2 from = _panelRect.anchoredPosition + new Vector2(0f, -80f);
        Vector2 to = _panelRect.anchoredPosition;

        float duration = 0.8f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f); // 缓出
            _panelRect.anchoredPosition = Vector2.Lerp(from, to, eased);
            _panelGroup.alpha = p;
            yield return null;
        }
        _panelRect.anchoredPosition = to;
        _panelGroup.alpha = 1f;
    }

    /// <summary>
    /// 开始界面入场: 从半透明淡入 + 轻微放大
    /// </summary>
    private IEnumerator StartEntrance()
    {
        _startGroup.alpha = 0f;
        _startRect.localScale = Vector3.one * 0.96f;
        float duration = 0.6f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f); // 缓出
            _startGroup.alpha = eased;
            _startRect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
            yield return null;
        }
        _startGroup.alpha = 1f;
        _startRect.localScale = Vector3.one;
    }

    /// <summary>
    /// 点"开始游戏": 淡出开始界面 -> 显示登录卡片
    /// </summary>
    private void OnStartClicked()
    {
        if (_switching) return; // 防止连点导致重复切换
        _switching = true;
        StartCoroutine(SwitchToLogin());
    }

    private IEnumerator SwitchToLogin()
    {
        // 先淡出开始界面
        float duration = 0.25f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _startGroup.alpha = 1f - t / duration;
            yield return null;
        }
        _startGroup.alpha = 0f;
        _startPanel.SetActive(false);

        // 再显示登录卡片并播放入场动画
        _panel.SetActive(true);
        StartCoroutine(CardEntrance());
    }

    /// <summary>
    /// 退出游戏: 编辑器里停止播放, 打包后关闭程序
    /// </summary>
    private void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================== 登录/注册逻辑 ====================

    private void OnLoginClicked() => Submit(false);
    private void OnRegisterClicked() => Submit(true);

    private void Submit(bool isRegister)
    {
        string user = _usernameInput.text.Trim();
        string pwd = _passwordInput.text;
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd))
        {
            PlayErrorSound();
            SetStatus("请输入用户名和密码", Color.red);
            return;
        }
        if (_connecting) return;
        _connecting = true;
        if (_spinner != null) _spinner.GetComponent<Image>().enabled = true;
        SetStatus(isRegister ? "注册中..." : "连接服务器中...", Color.yellow);

        GameClient.Instance.Connect(connected =>
        {
            _connecting = false;
            if (_spinner != null) _spinner.GetComponent<Image>().enabled = false;
            if (!connected)
            {
                PlayErrorSound();
                SetStatus("无法连接服务器，请先启动 GameServer", Color.red);
                return;
            }
            if (isRegister)
            {
                SetStatus("注册中...", Color.yellow);
                GameClient.Instance.Register(user, pwd);
            }
            else
            {
                SetStatus("登录中...", Color.yellow);
                GameClient.Instance.Login(user, pwd);
            }
        });
    }

    private void OnLoginResult(LoginResult r)
    {
        if (!r.success)
        {
            PlayErrorSound();
            SetStatus(r.msg, Color.red);
            return;
        }
        PlaySuccessSound();
        SetStatus("登录成功！", Color.green);
        ToastUI.ShowMessage($"欢迎回来，{r.username}！", new Color(0.4f, 1f, 0.5f));
        EnterGame();
    }

    private void OnRegisterResult(RegisterResult r)
    {
        if (!r.success)
        {
            PlayErrorSound();
            SetStatus(r.msg, Color.red);
            return;
        }
        SetStatus("注册成功，正在登录...", Color.green);
        GameClient.Instance.Login(_usernameInput.text.Trim(), _passwordInput.text);
    }

    /// <summary>
    /// 登录成功: 解锁输入 -> 显示加载界面 -> 异步加载游戏场景
    /// </summary>
    private void EnterGame()
    {
        UIManager.ExitUIBlock();
        StartCoroutine(LoadGameScene());
    }

    /// <summary>
    /// 异步加载游戏场景: 进度条平滑走完后才激活场景, 避免黑屏卡顿
    /// </summary>
    private IEnumerator LoadGameScene()
    {
        // 隐藏登录卡片, 显示加载界面
        _panel.SetActive(false);
        _loadingPanel.SetActive(true);
        _loadingGroup.alpha = 0f;
        _lastPercent = -1;
        SetLoadingProgress(0f);
        _loadingTip.text = _loadingTips[Random.Range(0, _loadingTips.Length)];

        // 立即开始异步加载, 与淡入重叠, 不浪费等待时间
        AsyncOperation op = SceneManager.LoadSceneAsync("SampleScene");
        op.allowSceneActivation = false;

        // 淡入(此时加载已经同步开始了)
        float fade = 0f;
        while (fade < 0.3f)
        {
            fade += Time.deltaTime;
            _loadingGroup.alpha = Mathf.Clamp01(fade / 0.3f);
            yield return null;
        }
        _loadingGroup.alpha = 1f;

        // 阶段1: 真实进度 0~0.9(场景已加载但未激活), 进度条平滑跟随, 最多走到 90%
        float display = 0f;
        while (op.progress < 0.9f)
        {
            display = Mathf.MoveTowards(display, op.progress / 0.9f, Time.deltaTime * 0.4f);
            SetLoadingProgress(display * 0.9f);
            yield return null;
        }

        // 阶段2: 场景已就绪, 用固定 0.6 秒把进度条从当前位置补到 99%
        float start = display * 0.9f;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / 0.6f);
            SetLoadingProgress(Mathf.Lerp(start, 0.99f, p));
            yield return null;
        }
        SetLoadingProgress(1f);

        // 阶段3: 显示"正在生成世界..."+ 转圈, 掩盖激活瞬间的冻结
        if (_generatingGroup != null)
        {
            _generatingGroup.SetActive(true);
            float wait = 0f;
            while (wait < 0.8f)
            {
                wait += Time.deltaTime;
                _loadingSpinner.Rotate(0f, 0f, -240f * Time.deltaTime);
                yield return null;
            }
            _generatingGroup.SetActive(false);
        }

        // 真正激活并切换到游戏场景
        op.allowSceneActivation = true;
    }

    /// <summary>
    /// 更新进度条宽度和百分比文本(参数 0~1)
    /// </summary>
    private void SetLoadingProgress(float p)
    {
        p = Mathf.Clamp01(p);
        _loadingFill.rectTransform.sizeDelta = new Vector2(560f * p, 14f);
        // 整数百分比没变就不刷新文本, 避免每帧触发 TMP 重建
        int percent = Mathf.RoundToInt(p * 100f);
        if (percent != _lastPercent)
        {
            _lastPercent = percent;
            _loadingPercent.text = percent + "%";
        }
    }

    private void SetStatus(string msg, Color color)
    {
        if (_statusText != null)
        {
            _statusText.text = msg;
            _statusText.color = color;
        }
    }

    // ==================== 音效播放 ====================

    private void PlayClickSound() => PlaySfx(_clickSfx);
    private void PlayHoverSound() => PlaySfx(_hoverSfx);
    private void PlaySuccessSound() => PlaySfx(_successSfx);
    private void PlayErrorSound() => PlaySfx(_errorSfx);

    /// <summary>
    /// 统一播放入口: 资源缺失时静默跳过; PlayOneShot 不打断正在播的其他音效
    /// </summary>
    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || _sfx == null) return;
        _sfx.PlayOneShot(clip);
    }

    // ==================== 贴图生成 ====================

    /// <summary>深蓝->紫 渐变背景</summary>
    private Sprite CreateGradientSprite()
    {
        int w = 2, h = 256;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float t = y / (float)(h - 1);
            Color c = Color.Lerp(new Color(0.03f, 0.05f, 0.11f), new Color(0.12f, 0.2f, 0.38f), t);
            tex.SetPixel(0, y, c);
            tex.SetPixel(1, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    /// <summary>小圆点(星星用)</summary>
    private RectTransform CreateDot(Transform parent, float size)
    {
        GameObject go = new GameObject("Star", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
        go.GetComponent<Image>().color = Color.white;
        return rt;
    }

    /// <summary>流星彗尾: 高亮头 -> 透明尾</summary>
    private Sprite CreateStreakSprite()
    {
        int w = 256, h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float along = 1f - x / (float)(w - 1); // 头在左
                float fade = Mathf.Pow(along, 1.7f);
                float edge = Mathf.Clamp01(1f - Mathf.Abs(y - h * 0.5f) / (h * 0.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, fade * edge));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
    }

    /// <summary>圆环(登录转圈用)</summary>
    private Sprite CreateRingSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = size * 0.5f, cy = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float a = (d >= 22f && d <= 30f) ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ==================== UI 元素创建 ====================

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, string text, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400f, 40f);

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.font = UITextHelper.GetFont();
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.text = text;
        return t;
    }

    private TMP_InputField CreateInput(Transform parent, string name, Vector2 pos, string placeholder, bool isPassword)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(460f, 56f);
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();

        TextMeshProUGUI textComp = CreateText(go.transform, "Text", new Vector2(0f, 0f), "", 24);
        textComp.alignment = TextAlignmentOptions.Left;
        RectTransform trt = textComp.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(14f, 0f); trt.offsetMax = new Vector2(-14f, 0f);
        input.textComponent = textComp;

        TextMeshProUGUI ph = CreateText(go.transform, "Placeholder", new Vector2(0f, 0f), placeholder, 24);
        ph.color = new Color(1f, 1f, 1f, 0.4f);
        ph.alignment = TextAlignmentOptions.Left;
        RectTransform prt = ph.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(14f, 0f); prt.offsetMax = new Vector2(-14f, 0f);
        input.placeholder = ph;

        if (isPassword) input.contentType = TMP_InputField.ContentType.Password;
        return input;
    }

    private Button CreateButton(Transform parent, string name, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick, float width = 180f, float height = 60f)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, height);

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);
        btn.onClick.AddListener(PlayClickSound); // 新增: 点击音效
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.25f, 0.5f, 1f);
        cb.highlightedColor = new Color(0.35f, 0.62f, 1f);
        cb.pressedColor = new Color(0.15f, 0.35f, 0.8f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;

        // 悬停放大 + 悬停音效(EventTrigger, 事件驱动)
        EventTrigger trigger = go.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
        {
            StartCoroutine(ScaleTo(rt, 1.06f));
            PlayHoverSound();
        });
        AddTrigger(trigger, EventTriggerType.PointerExit, () => StartCoroutine(ScaleTo(rt, 1f)));

        TextMeshProUGUI t = CreateText(go.transform, "Label", new Vector2(0f, 0f), label, 26);
        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return btn;
    }

    private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener(_ => action());
        trigger.triggers.Add(entry);
    }

    private IEnumerator ScaleTo(RectTransform rt, float target)
    {
        Vector3 from = rt.localScale;
        Vector3 to = Vector3.one * target;
        float t = 0f;
        float dur = 0.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(from, to, t / dur);
            yield return null;
        }
        rt.localScale = to;
    }
}
