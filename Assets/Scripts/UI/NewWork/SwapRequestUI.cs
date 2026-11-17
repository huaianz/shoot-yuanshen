using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>收到换位请求时的弹窗(5 秒自动关闭)</summary>
public class SwapRequestUI : MonoBehaviour
{
    public GameObject panelRoot;
    public TMP_Text messageText;
    public TMP_Text countdownText;
    public Button acceptBtn;
    public Button rejectBtn;

    private SwapIncomingDto _current;
    private float _remain;
    private bool _blocking;
    private bool _subscribed;

    private void Awake()
    {
        if (acceptBtn != null) acceptBtn.onClick.AddListener(Accept);
        if (rejectBtn != null) rejectBtn.onClick.AddListener(Reject);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;
        BattleClient.Instance.OnSwapIncoming += HandleIncoming;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
            BattleClient.Instance.OnSwapIncoming -= HandleIncoming;
        _subscribed = false;

        ReleaseUIBlock();
    }

    private void HandleIncoming(SwapIncomingDto dto)
    {
        if (dto == null) return;

        _current = dto;
        _remain = Mathf.Max(1f, dto.expiresInMs / 1000f);

        if (messageText != null)
            messageText.text = $"{dto.requesterName} 想和你交换位置";

        if (panelRoot != null) panelRoot.SetActive(true);

        if (!_blocking)
        {
            UIManager.EnterUIBlock();
            _blocking = true;
        }
    }

    private void Update()
    {
        TrySubscribe();

        if (_current == null) return;

        _remain -= Time.unscaledDeltaTime;
        if (countdownText != null)
            countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, _remain)).ToString();

        if (_remain <= 0f) Close();
    }

    private void Accept() => Respond(true);
    private void Reject() => Respond(false);

    private void Respond(bool accept)
    {
        if (_current == null) return;

        BattleClient.Instance.SendSwapResponse(_current.requestId, accept);
        Close();
    }

    private void Close()
    {
        _current = null;

        if (panelRoot != null) panelRoot.SetActive(false);
        ReleaseUIBlock();
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }
}
