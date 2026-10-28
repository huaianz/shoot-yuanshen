using UnityEngine;

/// <summary>网络调试面板: F9 开关, 4Hz 刷新(避免每帧拼字符串产生 GC)</summary>
public class NetworkDebugHUD : MonoBehaviour
{
    public bool visible = true;
    public KeyCode toggleKey = KeyCode.F9;

    private GUIStyle _style;
    private string _text = "";
    private float _refreshTimer;
    private float _fpsTimer;
    private int _frames;
    private float _fps;

    private void Update()
    {
        _fpsTimer += Time.unscaledDeltaTime;
        _frames++;
        if (_fpsTimer >= 1f)
        {
            _fps = _frames / _fpsTimer;
            _fpsTimer = 0f;
            _frames = 0;
        }

        if (Input.GetKeyDown(toggleKey)) visible = !visible;

        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer >= 0.25f)
        {
            _refreshTimer = 0f;
            RebuildText();
        }
    }

    private void RebuildText()
    {
        var bc = BattleClient.Instance;
        if (bc == null) { _text = "BattleClient 未就绪"; return; }

        int remoteCount = 0;
        if (bc.LatestSnapshot != null)
        {
            foreach (var p in bc.LatestSnapshot.players)
                if (p.id != bc.MyPlayerId) remoteCount++;
        }

        _text =
            $"FPS: {_fps:F0}\n" +
            $"战斗服: {(bc.IsConnected ? "已连接" : "未连接")}   房间: {bc.RoomId}\n" +
            $"我的ID: {bc.MyPlayerId}   服务器tick: {bc.LatestServerTick}\n" +
            $"Ping: {bc.PingMs} ms\n" +
            $"上行: {bc.UpKBps:F2} KB/s   下行: {bc.DownKBps:F2} KB/s\n" +
            $"同房间玩家: {remoteCount + 1} 人\n" +
            $"快照缓冲: {bc.Buffer.Count} 帧   插值延迟: {bc.Buffer.InterpolationDelay * 1000f:F0} ms\n" +
            $"开火: {bc.ShotsSent}   命中: {bc.HitsConfirmed}   命中率: {(bc.ShotsSent > 0 ? bc.HitsConfirmed * 100f / bc.ShotsSent : 0f):F0}%\n" +
            $"我的服务器HP: {bc.MyServerHp}" +
            $"局内金币: {bc.MatchCoins}";


    }

    private void OnGUI()
    {
        if (!visible) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            _style.normal.textColor = Color.green;
        }
        GUI.Label(new Rect(12, 12, 460, 180), _text, _style);
    }
}
