using System;
using System.Collections.Generic;
/// <summary>
/// 协议 DTO
/// </summary>
[Serializable]
public class LoginRequestDto { public string username; public string password; }
[Serializable]
public class LoginResponseDto { public int code; public string msg; public string username; public string token; }

[Serializable]
public class RegisterRequestDto { public string username; public string password; }
[Serializable]
public class RegisterResponseDto { public int code; public string msg; public string username; public string token; }

[Serializable]
public class GetPlayerDataRequestDto { public string token; }
[Serializable]
public class PlayerDataResponseDto { public int code; public string msg; public string username; public int coin; public string inventoryJson; public string roleDataJson; public string questDataJson; }
[Serializable]
public class SavePlayerDataRequestDto { public string token; public int coin; public string inventoryJson; public string roleDataJson; public string questDataJson; }

public class RoomListResult { public bool success; public string msg; public List<RoomInfo> rooms = new(); }
public class RoomInfo { public string roomId; public string name; public int players; public int maxPlayers; public string state; }
public class RoomResult
{
    public bool success;
    public string msg;
    public string roomId;
    public string playerId;
    public string battleIp;
    public int battlePort;
    public List<string> players = new List<string>();
}
//房间请求/响应 DTO 
[Serializable] public class RoomListRequestDto { public string token; }

[Serializable]
public class RoomInfoDto
{
    public string roomId;
    public string name;
    public int players;
    public int maxPlayers;
    public string state;
}

[Serializable]
public class RoomListResponseDto
{
    public int code;
    public string msg;
    public RoomInfoDto[] rooms;
}

[Serializable]
public class CreateRoomRequestDto { public string token; public string roomName; public int maxPlayers; }

[Serializable]
public class CreateRoomResponseDto
{
    public int code;
    public string msg;
    public string roomId;
    public string playerId;
    public string battleIp;
    public int battlePort;
}

[Serializable]
public class JoinRoomRequestDto { public string token; public string roomId; }

[Serializable]
public class JoinRoomResponseDto
{
    public int code;
    public string msg;
    public string roomId;
    public string playerId;
    public string battleIp;
    public int battlePort;
    public string[] players;
}

[Serializable]
public class LeaveRoomRequestDto { public string token; public string roomId; }