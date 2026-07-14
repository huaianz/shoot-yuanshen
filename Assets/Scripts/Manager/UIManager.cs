using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI管理器
/// </summary>
public class UIManager : SingleMonoBase<UIManager>
{
    public GameObject WorldSpaceCanvas;

    [Header("玩家UI")]
    public GameObject PlayerCanvas;
    [Header("角色界面")]
    public GameObject CharacterPanel;
    [Header("背包界面")]
    public GameObject PackagePanel;

    #region 界面的打开与关闭
    /// <summary>
    /// 打开界面
    /// </summary>
    /// <param name="panel"></param>
    public void ShowUIPanel(GameObject panel)
    {
        panel.SetActive(true);
    }

    /// <summary>
    /// 关闭界面
    /// </summary>
    /// <param name="panel"></param>
    public void CloseUIPanel(GameObject panel)
    {
        panel.SetActive(false);
    }

    /// <summary>
    /// 切换界面
    /// </summary>
    /// <param name="targetPanel">目标界面</param>
    /// <param name="panelsToClose">要隐藏的界面</param>
    public void SwitchPanel(GameObject targetPanel, params GameObject[] panelsToClose)
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }

        //遍历关闭其他所有指定的面板
        foreach (GameObject panel in panelsToClose)
        {
            if (panel != null && panel != targetPanel)
            {
                panel.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 切换界面（同时控制面板和箭头显隐）
    /// </summary>
    /// <param name="targetPanel">要激活的目标界面</param>
    /// <param name="tabPairs">多组 (面板, 箭头) 的配对，方法会自动将目标面板对应的箭头亮起，其他熄灭</param>
    public void SwitchTabWithPanel(GameObject targetPanel, params (GameObject panel, GameObject arrow)[] tabPairs)
    {
        //先遍历所有传入的配对
        foreach (var pair in tabPairs)
        {
            if (pair.panel == null) continue;
            //然后判断这一项是不是想要激活的界面
            bool isActive = (pair.panel == targetPanel);
            pair.panel.SetActive(isActive);
            if (pair.arrow != null)
                pair.arrow.SetActive(isActive);
        }
    }
    #endregion


}
