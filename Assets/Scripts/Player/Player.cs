using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// 游戏运行时的数据管理
/// </summary>
public class Player : SingleMonoBase<Player>
{
    [Header("玩家静态配置表")]
    public PlayerData_SO playerDataSO;
    public WeaponData_SO weaponDataSO;

    #region 角色动态数据
    [HideInInspector]
    public Character currentCharacter;
    [HideInInspector]
    public Weapon currentWeapon;
    [HideInInspector]
    public int currentLevel;
    [HideInInspector]
    public int currentHP;
    [HideInInspector]
    public int currentATK;
    [HideInInspector]
    public int currentDEF;
    #endregion

    private void Start()
    {
        currentCharacter = playerDataSO.GetCharacterByID(1001);
        currentWeapon = weaponDataSO.GetWeaponByID(currentCharacter.weaponID);
    }

    /// <summary>
    /// 更新当前武器数据
    /// </summary>
    /// <param name="id"></param>
    public void UpdateWeapon(int id)
    {
        currentCharacter.weaponID = 2000 + id;
        //未更新配置文件，在存档的时候更新
        currentWeapon = weaponDataSO.GetWeaponByID(currentCharacter.weaponID);
    }
    /// <summary>
    /// 根据ID获取当前角色数据
    /// </summary>
    public void LoadDataDataFromSO(int id)
    {
        currentCharacter = playerDataSO.GetCharacterByID(1000 + id);
    }


}
