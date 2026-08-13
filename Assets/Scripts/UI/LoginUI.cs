using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 登录/注册界面: 自动创建(不需要在场景里摆东西)。
/// 游戏启动时创建, 登录成功后隐藏并解锁游戏操作。
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

    private GameObject _panel;
    private TMP_InputField _usernameInput;
    private TMP_InputField _passwordInput;
    private TextMeshProUGUI _statusText;
    private bool _connecting;

    public static LoginUI CreateNew()
    {
        GameObject go = new GameObject("LoginUI");
        DontDestroyOnLoad(go);
        LoginUI ui = go.AddComponent<LoginUI>();
        ui.BuildUI();
        return ui;
    }

    /// <summary>
    /// 运行时搭建整个登录界面(EventSystem + Canvas + 输入框 + 按钮)
    /// </summary>
    private void BuildUI()
    {
        // 1. 输入框/按钮需要 EventSystem(场景里没有才创建)
        if (EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // 2. Canvas
        GameObject canvasGo = new GameObject("LoginCanvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000; // 最高层
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>(); // 登录界面需要接收点击

        // 3. 半透明黑底面板
        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.3f, 0.3f);
        panelRt.anchorMax = new Vector2(0.7f, 0.7f);
        _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

        // 4. 标题
        TextMeshProUGUI title = CreateText(_panel.transform, "Title", new Vector2(0f, 110f), "原神 FPS - 登录", 40);
        title.color = new Color(1f, 0.9f, 0.5f);

        // 5. 用户名 / 密码输入框
        _usernameInput = CreateInput(_panel.transform, "UsernameInput", new Vector2(0f, 40f), "用户名", false);
        _passwordInput = CreateInput(_panel.transform, "PasswordInput", new Vector2(0f, -20f), "密码", true);

        // 6. 登录 / 注册按钮
        CreateButton(_panel.transform, "LoginBtn", new Vector2(-90f, -90f), "登录", OnLoginClicked);
        CreateButton(_panel.transform, "RegisterBtn", new Vector2(90f, -90f), "注册", OnRegisterClicked);

        // 7. 状态提示文本
        _statusText = CreateText(_panel.transform, "StatusText", new Vector2(0f, -140f), "", 22);

        // 登录期间锁定游戏操作
        UIManager.EnterUIBlock();
    }

    private void OnEnable()
    {
        GameClient.Instance.OnLoginResult += OnLoginResult;
        GameClient.Instance.OnRegisterResult += OnRegisterResult;
        GameClient.Instance.OnPlayerDataResult += OnPlayerDataResult;
    }

    private void OnDisable()
    {
        GameClient.Instance.OnLoginResult -= OnLoginResult;
        GameClient.Instance.OnRegisterResult -= OnRegisterResult;
        GameClient.Instance.OnPlayerDataResult -= OnPlayerDataResult;
    }

    private void OnLoginClicked()
    {
        Submit(false);
    }

    private void OnRegisterClicked()
    {
        Submit(true);
    }

    private void Submit(bool isRegister)
    {
        string user = _usernameInput.text.Trim();
        string pwd = _passwordInput.text;
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pwd))
        {
            SetStatus("请输入用户名和密码", Color.red);
            return;
        }
        if (_connecting) return;
        _connecting = true;
        SetStatus("连接服务器中...", Color.yellow);

        GameClient.Instance.Connect(connected =>
        {
            _connecting = false;
            if (!connected)
            {
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
            SetStatus(r.msg, Color.red);
            return;
        }
        SetStatus("登录成功！", Color.green);
        EnterGame();
    }

    private void OnRegisterResult(RegisterResult r)
    {
        if (!r.success)
        {
            SetStatus(r.msg, Color.red);
            return;
        }
        SetStatus("注册成功，正在登录...", Color.green);
        // 注册成功后自动登录
        GameClient.Instance.Login(_usernameInput.text.Trim(), _passwordInput.text);
    }

    private void OnPlayerDataResult(PlayerDataResult r)
    {
        if (r.success)
        {
            ToastUI.ShowMessage($"欢迎回来，{GameClient.Instance.LoggedInUser}！(金币 {r.coin})", new Color(0.4f, 1f, 0.5f));
        }
    }

    private void EnterGame()
    {
        UIManager.ExitUIBlock();   // 解锁游戏操作
        _panel.SetActive(false);   // 隐藏登录界面
    }

    private void SetStatus(string msg, Color color)
    {
        if (_statusText != null)
        {
            _statusText.text = msg;
            _statusText.color = color;
        }
    }

    // ===== UI 创建辅助 =====

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
        rt.sizeDelta = new Vector2(320f, 44f);
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        TMP_InputField input = go.AddComponent<TMP_InputField>();

        // 输入文字
        TextMeshProUGUI textComp = CreateText(go.transform, "Text", new Vector2(0f, 0f), "", 24);
        textComp.alignment = TextAlignmentOptions.Left;
        RectTransform trt = textComp.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 0f);
        trt.offsetMax = new Vector2(-12f, 0f);
        input.textComponent = textComp;

        // 占位文字
        TextMeshProUGUI ph = CreateText(go.transform, "Placeholder", new Vector2(0f, 0f), placeholder, 24);
        ph.color = new Color(1f, 1f, 1f, 0.4f);
        ph.alignment = TextAlignmentOptions.Left;
        RectTransform prt = ph.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(12f, 0f);
        prt.offsetMax = new Vector2(-12f, 0f);
        input.placeholder = ph;

        if (isPassword)
        {
            input.contentType = TMP_InputField.ContentType.Password;
        }
        return input;
    }

    private void CreateButton(Transform parent, string name, Vector2 pos, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(140f, 50f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.9f);

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        TextMeshProUGUI t = CreateText(go.transform, "Label", new Vector2(0f, 0f), label, 26);
        RectTransform trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }
}