using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 实现背包面板功能
/// </summary>
public class PackagePanel : BaseUIPanel
{
    [Header("顶部菜单")]
    private Button menuWeaponBtn;
    private Button menuFoodBtn;
    private TextMeshProUGUI tabNameText;
    private Button closeBtn;
    [Header("武器区")]
    private Transform centerWeapon;
    private ScrollRect scrollRectWeapon;
    private RectTransform scrollContentWeapon;
    private Transform detailPanelWeapon;
    [Header("食物区")]
    private Transform centerFood;
    private ScrollRect scrollRectFood;
    private RectTransform scrollContentFood;
    private Transform detailPanelFood;
    [Header("底部")]
    private Transform UIDeletePanel;
    private Button deleteBackBtn;
    private TextMeshProUGUI deleteInfoText;
    private Button deleteConfirmBtn;
    private Transform UIBottomMenus;
    private Button deleteBtn;
    private Button detailBtn;

    #region 对象池
    public GameObject PackageUIItemPrefab;
    #region 武器对象池
    //存放已实例化的格子
    private List<PackageCell> cellPoolWeapon = new List<PackageCell>();
    //当前正在显示的格子
    private List<PackageCell> activeCellsWeapon = new List<PackageCell>();
    #endregion
    #region 食物对象池
    private List<PackageCell> cellPoolFood = new List<PackageCell>();
    private List<PackageCell> activeCellsFood = new List<PackageCell>();
    #endregion
    #endregion

    #region 数据
    public PackageMode currentMode = PackageMode.normal;
    //待删除的UID列表
    public List<string> deleteChooseUid = new List<string>();

    //当前选中的标签页类型（武器/食物）
    private int _currentTabType = GameManager.GameConst.PackageTypeWeapon;//默认武器
    public int CurrentTabType => _currentTabType;

    private string _chooseUid;
    public string chooseUID
    {
        get => _chooseUid;
        set
        {
            //避免重复刷新
            if (_chooseUid == value)
            {
                return;
            }
            _chooseUid = value;
            RefreshDetail();
        }
    }
    #endregion

