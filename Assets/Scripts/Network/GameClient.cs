using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 游戏客户端网络: 连接 GameServer, 发送/接收消息
/// 收消息在异步线程 -> 塞进队列 -> Update 在主线程统一处理
/// </summary>
public class GameClient : MonoBehaviour
{
    // 懒加载单例
    private static GameClient _instance;
    public static GameClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameClient>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameClient");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<GameClient>();
                }
            }
            return _instance;
        }
    }

    [Header("服务器地址")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 8888;

    // 消息ID
    public const int MsgHeartbeat = 1;
    public const int MsgRegister = 100;
    public const int MsgLogin = 101;
    public const int MsgGetPlayerData = 200;
    public const int MsgSavePlayerData = 201;

    // 事件
    public event Action<LoginResult> OnLoginResult;
    public event Action<RegisterResult> OnRegisterResult;
    public event Action<PlayerDataResult> OnPlayerDataResult;

    private TcpClient _client;
    private NetworkStream _stream;
    private bool _connected;

    // 线程安全队列: 收消息线程往这放, 主线程每帧取
    private readonly ConcurrentQueue<Action> _inbox = new ConcurrentQueue<Action>();

    // 复用缓冲
    private readonly byte[] _lenBuf = new byte[4];
    private readonly byte[] _idBuf = new byte[4];

    // 登录状态
    public string Token { get; private set; }
    public string LoggedInUser { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    private void Update()
    {
        // 主线程: 把收消息线程积压的处理动作一次性取完执行
        while (_inbox.TryDequeue(out Action act))
        {
            try { act(); }
            catch (Exception ex) { Debug.LogError($"[网络] 处理消息异常: {ex}"); }
        }
    }

    private void OnDestroy()
    {
        _connected = false;
        _client?.Close();
    }

    public void Connect(Action<bool> onResult)
    {
        if (_connected) { onResult?.Invoke(true); return; }
        _ = ConnectAsync(onResult);
    }

    private async Task ConnectAsync(Action<bool> onResult)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(serverIP, serverPort);
            _stream = _client.GetStream();
            _connected = true;
            Debug.Log($"[网络] 已连接 {serverIP}:{serverPort}");
            _ = ReceiveLoopAsync();
            onResult?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[网络] 连接失败: {ex.Message}");
            _connected = false;
            onResult?.Invoke(false);
        }
    }

    /// <summary>
    /// 收消息循环
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (_connected && _client != null)
            {
                if (!await ReadExactlyAsync(_lenBuf, 4)) break;
                if (!await ReadExactlyAsync(_idBuf, 4)) break;

                int len = BitConverter.ToInt32(_lenBuf, 0);
                if (len <= 0 || len > 65536) break;

                byte[] body = new byte[len];
                if (!await ReadExactlyAsync(body, len)) break;

                int msgId = BitConverter.ToInt32(_idBuf, 0);
                string json = Encoding.UTF8.GetString(body);

                // 塞进主线程队列，这里只捕获数据, 不执行UI逻辑
                int capturedMsgId = msgId;
                string capturedJson = json;
                _inbox.Enqueue(() => DispatchMessage(capturedMsgId, capturedJson));
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[网络] 接收中断: {ex.Message}");
        }
        _connected = false;
    }

    #region 对外接口 

    public void Register(string username, string password)
    {
        if (!_connected) return;
        var dto = new RegisterRequestDto { username = username, password = password };
        _ = SendAsync(MsgRegister, JsonUtility.ToJson(dto));
    }

    public void Login(string username, string password)
    {
        if (!_connected) return;
        var dto = new LoginRequestDto { username = username, password = password };
        _ = SendAsync(MsgLogin, JsonUtility.ToJson(dto));
    }

    public void GetPlayerData()
    {
        if (!IsLoggedIn) return;
        var dto = new GetPlayerDataRequestDto { token = Token };
        _ = SendAsync(MsgGetPlayerData, JsonUtility.ToJson(dto));
    }
    public void SavePlayerData(int coin, string inventoryJson, string roleDataJson)
    {
        if (!IsLoggedIn) return;
        var dto = new SavePlayerDataRequestDto { token = Token, coin = coin, inventoryJson = inventoryJson, roleDataJson = roleDataJson };
        _ = SendAsync(MsgSavePlayerData, JsonUtility.ToJson(dto));
    }
    #endregion

    #region 消息分发,只会在主线程被调用

    private void DispatchMessage(int msgId, string json)
    {
        switch (msgId)
        {
            case MsgLogin:
                HandleLoginResponse(json);
                break;
            case MsgRegister:
                HandleRegisterResponse(json);
                break;
            case MsgGetPlayerData:
                HandlePlayerDataResponse(json);
                break;
            case MsgSavePlayerData:
                break;

        }
    }

    private void HandleLoginResponse(string json)
    {
        var resp = JsonUtility.FromJson<LoginResponseDto>(json);
        bool ok = resp.code == 0;
        if (ok)
        {
            Token = resp.token;
            LoggedInUser = resp.username;
        }
        OnLoginResult?.Invoke(new LoginResult
        {
            success = ok,
            msg = resp.msg,
            username = resp.username,
            token = resp.token
        });
    }

    private void HandleRegisterResponse(string json)
    {
        var resp = JsonUtility.FromJson<RegisterResponseDto>(json);
        OnRegisterResult?.Invoke(new RegisterResult { success = resp.code == 0, msg = resp.msg });
    }

    private void HandlePlayerDataResponse(string json)
    {
        var resp = JsonUtility.FromJson<PlayerDataResponseDto>(json);
        OnPlayerDataResult?.Invoke(new PlayerDataResult
        {
            success = resp.code == 0,
            msg = resp.msg,
            coin = resp.coin,
            inventoryJson = resp.inventoryJson,
            roleDataJson = resp.roleDataJson   //把角色数据传给上层
        });
    }
    #endregion

    #region 底层收发 

    private async Task SendAsync(int msgId, string json)
    {
        if (_stream == null) return;
        try
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] len = BitConverter.GetBytes(body.Length);
            byte[] id = BitConverter.GetBytes(msgId);
            await _stream.WriteAsync(len, 0, 4);
            await _stream.WriteAsync(id, 0, 4);
            await _stream.WriteAsync(body, 0, body.Length);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[网络] 发送失败: {ex.Message}");
        }
    }

    private async Task<bool> ReadExactlyAsync(byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = await _stream.ReadAsync(buffer, total, count - total);
            if (n == 0) return false;
            total += n;
        }
        return true;
    }
    #endregion
}