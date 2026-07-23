using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 实现背包面板功能
/// </summary>
public class PackagePanel : BaseUIPanel
{
    #region 缓存UI元素组件
    private Transform UIMenu;
    private Transform UIMenuWeapon;
    private Transform UIMenuFood;
    private Transform UITabName;
    private Transform UICloseBtn;
    private Transform UICenter;
    private Transform UIScrollView;
    private Transform UIDetailPanel;
    private Transform UILeftBtn;
    private Transform UIRightBtn;
    private Transform UIDeletePanel;
    private Transform UIDeleteBackBtn;
    private Transform UIDeleteInfoText;
    private Transform UIDeleteConfirmBtn;
    private Transform UIBottomMenus;
    private Transform UIDeleteBtn;
    private Transform UIDetailBtn;
    #endregion

    //物品格子预制体
    public GameObject PackageItemPrefab;

    //当前背包模式
    public PackageMode currentMode = PackageMode.normal;
    //存储待删除物品的ID
    public List<string> deleteChooseUid;

    private string _chooseUid;

    public string chooseUID
    {
        get
        {
            return _chooseUid;
        }
        set
        {
            _chooseUid = value;
            RefreshDetail();
        }
    }


    override protected void Awake()
    {
        //初始化UI
        InitUI();
    }

    private void Start()
    {
        //刷新UI
        //RefreshUI();
    }

    private void InitUI()
    {
        //InitUIName();
        //InitClick();
    }

    /// <summary>
    /// 添加选中项
    /// </summary>
    /// <param name="uid">物品的uid</param>
    public void AddChooseDeleteUid(string uid)
    {
        this.deleteChooseUid ??= new List<string>();
        if (!this.deleteChooseUid.Contains(uid))
        {
            this.deleteChooseUid.Add(uid);
        }
        else
        {
            this.deleteChooseUid.Remove(uid);
        }
        RefreshDeletePanel();
    }

    /// <summary>
    /// 刷新删除面板状态
    /// </summary>
    private void RefreshDeletePanel()
    {
        RectTransform scrollContent = UIScrollView.GetComponent<ScrollRect>().content;
        foreach (Transform cell in scrollContent)
        {
            PackageCell packageCell = cell.GetComponent<PackageCell>();
            //packageCell.RefreshDeleteState();
        }
    }

    /// <summary>
    /// 刷新详细面板
    /// </summary>
    private void RefreshDetail()
    {
        // PackageLocalItem localItem =
    }
}
