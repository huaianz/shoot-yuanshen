using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Affiliation
{
    蒙德, 璃月, 稻妻, 须弥, 枫丹, 纳塔, 至冬, 坎瑞亚, 旅行者, 无
}

public enum WeaponType
{
    AK47,
    M4A1,
    SCAR_L,
    M249,
    UMP45冲锋枪,
    格洛克手枪,
    九二式手枪,
    M1911系列手枪,
    USP系列手枪,
    RPG反坦克火箭筒,
    雷明顿870霰弹枪,
    Vector冲锋枪,
    阿格拉姆冲锋枪,
    UZI冲锋枪,
    P90冲锋枪,
    MP5冲锋枪,
    M24狙击步枪,
    九八K狙击枪,
    半自动狙击步枪,
}

/// <summary>
/// 背包当前模式
/// </summary>
public enum PackageMode
{
    normal,
    delete,
    sort,
}

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Weapon,
    Food,
    Material
}


public enum ActionType
{
    None,
    GiveItem,
    RemoveItem,
    OpenShop,
    TriggerEvent,//触发自定义事件
    CompleteQuest//完成任务
}

public enum NPCType
{
    Dialogue,
    Shop,
    Quest
}

/// <summary>
/// 行为树节点执行结果
/// </summary>
public enum NodeState
{
    Success,
    Failure,
    Running
}

/// <summary>
/// 敌人行为阶段
/// </summary>
public enum EnemyPhase
{
    Patrol,   // 巡逻/闲置
    Alert,    // 警戒
    Combat,   // 战斗
    Hit,      // 受击硬直
    Dead      // 死亡
}

/// <summary>
/// 掉落物品类型
/// </summary>
public enum PickupType
{
    Coin,   // 金币
    Weapon, // 武器 
    Food,   // 食物 
    Material // 素材
}

public enum EnemyState
{
    Idle,
    Move,
    Attack,
    Dead
}

/// <summary>
/// 委托类型
/// </summary>
public enum QuestType
{
    Kill,    // 消灭指定类型的敌人
    Collect  // 收集指定物品
}