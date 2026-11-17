using UnityEngine;

/// <summary>
/// 战术对局 UI 显隐。建议挂在 GameManager、Canvas 或任意常驻物体上，
/// 不要把脚本挂在会被它自己关闭的 PlayerCanvas 上。
/// </summary>
[DisallowMultipleComponent]
public sealed class TacticalUIVisibilityController : MonoBehaviour
{
    [Header("需要控制的界面")]
    public GameObject playerCanvas;
    [Tooltip("PlayerCanvas 下的 HP 根节点")]
    public GameObject healthRoot;
    public GameObject tacticalHudRoot;

    [Tooltip("离开战术模式后恢复普通 PlayerCanvas")]
    public bool restorePlayerCanvasOutsidePvp = true;

    private bool _subscribed;
    private bool _hasControlledPvp;

    private void Awake()
    {
        if (playerCanvas == null && UIManager.INSTANCE != null)
            playerCanvas = UIManager.INSTANCE.PlayerCanvas;
    }

    private void OnEnable()
    {
        TrySubscribe();
        ApplyVisibility();
    }

    private void Start()
    {
        TrySubscribe();
        ApplyVisibility();
    }

    private void Update()
    {
        if (!_subscribed)
            TrySubscribe();

        // 离开房间不会保证再收到 RoundState，因此每帧保留一次轻量检查。
        ApplyVisibility();
    }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null)
            return;

        BattleClient.Instance.OnRoundState += HandleRoundState;
        BattleClient.Instance.OnMatchResult += HandleMatchResult;
        BattleClient.Instance.OnRoomChanged += ApplyVisibility;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
        {
            BattleClient.Instance.OnRoundState -= HandleRoundState;
            BattleClient.Instance.OnMatchResult -= HandleMatchResult;
            BattleClient.Instance.OnRoomChanged -= ApplyVisibility;
        }
        _subscribed = false;
    }

    private void HandleRoundState(RoundStateDto dto)
    {
        ApplyVisibility();
    }

    private void HandleMatchResult(MatchResultDto dto)
    {
        SetVisible(false);
    }

    private void ApplyVisibility()
    {
        bool tacticalRoom = RoomSession.CurrentMode == "pvp" && RoomSession.InRoom;
        if (!tacticalRoom)
        {
            if (_hasControlledPvp && restorePlayerCanvasOutsidePvp)
            {
                if (playerCanvas != null)
                    playerCanvas.SetActive(true);
                if (healthRoot != null)
                    healthRoot.SetActive(false);
                if (tacticalHudRoot != null)
                    tacticalHudRoot.SetActive(false);
            }
            _hasControlledPvp = false;
            return;
        }

        _hasControlledPvp = true;
        RoundStateDto round = BattleClient.Instance != null
            ? BattleClient.Instance.LastRound
            : null;

        // 只有正式战斗阶段显示。购买、选角、回合结束和新回合准备都隐藏。
        bool inActiveMatch = round != null
            && !round.matchOver
            && round.phase == 2;
        SetVisible(inActiveMatch);
    }

    private void SetVisible(bool visible)
    {
        if (playerCanvas != null && playerCanvas.activeSelf != visible)
            playerCanvas.SetActive(visible);

        if (healthRoot != null && healthRoot.activeSelf != visible)
            healthRoot.SetActive(visible);

        if (tacticalHudRoot != null && tacticalHudRoot.activeSelf != visible)
            tacticalHudRoot.SetActive(visible);
    }
}
