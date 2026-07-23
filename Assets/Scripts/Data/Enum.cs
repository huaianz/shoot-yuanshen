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
    Food
}
