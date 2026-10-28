using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 房间列表 + 创建 + 加入
/// </summary>
public class RoomListUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelRoot;

    [Header("列表")]
    public Transform content;
    public RoomItemUI itemPrefab;
    public GameObject emptyHint;
    public float autoRefreshInterval = 3f;

    [Header("创建")]
    public TMP_InputField roomNameInput;
    public TMP_Dropdown maxPlayersDropdown;   // 可留空
    public Button createBtn;

    [Header("按钮")]
    public Button refreshBtn;
    public Button closeBtn;

    [Header("加入成功回调(可在 Inspector 里挂场景切换)")]
    public UnityEvent onJoinedRoom;

    private readonly List<RoomItemUI> _items = new List<RoomItemUI>();
    private bool _busy;
    private float _busyTimer;
    private float _timer;
    private bool _blocking;   // 是否已经进入 UI 锁定状态

    private const float BusyTimeout = 6f;   // 请求超时时间(秒)

    private void Awake()
    {
        if (createBtn != null) createBtn.onClick.AddListener(OnClickCreate);
        if (refreshBtn != null) refreshBtn.onClick.AddListener(Refresh);
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        var gc = GameClient.Instance;
        if (gc == null) return;

        gc.OnRoomListResult += HandleRoomList;
        gc.OnCreateRoomResult += HandleCreate;
        gc.OnJoinRoomResult += HandleJoin;

        _busy = false;
        _busyTimer = 0f;
        _timer = 0f;
        Refresh();
    }

    private void OnDisable()
    {
        var gc = GameClient.Instance;
        if (gc != null)
        {
            gc.OnRoomListResult -= HandleRoomList;
            gc.OnCreateRoomResult -= HandleCreate;
            gc.OnJoinRoomResult -= HandleJoin;
        }

        // 面板被隐藏/销毁时兜底解锁鼠标
        ReleaseUIBlock();
    }

    private void Update()
    {
        // 面板显示时每 3 秒自动刷新一次
        if (panelRoot != null && !panelRoot.activeSelf) return;

        // 请求卡住时自动解锁, 否则按钮会永久点不动
        if (_busy)
        {
            _busyTimer += Time.unscaledDeltaTime;
            if (_busyTimer >= BusyTimeout)
            {
                _busy = false;
                _busyTimer = 0f;
                ToastUI.ShowMessage("服务器无响应，请稍后重试", new Color(1f, 0.6f, 0.4f));
            }
            return;
        }

        _timer += Time.unscaledDeltaTime;
        if (_timer >= autoRefreshInterval)
        {
            _timer = 0f;
            Refresh();
        }
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (!_blocking)
        {
            UIManager.EnterUIBlock();
            _blocking = true;
        }
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ReleaseUIBlock();
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;

        // 场景切换时 UIManager 可能已经重置过计数, 这时再减会变成负数
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }

    public void Refresh()
    {
        if (GameClient.Instance == null) return;
        if (!GameClient.Instance.IsConnected) return;

        GameClient.Instance.RequestRoomList();
    }

    private void OnClickCreate()
    {
        if (_busy) return;

        if (GameClient.Instance == null)
        {
            Debug.LogWarning("[RoomListUI] GameClient 不存在, 无法创建房间");
            ToastUI.ShowMessage("未连接服务器");
            return;
        }

        if (!GameClient.Instance.IsConnected || !GameClient.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[RoomListUI] 未连接或未登录, 无法创建房间");
            ToastUI.ShowMessage("尚未连接服务器，请重新登录", new Color(1f, 0.6f, 0.4f));
            return;
        }

        string name = roomNameInput != null ? roomNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name))
            name = GameClient.Instance.LoggedInUser + " 的房间";

        int maxPlayers = 2;
        if (maxPlayersDropdown != null)
            maxPlayers = Mathf.Clamp(maxPlayersDropdown.value + 2, 2, 4);

        _busy = true;
        _busyTimer = 0f;
        Debug.Log($"[RoomListUI] 创建房间: name={name}, maxPlayers={maxPlayers}");
        GameClient.Instance.CreateRoom(name, maxPlayers);
    }

    private void OnClickJoin(string roomId)
    {
        if (_busy) return;
        if (GameClient.Instance == null) return;

        if (!GameClient.Instance.IsConnected)
        {
            ToastUI.ShowMessage("尚未连接服务器，请重新登录", new Color(1f, 0.6f, 0.4f));
            return;
        }

        _busy = true;
        _busyTimer = 0f;
        Debug.Log($"[RoomListUI] 加入房间: {roomId}");
        GameClient.Instance.JoinRoom(roomId);
    }

    private void HandleRoomList(RoomListResult result)
    {
        if (!result.success)
        {
            Debug.LogWarning("[RoomListUI] 获取房间失败: " + result.msg);
            ToastUI.ShowMessage("获取房间失败: " + result.msg);
            return;
        }

        Rebuild(result.rooms);
    }

    private void HandleCreate(RoomResult result)
    {
        _busy = false;
        _busyTimer = 0f;

        if (!result.success)
        {
            Debug.LogWarning("[RoomListUI] 创建失败: " + result.msg);
            ToastUI.ShowMessage("创建失败: " + result.msg);
            return;
        }

        Success(result, "创建成功, 房间 " + result.roomId);
    }

    private void HandleJoin(RoomResult result)
    {
        _busy = false;
        _busyTimer = 0f;

        if (!result.success)
        {
            Debug.LogWarning("[RoomListUI] 加入失败: " + result.msg);
            ToastUI.ShowMessage("加入失败: " + result.msg);
            return;
        }

        Success(result, "加入成功, 房间 " + result.roomId);
    }

    private void Success(RoomResult result, string msg)
    {
        ToastUI.ShowMessage(msg);
        RoomSession.Enter(result);
        Close();
        onJoinedRoom?.Invoke();
    }

    private void Rebuild(List<RoomInfo> rooms)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i >= _items.Count)
            {
                var item = Instantiate(itemPrefab, content);
                _items.Add(item);
            }

            _items[i].gameObject.SetActive(true);
            _items[i].Bind(rooms[i], OnClickJoin);
        }

        for (int i = rooms.Count; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);

        if (emptyHint != null) emptyHint.SetActive(rooms.Count == 0);
    }
}
