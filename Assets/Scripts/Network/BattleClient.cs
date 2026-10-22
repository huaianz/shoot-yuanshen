using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

/// <summary>战斗服客户端: 房间连接 / 输入上报 / 快照接收 / 心跳 / 自动重连</summary>
public class BattleClient : MonoBehaviour
{
    public static BattleClient Instance;

    [Header("战斗服地址(由网关下发, 这里只是默认值)")]
    public string host = "127.0.0.1";
    public int port = 7777;

    [Header("上报频率(Hz)")]
    public int inputRate = 20;

    [Header("心跳与重连")]
    public float heartbeatInterval = 1f;   // 必须小于服务器超时(5s)
    public float reconnectDelay = 2f;
    public int maxReconnect = 5;
    [Header("人为延迟(毫秒): 0=关闭, 实验时设 50/100/200")]
    public int artificialLatencyMs = 0;

    private class DelayedPacket { public long DueMs; public byte[] Data; }
    private readonly List<DelayedPacket> _delayed = new List<DelayedPacket>();

    public bool IsConnected { get; private set; }
    public int MyPlayerId { get; private set; }
    public string RoomId { get; private set; }
    public int LatestServerTick { get; private set; }
    public SnapshotDto LatestSnapshot { get; private set; }

    // ===== 网络统计=====
    public int PingMs { get; private set; }
    public float UpKBps { get; private set; }
    public float DownKBps { get; private set; }
    public long BytesSent { get; private set; }
    public long BytesReceived { get; private set; }

    /// <summary>
    /// 快照缓冲(远端插值用)
    /// </summary>
    public SnapshotBuffer Buffer { get; private set; } = new SnapshotBuffer();

    /// <summary>
    /// 收到状态快照
    /// </summary>
    public event Action<SnapshotDto> OnSnapshot;
    /// <summary>
    /// 有玩家加入/离开: (playerId, joined)
    /// </summary>
    public event Action<int, bool> OnPlayerJoinLeave;
    /// <summary>
    /// 收到伤害结果(第 4 步做命中提示/伤害数字时用)
    /// </summary>
    public event Action<DamageResultDto> OnDamageResult;
    private TcpClient _tcp;
    private NetworkStream _stream;
    private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();

    private float _inputTimer;
    private float _heartbeatTimer;
    private float _reconnectTimer;
    private int _inputTick;
    private int _reconnectCount;

    private long _lastPingSentMs;
    private float _statTimer;
    private long _lastBytesSent;
    private long _lastBytesReceived;
    private string _lastSceneName;
    private bool _teleportPending;
    public int MyServerHp { get; private set; } = 100;

    private void Awake() { Instance = this; }

    /// <summary>由房间流程调用: 拿到网关下发的战斗服地址后连接</summary>
    public void ConnectTo(string ip, int battlePort, string roomId)
    {
        host = ip;
        port = battlePort;
        RoomId = roomId;
        _reconnectCount = 0;
        _reconnectTimer = 0f;
        _ = ConnectAsync();
    }

    /// <summary>主动断开(离开房间时用)</summary>
    public void Disconnect()
    {
        RoomId = null;
        IsConnected = false;
        try { _stream?.Close(); } catch { }
        try { _tcp?.Close(); } catch { }
        _stream = null;
        _tcp = null;
        MyPlayerId = 0;
        Debug.Log("[BattleClient] 已断开战斗服");
    }

    private async Task ConnectAsync()
    {
        if (IsConnected) return;
        try
        {
            _tcp = new TcpClient();
            await _tcp.ConnectAsync(host, port);
            _stream = _tcp.GetStream();
            IsConnected = true;
            _reconnectCount = 0;
            _inputTimer = _heartbeatTimer = 0f;
            Debug.Log($"[BattleClient] 已连接战斗服 {host}:{port}, 房间={RoomId}");
            _ = ReadLoop();
            SendJoin();
        }
        catch (Exception e)
        {
            IsConnected = false;
            Debug.LogWarning($"[BattleClient] 连接战斗服失败: {e.Message}");
        }
    }

    private void SendJoin()
    {
        var pc = PlayerController.INSTANCE;
        Vector3 pos = Vector3.zero;
        if (pc != null)
        {
            pos = pc.currentPlayerModel != null ? pc.currentPlayerModel.transform.position : pc.transform.position;
        }

        var req = new JoinRequestDto
        {
            playerId = GameClient.Instance != null ? GameClient.Instance.LoggedInUser : "local",
            token = GameClient.Instance != null ? GameClient.Instance.Token : "",
            roomId = RoomId,
            x = pos.x,
            y = pos.y,
            z = pos.z
        };
        Send(BattleMsgId.Join, JsonUtility.ToJson(req));
    }

