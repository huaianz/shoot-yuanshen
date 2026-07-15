using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPanelUI : MonoBehaviour
{
    [Header("角色面板")]
    public GameObject characterPanel;
    public GameObject AttibutePanel;
    public GameObject WeaponPanel;
    public GameObject DataPanel;

    [Header("角色面板按钮")]
    public Button ClosePanelBtn;
    public Button AttibuteBtn;
    public Button WeaponBtn;
    public Button DataBtn;
    [Header("伴随按钮点击的UI显示")]
    #region 伴随按钮点击的UI显示
    public GameObject AttibuteImage;
    public GameObject WeaponImage;
    public GameObject DataImage;
    #endregion

    #region 头像按钮资源
    public GameObject HeadObject;
    private Image HeadImage;
    #endregion
    private void Start()
    {
        #region 默认状态
        AttibutePanel.SetActive(true);
        WeaponPanel.SetActive(false);
        DataPanel.SetActive(false);
        AttibuteImage.SetActive(true);
        WeaponImage.SetActive(false);
        DataImage.SetActive(false);
        #endregion

        #region 按钮监听
        ClosePanelBtn.onClick.AddListener(() =>
        {
            characterPanel.SetActive(false);
        });
        //角色属性面板
        AttibuteBtn.onClick.AddListener(() =>
        {
            UIManager.INSTANCE.SwitchTabWithPanel(AttibutePanel,
                (AttibutePanel, AttibuteImage),
                (WeaponPanel, WeaponImage),
                (DataPanel, DataImage)
            );
        });

        WeaponBtn.onClick.AddListener(() =>
        {
            UIManager.INSTANCE.SwitchTabWithPanel(WeaponPanel,
                (AttibutePanel, AttibuteImage),
                (WeaponPanel, WeaponImage),
                (DataPanel, DataImage)
            );
        });

        DataBtn.onClick.AddListener(() =>
        {
            UIManager.INSTANCE.SwitchTabWithPanel(DataPanel,
                (AttibutePanel, AttibuteImage),
                (WeaponPanel, WeaponImage),
                (DataPanel, DataImage)
            );
        });
        #endregion

        #region 获取头像资源
        HeadObject = Resources.Load<GameObject>("Prefabs/Package/Left/back Image");
        #endregion
    }


}
