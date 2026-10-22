using System.Collections.Generic;
using UnityEngine;

/// <summary>P1 房间流程测试: F1 列表 / F2 创建 / F3 加入 / F4 离开</summary>
public class RoomFlowTester : MonoBehaviour
{
    private readonly List<RoomInfo> _lastRooms = new List<RoomInfo>();
    private string _currentRoomId;

    private void Start()
    {
        var gc = GameClient.Instance;

        gc.OnRoomListResult += r =>
        {
            _lastRooms.Clear();
            if (!r.success) { ToastUI.ShowMessage("获取房间失败: " + r.msg); return; }

            _lastRooms.AddRange(r.rooms);
            Debug.Log($"[房间] 共 {r.rooms.Count} 个:");
            foreach (var room in r.rooms)
                Debug.Log($"  roomId={room.roomId} {room.name} {room.players}/{room.maxPlayers} [{room.state}]");
            ToastUI.ShowMessage($"房间数: {r.rooms.Count}");
        };

        gc.OnCreateRoomResult += r =>
        {
            if (!r.success) { ToastUI.ShowMessage("创建失败: " + r.msg); return; }
            _currentRoomId = r.roomId;
            ToastUI.ShowMessage($"创建成功: 房间 {r.roomId}");
            Debug.Log($"[房间] 创建成功 roomId={r.roomId}, 战斗服 {r.battleIp}:{r.battlePort}");
            BattleClient.Instance.ConnectTo(r.battleIp, r.battlePort, r.roomId);
        };

        gc.OnJoinRoomResult += r =>
        {
            if (!r.success) { ToastUI.ShowMessage("加入失败: " + r.msg); return; }
            _currentRoomId = r.roomId;
            ToastUI.ShowMessage($"加入成功: 房间 {r.roomId}");
            Debug.Log($"[房间] 加入成功 roomId={r.roomId}, 战斗服 {r.battleIp}:{r.battlePort}");
            BattleClient.Instance.ConnectTo(r.battleIp, r.battlePort, r.roomId);
        };
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[房间] 请求房间列表");
            GameClient.Instance.RequestRoomList();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            string name = GameClient.Instance.LoggedInUser + " 的房间";
            Debug.Log($"[房间] 创建房间: {name}");
            GameClient.Instance.CreateRoom(name, 2);
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            // P1 简化: 加入列表里的第一个房间(先按 F1 刷新列表)
            if (_lastRooms.Count == 0) { ToastUI.ShowMessage("先按 F1 获取房间列表"); return; }
            string roomId = _lastRooms[0].roomId;
            Debug.Log($"[房间] 加入房间 {roomId}");
            GameClient.Instance.JoinRoom(roomId);
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (string.IsNullOrEmpty(_currentRoomId)) return;
            Debug.Log($"[房间] 离开房间 {_currentRoomId}");
            GameClient.Instance.LeaveRoom(_currentRoomId);
            BattleClient.Instance.Disconnect();
            _currentRoomId = null;
        }
    }
}