    private async Task ReadLoop()
    {
        try
        {
            while (IsConnected)
            {
                byte[] header = await ReadExactly(8);
                if (header == null) break;

                int len = BitConverter.ToInt32(header, 0);
                int msgId = BitConverter.ToInt32(header, 4);
                if (len <= 0 || len > 65536) break;

                byte[] body = await ReadExactly(len);
                if (body == null) break;

                string json = Encoding.UTF8.GetString(body);
                BytesReceived += 8 + len;              // 统计下行字节
                _mainThread.Enqueue(() => Dispatch(msgId, json));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BattleClient] 读循环异常: " + e.Message);
        }

        IsConnected = false;
        Debug.Log("[BattleClient] 与战斗服的连接已断开");
    }

    private void Update()
    {
        // 人为延迟: 到点的包才真正发出
        if (_delayed.Count > 0 && _stream != null)
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            for (int i = _delayed.Count - 1; i >= 0; i--)
            {
                if (nowMs < _delayed[i].DueMs) continue;
                try { _stream.Write(_delayed[i].Data, 0, _delayed[i].Data.Length); } catch { }
                _delayed.RemoveAt(i);
            }
        }
        while (_mainThread.TryDequeue(out var act)) act?.Invoke();

        // 场景切换后第一帧标记为"传送", 让服务器跳过一次距离校验
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != _lastSceneName)
        {
            _lastSceneName = sceneName;
            _teleportPending = true;
        }

        // 带宽统计: 每秒算一次
        _statTimer += Time.deltaTime;
        if (_statTimer >= 1f)
        {
            UpKBps = (BytesSent - _lastBytesSent) / 1024f / _statTimer;
            DownKBps = (BytesReceived - _lastBytesReceived) / 1024f / _statTimer;
            _lastBytesSent = BytesSent;
            _lastBytesReceived = BytesReceived;
            _statTimer = 0f;
        }

        if (IsConnected)
        {
            // 1) 心跳(1 秒一次)
            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= heartbeatInterval)
            {
                _heartbeatTimer = 0f;
                _lastPingSentMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Send(BattleMsgId.Heartbeat, "{\"t\":" + _lastPingSentMs + "}");
            }

            // 2) 输入上报(20Hz)
            _inputTimer += Time.deltaTime;
            float interval = 1f / inputRate;
            if (_inputTimer >= interval)
            {
                _inputTimer -= interval;
                SendInput();
            }
        }
        else if (!string.IsNullOrEmpty(RoomId) && _reconnectCount < maxReconnect)
        {
            // 3) 自动重连
            _reconnectTimer += Time.deltaTime;
            if (_reconnectTimer >= reconnectDelay)
            {
                _reconnectTimer = 0f;
                _reconnectCount++;
                Debug.Log($"[BattleClient] 断线重连第 {_reconnectCount}/{maxReconnect} 次");
                _ = ConnectAsync();
            }
        }
    }

    private void SendInput()
    {
        var pc = PlayerController.INSTANCE;
        if (pc == null) return;

        Vector3 pos = pc.currentPlayerModel != null ? pc.currentPlayerModel.transform.position : pc.transform.position;

        var cmd = new InputCommandDto
        {
            tick = ++_inputTick,
            moveX = pc.worldMovement.x,
            moveZ = pc.worldMovement.z,
            yaw = pc.transform.eulerAngles.y,
            buttons = (pc.isSprint ? 1 : 0) | (pc.isAiming ? 2 : 0) | (pc.isFire ? 4 : 0),
            clientTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            x = pos.x,
            y = pos.y,
            z = pos.z,
            animId = GetAnimId(pc.currentPlayerModel),
            teleport = _teleportPending
        };
        _teleportPending = false;      // 传送标记只发一次
        Send(BattleMsgId.Input, JsonUtility.ToJson(cmd));
    }

    private void Dispatch(int msgId, string json)
    {
        switch (msgId)
        {
            case BattleMsgId.JoinAccepted:
                {
                    var dto = JsonUtility.FromJson<JoinAcceptedDto>(json);
                    MyPlayerId = dto.playerId;
                    foreach (var p in dto.players)
                        if (p.id == MyPlayerId)
                        {
                            MyServerHp = p.hp;
                            break;
                        }
                    RoomId = dto.roomId;
                    LatestServerTick = dto.tick;
                    Debug.Log($"[BattleClient] 加入战斗服成功: 我的id={MyPlayerId}, 房间={RoomId}");
                    break;
                }
            case BattleMsgId.Snapshot:
                {
                    var dto = JsonUtility.FromJson<SnapshotDto>(json);
                    LatestSnapshot = dto;
                    LatestServerTick = dto.tick;
                    Buffer.Push(dto);              // 入插值缓冲
                    foreach (var p in dto.players)
                        if (p.id == MyPlayerId)
                        {
                            MyServerHp = p.hp;
                            break;
                        }
                    OnSnapshot?.Invoke(dto);
                    break;
                }
            case BattleMsgId.Heartbeat:
                {
                    // 服务器原样回传, 用它算 RTT
                    long t = ExtractLong(json, "t");
                    if (t > 0) PingMs = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - t);
                    break;
                }
            case BattleMsgId.PlayerJoinLeave:
                {
                    // 服务器发的格式: {"playerId":2,"joined":true}
                    int id = ExtractInt(json, "playerId");
                    bool joined = json.Contains("true");
                    OnPlayerJoinLeave?.Invoke(id, joined);
                    break;
                }
            //伤害结算
            case BattleMsgId.Damage:
                {
                    var dto = JsonUtility.FromJson<DamageResultDto>(json);
                    if (dto.shooterId == MyPlayerId) HitsConfirmed++;
                    if (dto.targetId == MyPlayerId) MyServerHp = dto.targetHp;
                    Debug.Log($"[BattleClient] 伤害: 射手={dto.shooterId} 目标={dto.targetId} 伤害={dto.damage} 目标HP={dto.targetHp} 击杀={dto.isKill}");
                    OnDamageResult?.Invoke(dto);
                    break;
                }
        }
    }

    private void Send(int msgId, string json)
    {
        if (_stream == null) return;
        byte[] data = BattlePack.Pack(msgId, json);
        BytesSent += data.Length;

        if (artificialLatencyMs > 0)
        {
            _delayed.Add(new DelayedPacket
            {
                DueMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + artificialLatencyMs,
                Data = data
            });
            return;
        }

        try { _stream.Write(data, 0, data.Length); }
        catch (Exception e) { Debug.LogWarning("[BattleClient] 发送失败: " + e.Message); }
    }

    private async Task<byte[]> ReadExactly(int count)
    {
        byte[] buf = new byte[count];
        int total = 0;
        while (total < count)
        {
            int n = await _stream.ReadAsync(buf, total, count - total);
            if (n <= 0) return null;
            total += n;
        }
        return buf;
    }

    /// <summary>
    /// 简易 JSON 取整数(避免为了两个字段再写 DTO)
    /// </summary>
    private static int ExtractInt(string json, string key)
    {
        int idx = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
        if (idx < 0) return 0;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return 0;
        int end = colon + 1;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
        return int.TryParse(json.Substring(colon + 1, end - colon - 1), out int v) ? v : 0;
    }

    /// <summary>
    /// 把玩家状态映射成动作编号(联机同步用)
    /// </summary>
    private static int GetAnimId(PlayerModel m)
    {
        if (m == null) return 0;
        switch (m.CurrentStateEnum)
        {
            case PlayerState.Move: return 1;
            case PlayerState.Aiming: return 2;
            case PlayerState.Crouch: return 3;
            case PlayerState.Climb: return 4;
            case PlayerState.Slide: return 5;
            case PlayerState.Dodge: return 6;
            case PlayerState.Hover: return 7;
            default: return 0;   // Idle
        }
    }

    /// <summary>
    /// 简易 JSON 取 long(心跳时间戳用)
    /// </summary>
    private static long ExtractLong(string json, string key)
    {
        int idx = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
        if (idx < 0) return 0;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return 0;
        int end = colon + 1;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
        return long.TryParse(json.Substring(colon + 1, end - colon - 1), out long v) ? v : 0;
    }

    #region 开火方法和计数
    public int ShotsSent { get; private set; }
    public int HitsConfirmed { get; private set; }

    /// <summary>
    /// 开火上报(服务器做权威命中判定)
    /// </summary>
    public void SendFire(Vector3 origin, Vector3 dir)
    {
        if (!IsConnected || MyPlayerId <= 0) return;

        var dto = new FireEventDto
        {
            playerId = MyPlayerId,
            ox = origin.x,
            oy = origin.y,
            oz = origin.z,
            dx = dir.x,
            dy = dir.y,
            dz = dir.z,
            fireTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            pingMs = PingMs
        };
        ShotsSent++;
        Send(BattleMsgId.Fire, JsonUtility.ToJson(dto));
    }
    #endregion
}