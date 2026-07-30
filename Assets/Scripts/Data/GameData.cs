using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


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
    public int healAmount;//回复生命值
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




