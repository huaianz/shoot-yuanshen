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
    [Header("角色详细信息")]
    public string birthday;
    public string address;
    public string constellation;
    public string information;
}

/// <summary>
/// 武器数据
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

}


