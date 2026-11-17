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
    public event Action<EnemyHitDto> OnEnemyHit;
    public event Action<WaveStateDto> OnWaveState;
    public event Action<EnemyShotDto> OnEnemyShot;
    public event Action<PlayerShotDto> OnPlayerShot;
    public event Action<RoundStateDto> OnRoundState;
    public event Action<LobbyStateDto> OnLobbyState;
    public event Action<SwapIncomingDto> OnSwapIncoming;
    public event Action<SwapResultDto> OnSwapResult;
    public event Action<CharacterSelectStateDto> OnCharacterSelectState;
    public event Action<BuyItemResultDto> OnBuyItemResult;
    public event Action<MatchResultDto> OnMatchResult;
    public event Action<SpikeStateDto> OnSpikeState;
    public event Action<RewardDto> OnReward;
    public event Action<BuyResultDto> OnBuyResult;
    public event Action OnRoomChanged;
    public int MatchCoins { get; private set; }
    public int CurrentWave { get; private set; }
    public int MyTeam { get; private set; } = -1;
    public int MyArmor { get; private set; }
    public int MyMatchWeaponId { get; private set; }
    public int MyMatchWeaponDamage { get; private set; }
    public RoundStateDto LastRound { get; private set; }
    public LobbyStateDto LastLobby { get; private set; }
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
    private int _connectionGen;   // 连接代次: 换房/重连时作废旧读循环

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
        _connectionGen++;   // 让旧的读循环失效, 不会影响下一次连接
        RoomId = null;
        IsConnected = false;
        try { _stream?.Close(); } catch { }
        try { _tcp?.Close(); } catch { }
        _stream = null;
        _tcp = null;
        MyPlayerId = 0;

        // 丢弃旧连接积压在主线程队列里的消息
        while (_mainThread.TryDequeue(out _)) { }

        Debug.Log("[BattleClient] 已断开战斗服");
    }

    private async Task ConnectAsync()
    {
        if (IsConnected) return;

        int gen = ++_connectionGen;

        try
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port);

            // 连接期间被断开/换了房间: 这条连接直接作废
            if (gen != _connectionGen)
            {
                try { tcp.Close(); } catch { }
                return;
            }

            _tcp = tcp;
            _stream = tcp.GetStream();
            IsConnected = true;
            _reconnectCount = 0;
            _inputTimer = _heartbeatTimer = 0f;
            _ = ReadLoop(gen, _stream);
            SendJoin();
        }
        catch (Exception e)
        {
            if (gen != _connectionGen) return;
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
            mode = RoomSession.CurrentMode,
            maxPlayers = RoomSession.CurrentMaxPlayers,
            x = pos.x,
            y = pos.y,
            z = pos.z
        };
        Send(BattleMsgId.Join, JsonUtility.ToJson(req));
    }

    private async Task ReadLoop(int gen, NetworkStream stream)
    {
        try
        {
            while (gen == _connectionGen && IsConnected)
            {
                byte[] header = await ReadExactly(stream, 8);
                if (header == null) break;

                int len = BitConverter.ToInt32(header, 0);
                int msgId = BitConverter.ToInt32(header, 4);
                if (len <= 0 || len > 65536) break;

                byte[] body = await ReadExactly(stream, len);
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

        if (gen != _connectionGen) return;   // 旧连接的退出, 不应影响新连接

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

        // 角色的转向发生在模型节点上(PlayerMoveState/AimingState 都是转 playerModel),
        // 根节点 PlayerController 的 yaw 基本不变, 所以这里必须取模型的朝向
        Transform modelT = pc.currentPlayerModel != null ? pc.currentPlayerModel.transform : pc.transform;
        Vector3 pos = modelT.position;

        var cmd = new InputCommandDto
        {
            tick = ++_inputTick,
            moveX = pc.worldMovement.x,
            moveZ = pc.worldMovement.z,
            yaw = modelT.eulerAngles.y,
            buttons = (pc.isSprint ? 1 : 0) | (pc.isAiming ? 2 : 0) | (pc.isFire ? 4 : 0),
            clientTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            x = pos.x,
            y = pos.y,
            z = pos.z,
            animId = GetAnimId(pc.currentPlayerModel),
            weaponId = pc.currentPlayerModel != null && pc.currentPlayerModel.weapon != null
                ? GetWeaponIdForNetwork(pc.currentPlayerModel.weapon)
                : -1,
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
                    MatchCoins = dto.matchCoins;   // 以服务器为准(重连时恢复)
                    CurrentWave = dto.wave;
                    foreach (var p in dto.players)
                        if (p.id == MyPlayerId)
                        {
                            MyServerHp = p.hp;
                            MyTeam = p.team;
                            MatchCoins = p.matchCoins;
                            MyArmor = p.armor;
                            MyMatchWeaponId = p.matchWeaponId;
                            MyMatchWeaponDamage = p.matchWeaponDamage;
                            break;
                        }
                    string oldRoom = RoomId;
                    RoomId = dto.roomId;
                    LatestServerTick = dto.tick;

                    // 换房间: 清掉上一房间的快照和远端对象
                    if (oldRoom != RoomId)
                    {
                        Buffer.Clear();
                        OnRoomChanged?.Invoke();
                    }
                    Debug.Log($"[BattleClient] 加入战斗服成功: 我的id={MyPlayerId}, 房间={RoomId}, 房内玩家={dto.players.Count}");

                    // 大厅状态可能比 JoinAccepted 先到, 拿到自己的 id 后重刷一次
                    if (LastLobby != null) OnLobbyState?.Invoke(LastLobby);
                    break;
                }
            case BattleMsgId.Snapshot:
                {
                    var dto = JsonUtility.FromJson<SnapshotDto>(json);

                    // 换房时旧连接的迟到包: 房间对不上直接丢掉
                    if (!string.IsNullOrEmpty(dto.roomId) && dto.roomId != RoomId)
                        break;

                    LatestSnapshot = dto;
                    LatestServerTick = dto.tick;
                    Buffer.Push(dto);              // 入插值缓冲
                    foreach (var p in dto.players)
                        if (p.id == MyPlayerId)
                        {
                            MyServerHp = p.hp;
                            MyTeam = p.team;
                            MatchCoins = p.matchCoins;
                            MyArmor = p.armor;
                            MyMatchWeaponId = p.matchWeaponId;
                            MyMatchWeaponDamage = p.matchWeaponDamage;
                            break;
                        }
                    OnSnapshot?.Invoke(dto);
                    break;
                }
            case BattleMsgId.RoundState:
                {
                    var dto = JsonUtility.FromJson<RoundStateDto>(json);
                    LastRound = dto;
                    OnRoundState?.Invoke(dto);
                    break;
                }
            case BattleMsgId.LobbyState:
                {
                    var dto = JsonUtility.FromJson<LobbyStateDto>(json);
                    LastLobby = dto;
                    OnLobbyState?.Invoke(dto);
                    break;
                }
            case BattleMsgId.SwapIncoming:
                {
                    var dto = JsonUtility.FromJson<SwapIncomingDto>(json);
                    OnSwapIncoming?.Invoke(dto);
                    break;
                }
            case BattleMsgId.SwapResult:
                {
                    var dto = JsonUtility.FromJson<SwapResultDto>(json);
                    OnSwapResult?.Invoke(dto);
                    break;
                }
            case BattleMsgId.CharacterSelectState:
                {
                    var dto = JsonUtility.FromJson<CharacterSelectStateDto>(json);
                    OnCharacterSelectState?.Invoke(dto);
                    break;
                }
            case BattleMsgId.BuyItemResult:
                {
                    var dto = JsonUtility.FromJson<BuyItemResultDto>(json);
                    MyArmor = dto.armor;
                    MatchCoins = dto.coins;
                    MyServerHp = dto.hp;
                    MyMatchWeaponId = dto.matchWeaponId;
                    MyMatchWeaponDamage = dto.matchDamage;
                    OnBuyItemResult?.Invoke(dto);
                    break;
                }
            case BattleMsgId.MatchResult:
                {
                    var dto = JsonUtility.FromJson<MatchResultDto>(json);
                    OnMatchResult?.Invoke(dto);
                    break;
                }
            case BattleMsgId.SpikeState:
                {
                    var dto = JsonUtility.FromJson<SpikeStateDto>(json);
                    OnSpikeState?.Invoke(dto);
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
                    if (dto.attackerKind == 0 && dto.shooterId == MyPlayerId) HitsConfirmed++;
                    if (dto.targetId == MyPlayerId)
                    {
                        MyServerHp = dto.targetHp;
                        MyArmor = dto.targetArmor;
                    }
                    OnDamageResult?.Invoke(dto);
                    break;
                }
            case BattleMsgId.EnemyHit:
                {
                    var dto = JsonUtility.FromJson<EnemyHitDto>(json);
                    if (dto.shooterId == MyPlayerId) HitsConfirmed++;
                    OnEnemyHit?.Invoke(dto);
                    break;
                }
            case BattleMsgId.WaveState:
                {
                    var dto = JsonUtility.FromJson<WaveStateDto>(json);
                    CurrentWave = dto.wave;
                    OnWaveState?.Invoke(dto);
                    break;
                }
            case BattleMsgId.EnemyShot:
                {
                    var dto = JsonUtility.FromJson<EnemyShotDto>(json);
                    OnEnemyShot?.Invoke(dto);
                    break;
                }
            case BattleMsgId.PlayerShot:
                {
                    var dto = JsonUtility.FromJson<PlayerShotDto>(json);
                    OnPlayerShot?.Invoke(dto);
                    break;
                }
            case BattleMsgId.Reward:
                {
                    var dto = JsonUtility.FromJson<RewardDto>(json);
                    if (dto.playerId == MyPlayerId) MatchCoins = dto.total;
                    OnReward?.Invoke(dto);
                    break;
                }
            case BattleMsgId.BuyResult:
                {
                    var dto = JsonUtility.FromJson<BuyResultDto>(json);
                    if (dto.myHp > 0) MyServerHp = dto.myHp;
                    OnBuyResult?.Invoke(dto);
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

    private async Task<byte[]> ReadExactly(NetworkStream stream, int count)
    {
        if (stream == null) return null;

        byte[] buf = new byte[count];
        int total = 0;
        while (total < count)
        {
            int n = await stream.ReadAsync(buf, total, count - total);
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

    public void SendBuy(int itemId)
    {
        if (!IsConnected) return;
        Send(BattleMsgId.Buy, "{\"itemId\":" + itemId + "}");
    }

    /// <summary>请求服务器开始挑战(刷第一波)</summary>
    public void SendStartWave()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.StartWave, "{}");
    }

    public void SendSetReady(bool ready)
    {
        if (!IsConnected) return;
        Send(BattleMsgId.SetReady, "{\"ready\":" + (ready ? "true" : "false") + "}");
    }

    public void SendStartMatch()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.StartMatch, "{}");
    }

    public void SendMoveSlot(int team, int slot)
    {
        if (!IsConnected) return;
        Send(BattleMsgId.MoveSlot, "{\"team\":" + team + ",\"slot\":" + slot + "}");
    }

    public void SendSwapRequest(int targetId)
    {
        if (!IsConnected) return;
        Send(BattleMsgId.SwapRequest, "{\"targetId\":" + targetId + "}");
    }

    public void SendSwapResponse(int requestId, bool accept)
    {
        if (!IsConnected) return;
        Send(BattleMsgId.SwapResponse,
            "{\"requestId\":" + requestId + ",\"accept\":" + (accept ? "true" : "false") + "}");
    }

    public void SendSelectCharacter(int characterId)
    {
        if (!IsConnected) return;
        Send(BattleMsgId.SelectCharacter, "{\"characterId\":" + characterId + "}");
    }

    public void SendConfirmCharacter()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.ConfirmCharacter, "{}");
    }

    public void SendBuyItem(int itemId)
    {
        string category = itemId == 1 || itemId == 2
            ? "armor"
            : (itemId == 3 ? "supply" : "weapon");
        SendBuyItem(itemId, category);
    }

    public void SendBuyItem(int itemId, string category)
    {
        if (!IsConnected) return;

        Weapon template = InventoryManager.INSTANCE != null
            ? InventoryManager.INSTANCE.weaponData?.GetWeaponByID(itemId)
            : null;
        var dto = new BuyItemDto
        {
            itemId = itemId,
            category = string.IsNullOrEmpty(category) ? "weapon" : category,
            matchDamage = template != null ? template.matchDamage : 0,
            matchPrice = template != null ? template.matchPrice : 0,
            matchFireIntervalMs = template != null ? template.matchFireIntervalMs : 0
        };
        Send(BattleMsgId.BuyItem, JsonUtility.ToJson(dto));
    }

    public void SendReturnToLobby()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.ReturnToLobby, "{}");
    }

    public void SendPlantSpike()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.PlantSpike, "{}");
    }

    public void SendDefuseSpike()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.DefuseSpike, "{}");
    }

    public void SendPickupSpike()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.PickupSpike, "{}");
    }

    public void SendDropSpike()
    {
        if (!IsConnected) return;
        Send(BattleMsgId.DropSpike, "{}");
    }

    /// <summary>退出房间后清空本局数据</summary>
    public void ClearRoomState()
    {
        MyPlayerId = 0;
        MyTeam = -1;
        MyServerHp = 100;
        MyArmor = 0;
        MyMatchWeaponId = 0;
        MyMatchWeaponDamage = 0;
        MatchCoins = 0;
        CurrentWave = 0;
        LatestSnapshot = null;
        Buffer.Clear();
        OnRoomChanged?.Invoke();
    }

    /// <summary>
    /// 由场景传送/死亡回城调用: 下一次输入包跳过距离校验
    /// </summary>
    public void MarkTeleport()
    {
        _teleportPending = true;
    }
    #endregion

    private static int GetWeaponIdForNetwork(PlayerWeapon weapon)
    {
        if (RoomSession.CurrentMode == "pvp"
            && RoomSession.InRoom
            && Instance != null
            && Instance.MyMatchWeaponId > 0)
            return Instance.MyMatchWeaponId;
        return weapon != null ? weapon.CurrentWeaponID : -1;
    }
}
