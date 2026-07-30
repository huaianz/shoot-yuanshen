using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;

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

    //记录当前打开的面板的列表
    private List<BaseUIPanel> openedPanels = new List<BaseUIPanel>();
    private HashSet<Type> panelsRequireMouse = new HashSet<Type>
    {
        typeof(PackagePanel),
        //TODO:角色界面未实现鼠标显示
        //typeof(CharacterPanel)  // 如果还没有这个类，先注释掉
    };


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

    #region 鼠标显示
    public void OpenPanel<T>(T panel) where T : BaseUIPanel
    {
        //实例化并打开面板
        openedPanels.Add(panel);
        UpdateMouseVisibility();
    }
    // 关闭面板时
    public void ClosePanel(BaseUIPanel panel)
    {
        openedPanels.Remove(panel);
        UpdateMouseVisibility();
    }
    // 更新鼠标可见性
    private void UpdateMouseVisibility()
    {
        // 检查当前是否有任意一个“需要鼠标”的面板处于打开状态
        bool shouldShowMouse = openedPanels.Any(p => panelsRequireMouse.Contains(p.GetType()));

        Cursor.visible = shouldShowMouse;
        Cursor.lockState = shouldShowMouse ? CursorLockMode.None : CursorLockMode.Locked;
    }
    #endregion

}