    protected override void Awake()
    {
        base.Awake();
        InitUI();
        //刚开始隐藏删除面板
        if (UIDeletePanel != null)
        {
            UIDeletePanel.gameObject.SetActive(false);
        }
        //默认显示武器
        SwitchTab(GameManager.GameConst.PackageTypeWeapon);
    }

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// 初始化UI组件
    /// </summary>
    private void InitUI()
    {
        //查找并缓存组件
        var menuWeaponTrans = transform.Find("TopCenter/Menus/Weapon");
        if (menuWeaponTrans != null)
        {
            menuWeaponBtn = menuWeaponTrans.GetComponent<Button>();
        }
        var menuFoodTrans = transform.Find("TopCenter/Menus/Food");
        if (menuFoodTrans != null)
        {
            menuWeaponBtn = menuFoodTrans.GetComponent<Button>();
        }
        var tabNameTrans = transform.Find("LeftTop/TabName");
        if (tabNameTrans != null)
        {
            tabNameText = tabNameTrans.GetComponent<TextMeshProUGUI>();
        }
        var closeTrans = transform.Find("RightTop/Close");
        if (closeTrans != null)
        {
            closeBtn = closeTrans.GetComponent<Button>();
        }
        //武器区
        centerWeapon = transform.Find("Center-Weapon");
        if (centerWeapon != null)
        {
            var scrollView = centerWeapon.Find("Scroll View");
            if (scrollView != null)
            {
                scrollRectWeapon = scrollView.GetComponent<ScrollRect>();
                scrollContentWeapon = scrollRectWeapon?.content;
            }
            detailPanelWeapon = centerWeapon.Find("DetailPanel-Weapon");
        }
        //食物区
        centerFood = transform.Find("Center-Food");
        if (centerFood != null)
        {
            var scrollView = centerFood.Find("Scroll View");
            if (scrollView != null)
            {
                scrollRectFood = scrollView.GetComponent<ScrollRect>();
                scrollContentFood = scrollRectFood?.content;
            }
            detailPanelFood = centerFood.Find("DetailPanel-Food");
        }
        //底部
        UIDeletePanel = transform.Find("Bottom/DeletePanel");
        var deleteBackTrans = transform.Find("Bottom/DeletePanel/Back");
        if (deleteBackTrans != null)
        {
            deleteBackBtn = deleteBackTrans.GetComponent<Button>();
        }
        var infoTrans = transform.Find("Bottom/DeletePanel/InfoText");
        if (infoTrans != null)
        {
            deleteInfoText = infoTrans.GetComponent<TextMeshProUGUI>();
        }
        var confirmTrans = transform.Find("Bottom/DeletePanel/ConfirmBtn");
        if (confirmTrans != null)
        {
            deleteConfirmBtn = confirmTrans.GetComponent<Button>();
        }
        UIBottomMenus = transform.Find("Bottom/BottomMenus");
        var deleteBtnTrans = transform.Find("Bottom/BottomMenus/DeleteBtn");
        if (deleteBtnTrans != null)
        {
            deleteBtn = deleteBtnTrans.GetComponent<Button>();
        }
        var detailBtnTrans = transform.Find("Bottom/BottomMenus/DetailBtn");
        if (detailBtnTrans != null)
        {
            detailBtn = detailBtnTrans.GetComponent<Button>();
        }

        #region 绑定点击事件
        if (menuWeaponBtn != null)
        {
            menuWeaponBtn.onClick.AddListener(() => OnClickTab(GameManager.GameConst.PackageTypeWeapon));
        }
        if (menuFoodBtn != null)
        {
            menuFoodBtn.onClick.AddListener(() => OnClickTab(GameManager.GameConst.PackageTypeFood));
        }
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(OnClickClose);
        }
        if (deleteBackBtn != null)
        {
            deleteBackBtn.onClick.AddListener(OnDeleteBack);
        }
        if (deleteConfirmBtn != null)
        {
            deleteConfirmBtn.onClick.AddListener(OnDeleteConfirm);
        }
        if (deleteBtn != null)
        {
            deleteBtn.onClick.AddListener(OnDelete);
        }
        if (detailBtn != null)
        {
            detailBtn.onClick.AddListener(OnDetail);
        }
        #endregion

