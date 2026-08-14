using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;


/// <summary>
/// 单挑角色数据
/// </summary>
[Serializable]
public class Character
{
    [Header("角色基础信息")]
    public int characterID;
    public string characterName;
    public int characterLevel;
    public int characterExp;
    public int characterHP;
    public int characterATK;
    public int characterDEF;
    public string description;
    [Header("角色佩戴武器")]
    public int weaponID;
    [Header("角色头像路径")]
    public string avatarPath;
    [Header("角色背景视频路径")]
    public string videoPath;
    [Header("角色详细信息")]
    public string birthday;
    public string address;
    public string constellation;
    public string information;
}

/// <summary>
/// 武器类
/// </summary>
[Serializable]
public class Weapon
{
    public int weaponID;
    public string weaponName;
    [Header("武器基础信息")]
    public string weaponType;
    public int weaponATK;
    public int fireRate;
    public int BulletNum;
    public int Stars;
    [Header("武器详细信息")]
    public string weaponDescription;
    [Header("武器物品图片路径")]
    public string iconPath;

}

/// <summary>
/// 食物类
/// </summary>
[Serializable]
public class Food
{
    public int foodID;
    public string foodName;
    [Header("回血方式")]
    public FoodHealType healType = FoodHealType.Instant;
    [Header("回血量(立即回血=总血量; 间断回血=每次回血量)")]
    public int healAmount;
    [Header("间断回血总时长(秒)")]
    public float overTimeDuration = 0f;
    [Header("每次回血间隔(秒)")]
    public float tickInterval = 0f;
    public int maxStack = 99;
    public string description;
    public string iconPath;
}

/// <summary>
/// 运行时的实际武器数据
/// </summary>
[Serializable]
public class WeaponItem : ItemBase
{
    //目前没有额外的字段   
}

[Serializable]
public class FoodItem : ItemBase
{
    public int count = 1;
    //强制没有拥有者
    public FoodItem()
    {
        ownerID = -1;
    }
}

[System.Serializable]
public class RoleData
{
    public int roleID;
    public string roleName;
    public Sprite avatar;
    public float baseHealth = 100f;
    public float baseAttack = 10f;
    public float baseDefence = 5f;
    public float moveSpeed = 5f;
    public string birthday = "未知";
    public string address = "未知";
    public string constellation = "未知";
    public string information = "未知";
    public int level = 1;
}

public class RoleRuntimeData
{
    public int roleID;
    public Character baseData;
    public float currentHealth;
    public float currentArmor;
    public float currentStamina;
    // 缓存最终战斗属性
    public float finalAttack;
    public float finalDefense;
    public float finalMoveSpeed;
    public float finalMaxHealth;
    public float finalMaxArmor;

    public bool isDirty = true;
    public string equippedWeaponId;

    public RoleRuntimeData(Character character)
    {
        roleID = character.characterID;
        baseData = character;
        currentHealth = character.characterHP;
        currentArmor = character.characterDEF;
        currentStamina = 100f;
        isDirty = true;
    }
}

/// <summary>
/// 单个对话选项
/// </summary>
[System.Serializable]
public class DialogueOption
{
    [Header("选项显示")]
    public string optionText;
    [Header("选项行为")]
    public int nextDialogueID;
    public DialogueAction action;//选择后执行的动作
}

/// <summary>
/// 选择选项后触发的对话动作
/// </summary>
[System.Serializable]
public class DialogueAction
{
    public ActionType type;
    public int itemID;
    public int amount = 1;
    public string eventName;
}

[System.Serializable]
public class ShopItem
{
    public int itemID;
    public int price;
    public int stock = -1;//-1表示无限
    public int purchaseLimit = 0;//购买限制
    public string currentcyType = "Coin";
}

/// <summary>
/// 敌人属性数据
/// </summary>
[System.Serializable]
public class EnemyStats
{
    [Header("生命与攻击")]
    public int maxHealth = 100;
    public int attackDamage = 10;
    [Header("移动速度")]
    public float patrolSpeed = 1.5f;//巡逻速度
    public float chaseSpeed = 3f;//追击速度
    public float attackSpeed = 2f;//攻击时的移动速度
    public float retreatSpeed = 3f;//后退速度
}

/// <summary>
/// 一条掉落配置
/// </summary>
[System.Serializable]
public class LootDrop
{
    [Tooltip("掉落物类型")]
    public PickupType type = PickupType.Coin;
    [Tooltip("物品ID(武器/食物模板ID, 金币可不填)")]
    public int itemID;
    [Tooltip("数量(金币/食物数量)")]
    public int amount = 1;
    [Range(0f, 1f)]
    [Tooltip("掉落概率, 1=必定掉落")]
    public float chance = 1f;
    [Tooltip("掉落物预制体")]
    public GameObject dropPrefab;
}

/// <summary>
/// 素材类(史莱姆凝液/丘丘人面具/石头碎片等)
/// </summary>
[Serializable]
public class MaterialData
{
    public int materialID;
    public string materialName;
    public int maxStack = 99;
    public string description;
    public string iconPath;
}

/// <summary>
/// 背包里的素材物品(可堆叠, 无拥有者)
/// </summary>
[Serializable]
public class MaterialItem : ItemBase
{
    public int count = 1;
    public MaterialItem()
    {
        ownerID = -1;
    }
}

/// <summary>
/// 一波里的一群敌人
/// </summary>
[System.Serializable]
public class WaveEnemy
{
    [Tooltip("敌人预制体")]
    public GameObject enemyPrefab;
    [Tooltip("这一组刷几只")]
    public int count = 1;
}

/// <summary>
/// 一波的配置
/// </summary>
[System.Serializable]
public class WaveConfig
{
    [Tooltip("波次名字")]
    public string waveName = "第1波";
    [Tooltip("这一波有哪些敌人")]
    public List<WaveEnemy> enemies = new List<WaveEnemy>();
    [Tooltip("这一波清完后, 等几秒再出下一波")]
    public float nextWaveDelay = 3f;
}

/// <summary>
/// 云背包里的一个物品(只保存类型/ID/数量, 不带装备状态)
/// </summary>
[System.Serializable]
public class CloudItemData
{
    public string type;    // "Weapon" / "Food" / "Material"
    public int itemID;
    public int count;
}

/// <summary>
/// 云背包整体(序列化成 JSON 存到服务器)
/// </summary>
[System.Serializable]
public class CloudInventoryData
{
    public System.Collections.Generic.List<CloudItemData> items = new System.Collections.Generic.List<CloudItemData>();
}
