using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>联机房间里的"开始挑战"按钮: 由服务器决定何时刷第一波</summary>
public class CoopWaveStarter : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;
    public TMP_Text label;

    [Header("可选: 点击后关闭的门")]
    public DoorController door;

    [Header("文字")]
    public string idleText = "开始挑战";
    public string runningText = "第 {0} 波进行中";
    public string notReadyText = "未进入房间";

    private float _refreshTimer;

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnWaveState += HandleWave;

        Refresh();
    }

    private void OnDisable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnWaveState -= HandleWave;
    }

    private void Update()
    {
        // 低频刷新, 避免每帧改 TMP 文本
        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer < 0.5f) return;

        _refreshTimer = 0f;
        Refresh();
    }

    private void OnClick()
    {
        var bc = BattleClient.Instance;
        if (!CanUse(bc))
        {
            ToastUI.ShowMessage(notReadyText, new Color(1f, 0.65f, 0.35f));
            return;
        }

        if (bc.CurrentWave > 0)
        {
            ToastUI.ShowMessage("挑战已经开始了", new Color(1f, 0.65f, 0.35f));
            return;
        }

        bc.SendStartWave();

        if (door != null) door.Close();

        ToastUI.ShowMessage("已请求开始挑战，等待服务器刷怪", new Color(0.8f, 0.95f, 1f));
        Refresh();
    }

    private void HandleWave(WaveStateDto dto)
    {
        Refresh();
    }

    private void Refresh()
    {
        var bc = BattleClient.Instance;
        bool ready = CanUse(bc);
        bool started = bc != null && bc.CurrentWave > 0;

        if (startButton != null)
        {
            startButton.gameObject.SetActive(ready || started);
            startButton.interactable = ready && !started;
        }

        if (label != null)
        {
            if (!ready && !started) label.text = notReadyText;
            else if (started) label.text = string.Format(runningText, bc.CurrentWave);
            else label.text = idleText;
        }
    }

    private static bool CanUse(BattleClient bc)
    {
        return bc != null && bc.IsConnected && bc.MyPlayerId > 0 && RoomSession.InRoom;
    }
}
