using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.InteropServices;

public class AttibuteUI : MonoBehaviour
{
    [Header("属性界面UI")]
    #region 属性界面UI
    public TextMeshProUGUI CharacterName;
    public TextMeshProUGUI CharacterLevel;
    public TextMeshProUGUI Hp;
    public TextMeshProUGUI Atk;
    public TextMeshProUGUI Def;
    public TextMeshProUGUI description;
    #endregion

    private int _currentRoleID = -1;

    /// <summary>
    /// 刷新属性显示
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
            ClearDisplay();
            return;
        }

        _currentRoleID = roleID;
        var roleData = GameManager.INSTANCE.GetRoleData(roleID);
        if (roleData == null)
        {
            ClearDisplay();
            return;
        }

        //确保属性已计算
        if (roleData.isDirty)
        {
            GameManager.INSTANCE.RefreshRoleStats(roleID);
        }

        var baseData = roleData.baseData;
        if (CharacterName != null)
        {
            CharacterName.text = baseData.characterName;
        }
        if (CharacterLevel != null)
        {
            CharacterLevel.text = $"等级 {roleData.roleLevel}/90";
        }
        if (Hp != null)
        {
            Hp.text = $"{roleData.currentHealth:F0}/{roleData.finalMaxHealth:F0}";
        }
        if (Atk != null)
        {
            Atk.text = roleData.finalAttack.ToString("F0");
        }
        if (Def != null)
        {
            Def.text = roleData.finalDefense.ToString("F0");
        }

        if (description != null)
        {
            description.text = baseData.description;
        }
    }

    /// <summary>
    /// 清空显示
    /// </summary>
    private void ClearDisplay()
    {
        if (CharacterName != null)
        {
            CharacterName.text = "未知";
        }
        if (CharacterLevel != null)
        {
            CharacterLevel.text = "等级1/90";
        }
        if (Hp != null)
        {
            Hp.text = "0/0";
        }
        if (Atk != null)
        {
            Atk.text = "0";
        }
        if (Def != null)
        {
            Def.text = "0";
        }
        if (description != null)
        {
            description.text = "";
        }
    }
}
