using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 设置面板: 打开时暂停游戏, 包含音量/音乐开关/继续/返回主菜单/退出
/// ESC 打开与关闭
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    [Header("引用(在Inspector里拖进来)")]
    public Slider bgmSlider;           // 音量滑块
    public Toggle bgmToggle;           // 音乐开关
    public TextMeshProUGUI volumeText; // 音量百分比文本
    public Button closeBtn;            // 继续游戏(关闭面板)

    [Header("暂停菜单按钮")]
    public Button mainMenuBtn;         // 返回主菜单
    public Button quitBtn;             // 退出游戏

    private void Start()
    {
        // 绑定事件（先清空再绑，防止重复绑定）
        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(ClosePanel);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (bgmToggle != null)
        {
            bgmToggle.onValueChanged.RemoveAllListeners();
            bgmToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (mainMenuBtn != null)
        {
            mainMenuBtn.onClick.RemoveAllListeners();
            mainMenuBtn.onClick.AddListener(BackToMainMenu);
        }

        if (quitBtn != null)
        {
            quitBtn.onClick.RemoveAllListeners();
            quitBtn.onClick.AddListener(QuitGame);
        }
    }

    private void Update()
    {
        // ESC: 面板开着就关闭(继续游戏), 没开且没有其他界面时打开(暂停)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (gameObject.activeSelf)
            {
                ClosePanel();
            }
            else if (!UIManager.IsAnyUIOpen)
            {
                OpenPanel();
            }
        }
    }

    /// <summary>打开设置 = 暂停游戏</summary>
    public void OpenPanel()
    {
        if (gameObject.activeSelf) return;   // 防止重复打开导致计数错乱

        // 打开时用当前设置刷新 UI，保证显示的是真实状态
        if (bgmSlider != null) bgmSlider.value = AudioManager.INSTANCE.BGMVolume;
        if (bgmToggle != null) bgmToggle.isOn = AudioManager.INSTANCE.BGMEnabled;
        RefreshVolumeText();

        gameObject.SetActive(true);
        UIManager.EnterUIBlock();       // 显示鼠标、隐藏 HUD
        Time.timeScale = 0f;            // 冻结游戏
    }

    /// <summary>关闭设置 = 继续游戏</summary>
    private void ClosePanel()
    {
        gameObject.SetActive(false);
        Time.timeScale = 1f;            // 恢复游戏
        UIManager.ExitUIBlock();
        PlayerPrefs.Save();             // 一次性写盘
    }

    /// <summary>返回主菜单: 先存盘再回登录场景</summary>
    private void BackToMainMenu()
    {
        Time.timeScale = 1f;
        if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.UploadNow();
        UIManager.ResetUIState();   // 清掉UI计数残留, 防止重进游戏HUD消失/角色不能动
        SceneManager.LoadScene("LoginScene");
    }

    /// <summary>退出游戏: 编辑器里停止播放, 打包后关闭程序</summary>
    private void QuitGame()
    {
        Time.timeScale = 1f;
        if (CloudSaveManager.Instance != null) CloudSaveManager.Instance.UploadNow();
        UIManager.ResetUIState();   // 清掉UI计数残留
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnVolumeChanged(float v)
    {
        AudioManager.INSTANCE.SetBGMVolume(v);
        RefreshVolumeText();
    }

    private void OnToggleChanged(bool on)
    {
        AudioManager.INSTANCE.SetBGMEnabled(on);
    }

    private void RefreshVolumeText()
    {
        if (volumeText != null)
        {
            volumeText.text = Mathf.RoundToInt(AudioManager.INSTANCE.BGMVolume * 100) + "%";
        }
    }
}