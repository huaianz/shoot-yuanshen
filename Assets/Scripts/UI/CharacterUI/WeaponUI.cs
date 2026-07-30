using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("佩戴武器UI")]
    public TextMeshProUGUI weaponName;
    public TextMeshProUGUI weaponType;
    public TextMeshProUGUI weaponAtk;
    public TextMeshProUGUI fireRate;
    public TextMeshProUGUI BulletNum;
    public TextMeshProUGUI weaponDesc;

    /// <summary>
    /// 刷新佩戴武器UI
    /// </summary>
    /// <param name="roleID"></param>
    public void RefreshUI(int roleID = -1)
    {
        if (roleID < 0)
        {
            roleID = GameManager.INSTANCE.GetActiveRoleID();
        }
        if (roleID < 0)
        {
            ClearWeaponDisplay();
            return;
        }

        //获取该角色装备的武器
        var weaponData = InventoryManager.INSTANCE.GetRoleWeaponData(roleID);
        if (weaponData == null)
        {
            ClearWeaponDisplay();
            return;
        }

        //从模版获取武器详细信息
        var item = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weaponData.itemID);
        if (item == null)
        {
            ClearWeaponDisplay();
            return;
        }
        if (weaponName != null)
        {
            weaponName.text = item.weaponName;
        }
        if (weaponType != null)
        {
            weaponType.text = item.weaponType;
        }
        if (weaponAtk != null)
        {
            weaponAtk.text = item.weaponATK.ToString();
        }
        if (fireRate != null)
        {
            fireRate.text = item.fireRate.ToString();
        }
        if (BulletNum != null)
        {
            BulletNum.text = item.BulletNum.ToString();
        }
        if (weaponDesc != null)
        {
            weaponDesc.text = item.weaponDescription;
        }
    }

    /// <summary>
    /// 清空武器显示
    /// </summary>
    private void ClearWeaponDisplay()
    {
        if (weaponName != null)
        {
            weaponName.text = "无武器";
        }
        if (weaponType != null)
        {
            weaponType.text = "";
        }
        if (weaponAtk != null)
        {
            weaponAtk.text = "0";
        }
        if (fireRate != null)
        {
            fireRate.text = "0";
        }
        if (BulletNum != null)
        {
            BulletNum.text = "0";
        }
        if (weaponDesc != null)
        {
            weaponDesc.text = "请装备武器";
        }
    }
}
