using UnityEngine;

/// <summary>
/// 当前所在的房间
/// </summary>
public static class RoomSession
{
    public static string CurrentRoomId { get; private set; }
    public static bool InRoom => !string.IsNullOrEmpty(CurrentRoomId);

    public static void Enter(RoomResult result)
    {
        if (result == null || !result.success) return;

        // 已经在别的房间: 先断开, 否则 ConnectTo 会因为 IsConnected 直接返回
        if (BattleClient.Instance.IsConnected)
            BattleClient.Instance.Disconnect();

        CurrentRoomId = result.roomId;
        BattleClient.Instance.ConnectTo(result.battleIp, result.battlePort, result.roomId);

        Debug.Log($"[RoomSession] 进入房间 {result.roomId}, 战斗服 {result.battleIp}:{result.battlePort}");
    }

    public static void Leave()
    {
        if (!InRoom) return;

        GameClient.Instance.LeaveRoom(CurrentRoomId);
        BattleClient.Instance.Disconnect();
        Debug.Log($"[RoomSession] 离开房间 {CurrentRoomId}");
        CurrentRoomId = null;
    }
}