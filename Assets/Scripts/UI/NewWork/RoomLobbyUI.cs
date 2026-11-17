using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>战术房间的准备大厅: 席位 / 准备 / 房主开始</summary>
public class RoomLobbyUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelRoot;
    public TMP_Text roomNameText;
    public TMP_Text modeText;
    public TMP_Text statusText;

    [Header("席位")]
    public Transform allyColumn;
    public Transform enemyColumn;
    public LobbySlotUI slotPrefab;
    public int slotHeight = 90;

    [Header("按钮")]
    public Button readyBtn;
    public TMP_Text readyText;
    public Button startBtn;
    public Button leaveBtn;

    private readonly List<LobbySlotUI> _allySlots = new List<LobbySlotUI>();
    private readonly List<LobbySlotUI> _enemySlots = new List<LobbySlotUI>();
    private LobbyStateDto _last;
    private bool _blocking;
    private bool _subscribed;

    private void Awake()
    {
        if (readyBtn != null) readyBtn.onClick.AddListener(ToggleReady);
        if (startBtn != null) startBtn.onClick.AddListener(StartMatch);
        if (leaveBtn != null) leaveBtn.onClick.AddListener(LeaveRoom);
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

        BattleClient.Instance.OnLobbyState += HandleLobby;
        BattleClient.Instance.OnSwapResult += HandleSwapResult;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
        {
            BattleClient.Instance.OnLobbyState -= HandleLobby;
            BattleClient.Instance.OnSwapResult -= HandleSwapResult;
        }
        _subscribed = false;

        ReleaseUIBlock();
    }

    private void Update()
    {
        // BattleClient 可能比 UI 晚初始化, 这里兜底重试订阅
        TrySubscribe();

        // 不在房间里时自动收起(比如从别处退出房间)
        if (panelRoot != null && panelRoot.activeSelf && !RoomSession.InRoom)
            Hide();
    }

    private void HandleLobby(LobbyStateDto dto)
    {
        if (dto == null) return;
        _last = dto;

        if (dto.started)
        {
            Hide();
            return;
        }

        if (RoomSession.CurrentMode != "pvp")
        {
            Debug.Log($"[Lobby] 忽略大厅: 当前模式={RoomSession.CurrentMode}");
            Hide();
            return;
        }

        if (panelRoot != null && !panelRoot.activeSelf)
            Debug.Log($"[Lobby] 显示大厅 room={dto.roomId} capacity={dto.capacity} players={dto.players.Count}");

        Show();
        Rebuild(dto);
        RefreshButtons(dto);
    }

    private void Show()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (!_blocking)
        {
            UIManager.EnterUIBlock();
            _blocking = true;
        }
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ReleaseUIBlock();
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }

    private void Rebuild(LobbyStateDto dto)
    {
        int teamSize = Mathf.Clamp(dto.capacity / 2, 1, 3);

        if (roomNameText != null) roomNameText.text = "房间 " + dto.roomId;
        if (modeText != null) modeText.text = $"{teamSize}v{teamSize} 战术模式";

        BuildColumn(allyColumn, _allySlots, teamSize, dto, 0);
        BuildColumn(enemyColumn, _enemySlots, teamSize, dto, 1);
    }

    private void BuildColumn(Transform column, List<LobbySlotUI> slots, int count,
                             LobbyStateDto dto, int team)
    {
        if (column == null || slotPrefab == null) return;

        while (slots.Count < count)
        {
            var slot = Instantiate(slotPrefab, column);
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            bool used = i < count;
            slots[i].gameObject.SetActive(used);
            if (!used) continue;

            int teamLocal = team;
            int slotLocal = i;
            LobbyPlayerDto player = null;
            foreach (var p in dto.players)
                if (p.team == team && p.slot == i) { player = p; break; }

            bool isSelf = player != null && player.id == BattleClient.Instance.MyPlayerId;

            slots[i].Bind(player, isSelf, () => OnSlotClicked(teamLocal, slotLocal, player));
        }
    }

    private void OnSlotClicked(int team, int slot, LobbyPlayerDto player)
    {
        if (BattleClient.Instance == null) return;

        if (player == null)
        {
            BattleClient.Instance.SendMoveSlot(team, slot);
            return;
        }

        if (player.id == BattleClient.Instance.MyPlayerId) return;

        bool full = _last != null && _last.players.Count >= _last.capacity;
        if (!full)
        {
            ToastUI.ShowMessage("还有空位，请点空位移动", new Color(1f, 0.75f, 0.4f));
            return;
        }

        BattleClient.Instance.SendSwapRequest(player.id);
    }

    private void HandleSwapResult(SwapResultDto dto)
    {
        if (dto == null) return;

        ToastUI.ShowMessage(dto.reason,
            dto.accepted ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.6f, 0.4f));
    }

    private void RefreshButtons(LobbyStateDto dto)
    {
        bool isHost = dto.hostId == BattleClient.Instance.MyPlayerId;
        LobbyPlayerDto me = null;
        bool allReady = dto.players.Count > 0;
        int attack = 0, defense = 0;

        foreach (var p in dto.players)
        {
            if (p.id == BattleClient.Instance.MyPlayerId) me = p;
            if (!p.ready) allReady = false;
            if (p.team == 0) attack++; else defense++;
        }

        if (readyBtn != null)
        {
            readyBtn.gameObject.SetActive(true);
            if (readyText != null)
                readyText.text = me != null && me.ready ? "取消准备" : "准备";
        }

        if (startBtn != null)
        {
            startBtn.gameObject.SetActive(isHost);
            startBtn.interactable = isHost && allReady && attack > 0 && defense > 0;
        }

        if (statusText != null)
        {
            statusText.text = isHost
                ? (allReady ? "所有人已准备，可以开始" : "等待其他玩家准备")
                : "等待房主开始比赛";
        }
    }

    private void ToggleReady()
    {
        Debug.Log($"[Lobby] 点击准备, last={(_last != null)}");

        if (_last == null)
        {
            Debug.LogWarning("[Lobby] 还没收到大厅状态, 无法准备");
            return;
        }

        bool ready = false;
        foreach (var p in _last.players)
            if (p.id == BattleClient.Instance.MyPlayerId) ready = p.ready;

        BattleClient.Instance.SendSetReady(!ready);
        Debug.Log($"[Lobby] 发送准备={!ready}");
    }

    private void StartMatch()
    {
        BattleClient.Instance.SendStartMatch();
    }

    private void LeaveRoom()
    {
        Hide();
        RoomSession.Leave();
        ToastUI.ShowMessage("已退出房间", new Color(0.8f, 0.95f, 1f));
    }
}
