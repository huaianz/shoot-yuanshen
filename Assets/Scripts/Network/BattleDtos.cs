using System;
using System.Collections.Generic;

///<summary>
/// 加入请求
///</summary>
[Serializable]
public class JoinRequestDto
{
    public string playerId;     // 用户名
    public string token;
    public string roomId;       // 要加入的房间
    public float x, y, z;       // 客户端当前坐标
}

/// <summary>
///  输入指令
/// </summary>
[Serializable]
public class InputCommandDto
{
    public int tick;
    public float moveX, moveZ, yaw;
    public int buttons;        // 位标志: 1=冲刺 2=瞄准 4=开火 ...
    public long clientTimeMs;   // 客户端时间戳
    public float x, y, z;
    public int animId;          // 动作状态(0待机 1移动 2瞄准 3下蹲 4攀爬 5滑铲 6闪避 7悬空)
    public bool teleport;       // 本帧是传送落地(服务器跳过距离校验)
}

/// <summary>
/// 单个玩家的状态
/// </summary>
[Serializable]
public class PlayerStateDto
{
    public int id;
    public float x, y, z, yaw;
    public string anim = "Idle";
    public int animId;
    public int hp;
}

/// <summary>
/// 世界快照
/// </summary>
[Serializable]
public class SnapshotDto
{
    public int tick;    //逻辑帧，保证玩家之间的同步
    public List<PlayerStateDto> players = new List<PlayerStateDto>();
}

/// <summary>
/// 加入确认
/// </summary>
[Serializable]
public class JoinAcceptedDto
{
    public int playerId;
    public string roomId;
    public int tick;
    public List<PlayerStateDto> players = new List<PlayerStateDto>();
}

/// <summary>
/// 开火
/// </summary>
[Serializable]
public class FireEventDto
{
    public int playerId;
    public float ox, oy, oz;      // 开火起点
    public float dx, dy, dz;      // 方向(归一化)
    public long fireTimeMs;       // 客户端开火时间
    public int pingMs;            // 客户端 RTT(用于回溯)
}

/// <summary>
/// 伤害结算
/// </summary>
[Serializable]
public class DamageResultDto
{
    public int shooterId;//攻击者ID
    public int targetId;// 受伤者ID
    public int damage;
    public int targetHp;
    public bool isKill;
    public float hitX, hitY, hitZ;
}