        //初始隐藏删除界面
        if (UIDeletePanel != null)
        {
            UIDeletePanel.gameObject.SetActive(false);
        }
    }

    #region 切换标签页
    /// <summary>
    /// 切换标签页
    /// </summary>
    /// <param name="tabType"></param>
    private void OnClickTab(int tabType)
    {
        if (_currentTabType == tabType)
        {
            return;
        }
        SwitchTab(tabType);
        //切换时刷新列表
        RefreshScroll(tabType);
        //清空选中
        chooseUID = null;
    }

    private void SwitchTab(int tabType)
    {
        bool isWeapon = (tabType == GameManager.GameConst.PackageTypeWeapon);
        //显示和隐藏对应的Center
        if (centerWeapon != null)
        {
            centerWeapon.gameObject.SetActive(isWeapon);
        }
        if (centerFood != null)
        {
            centerFood.gameObject.SetActive(!isWeapon);
        }
        //更新Tab名称
        if (tabNameText != null)
        {
            tabNameText.text = isWeapon ? "武器" : "食物";
        }
    }
    #endregion

    #region 刷新UI
    private void RefreshUI()
    {
        //刷新当前标签页
        RefreshScroll(_currentTabType);
        if (!string.IsNullOrEmpty(chooseUID))
        {
            RefreshDetail();
        }
    }

    /// <summary>
    /// 刷新指定类型的滚动列表
    /// </summary>
    /// <param name="tabType"></param>
    private void RefreshScroll(int tabType)
    {
        bool isWeapon = (tabType == GameManager.GameConst.PackageTypeWeapon);
        RectTransform content = isWeapon ? scrollContentWeapon : scrollContentFood;
        if (content == null || PackageUIItemPrefab == null)
        {
            return;
        }
        //获取对应类型数据
        List<ItemBase> dataList;
        if (isWeapon)
        {
            //获取武器数据
            dataList = GameManager.INSTANCE.GetWeaponItems();
        }
        else
        {
            dataList = GameManager.INSTANCE.GetFoodItems();
        }

        //选择对应的池子
        List<PackageCell> pool = isWeapon ? cellPoolWeapon : cellPoolFood;
        List<PackageCell> activeList = isWeapon ? activeCellsWeapon : activeCellsFood;

        //确保池子里有足够的格子
        int needCount = dataList.Count;
        for (int i = pool.Count; i < needCount; i++)
        {
            GameObject go = Instantiate(PackageUIItemPrefab, content);
            PackageCell cell = go.GetComponent<PackageCell>();
            pool.Add(cell);
        }

        //激活需要的格子，隐藏多余的
        activeList.Clear();
        for (int i = 0; i < pool.Count; i++)
        {
            bool active = i < needCount;
            pool[i].gameObject.SetActive(active);
            if (active)
            {
                pool[i].Refresh(dataList[i], this);
                activeList.Add(pool[i]);
            }
        }

        //重置滚动位置
        ScrollRect scrollRect = isWeapon ? scrollRectWeapon : scrollRectFood;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    //刷新详细页面
    private void RefreshDetail()
    {
        if (string.IsNullOrEmpty(chooseUID))
        {
            return;
        }

        ItemBase item = InventoryManager.INSTANCE.GetItem(chooseUID);
        if (item == null)
        {
            return;
        }
        bool isWeapon = (_currentTabType == GameManager.GameConst.PackageTypeWeapon);
        Transform detailPanel = isWeapon ? detailPanelWeapon : detailPanelFood;
        if (detailPanel == null)
        {
            return;
        }
        // //获取对应详细组件
        // var detail = detailPanel.GetComponent<PackageDetail>();
        // if (detail != null)
        // {
        //     detail.Refresh(item, this);
        // }
        //TODO:详细页面刷新
    }

    /// <summary>
    /// 删除状态刷新
    /// </summary>
    public void RefreshDeletePanel()
    {
        //值刷新当前激活的标签页
        List<PackageCell> activeList = (_currentTabType == GameManager.GameConst.PackageTypeWeapon) ? activeCellsWeapon : activeCellsFood;
        foreach (var cell in activeList)
        {
            if (cell != null)
            {
                cell.RefreshDeleteState();
            }
        }

        if (deleteInfoText != null)
        {
            deleteInfoText.text = $"已选择 {deleteChooseUid.Count}/100";
        }
    }

    public void AddChooseDeleteUid(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return;
        }
        if (deleteChooseUid.Contains(uid))
        {
            deleteChooseUid.Remove(uid);
        }
        else
        {
            deleteChooseUid.Add(uid);
        }
    }

    #endregion

    #region 按钮事件
    private void OnClickClose()
    {
        ClosePanel();
        //打开主页面
        // UIManager,INSTANCE.OpenPanel(UIConst.MainPANEL);
    }

    private void OnDeleteBack()
    {
        currentMode = PackageMode.normal;
        if (UIDeletePanel != null)
        {
            UIDeletePanel.gameObject.SetActive(false);
        }
        deleteChooseUid.Clear();
        RefreshDeletePanel();
    }
    private void OnDeleteConfirm()
    {
        if (deleteChooseUid == null || deleteChooseUid.Count == 0)
            return;

        GameManager.INSTANCE.DeletePackageItems(deleteChooseUid);
        deleteChooseUid.Clear();

        // 刷新当前标签页
        RefreshScroll(_currentTabType);
        currentMode = PackageMode.normal;
        if (UIDeletePanel != null) UIDeletePanel.gameObject.SetActive(false);
    }
    private void OnDelete()
    {
        currentMode = PackageMode.delete;
        if (UIDeletePanel != null) UIDeletePanel.gameObject.SetActive(true);
        deleteChooseUid.Clear();
        RefreshDeletePanel();
    }

    private void OnDetail()
    {
        // 显示详情（可扩展）
        print(">>>>> OnDetail");
    }
    #endregion
}
