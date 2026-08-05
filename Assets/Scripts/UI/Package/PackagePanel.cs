using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 实现背包面板功能
/// </summary>
public class PackagePanel : BaseUIPanel
{
    [Header("顶部菜单")]
    private Button menuWeaponBtn;
    private Image selectWeapon;
    private Button menuFoodBtn;
    private Image selectFood;
    private TextMeshProUGUI tabNameText;
    private Button closeBtn;
    //总容量显示

    private TextMeshProUGUI capacityText;
    [Header("武器区")]
    private Transform centerWeapon;
    private ScrollRect scrollRectWeapon;
    private RectTransform scrollContentWeapon;
    //详细信息
    private TextMeshProUGUI weaponNameText;
    private TextMeshProUGUI weaponDescText;
    private Transform weaponStars;
    private Image weaponIcon;
    [Header("食物区")]
    private Transform centerFood;
    private ScrollRect scrollRectFood;
    private RectTransform scrollContentFood;
    //详细信息
    private TextMeshProUGUI foodNameText;
    private TextMeshProUGUI foodDescText;
    private Transform foodStars;
    private Image foodIcon;
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
    //当前激活的格子列表
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
        //TODO:暂时调用打开背包鼠标显示
        UIManager.EnterUIBlock();
    }

    /// <summary>
    /// 初始化UI组件
    /// </summary>
    private void InitUI()
    {
        #region 查找并缓存组件
        var menuWeaponTrans = transform.Find("TopCenter/Menus/Weapon");
        if (menuWeaponTrans != null)
        {
            menuWeaponBtn = menuWeaponTrans.GetComponent<Button>();
            var select = menuWeaponTrans.Find("Select");
            selectWeapon = select.GetComponent<Image>();
        }
        var menuFoodTrans = transform.Find("TopCenter/Menus/Food");
        if (menuFoodTrans != null)
        {
            menuFoodBtn = menuFoodTrans.GetComponent<Button>();
            var select = menuFoodTrans.Find("Select");
            selectFood = select.GetComponent<Image>();
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
            var Center = centerWeapon.Find("DetailPanel-Weapon/Center");
            var Bottom = centerWeapon.Find("DetailPanel-Weapon/Bottom");
            //星级
            weaponStars = Center.Find("Stars");
            //武器名称和图片
            var nameWeapon = Center.Find("name");
            weaponNameText = nameWeapon.GetComponent<TextMeshProUGUI>();
            var iconWeapon = Center.Find("icon");
            weaponIcon = iconWeapon.GetComponent<Image>();
            //描述
            var description = Bottom.Find("description");
            weaponDescText = description.GetComponent<TextMeshProUGUI>();
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
            var Center = centerFood.Find("DetailPanel-Food/Center");
            var Bottom = centerFood.Find("DetailPanel-Food/Bottom");
            //星级
            foodStars = Center.Find("Stars");
            //武器名称和图片
            var nameWeapon = Center.Find("name");
            foodNameText = nameWeapon.GetComponent<TextMeshProUGUI>();
            var iconWeapon = Center.Find("icon");
            foodIcon = iconWeapon.GetComponent<Image>();
            //描述
            var description = Bottom.Find("description");
            foodDescText = description.GetComponent<TextMeshProUGUI>();
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
        var confirmTrans = transform.Find("Bottom/DeletePanel/ConfilmBtn");
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
        //总容量

        #endregion
        var capacityTrans = transform.Find("RightTop/NumText");  // 请根据实际路径修改
        if (capacityTrans != null)
        {
            capacityText = capacityTrans.GetComponent<TextMeshProUGUI>();
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
        _currentTabType = tabType;
    }

    private void SwitchTab(int tabType)
    {
        bool isWeapon = (tabType == GameManager.GameConst.PackageTypeWeapon);
        //显示和隐藏对应的Center
        if (centerWeapon != null)
        {
            Debug.Log("武器界面不为空");
            centerWeapon.gameObject.SetActive(isWeapon);
        }
        if (centerFood != null)
        {
            Debug.Log("食物界面不为空");
            centerFood.gameObject.SetActive(!isWeapon);
        }
        //更新Tab名称
        if (tabNameText != null)
        {
            tabNameText.text = isWeapon ? "武器" : "食物";
        }
        //更新Tab图标
        selectWeapon.gameObject.SetActive(isWeapon);
        selectFood.gameObject.SetActive(!isWeapon);
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
        UpdateCapacityDisplay();
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

    /// <summary>
    /// 刷新详细页面
    /// </summary>
    private void RefreshDetail()
    {
        if (string.IsNullOrEmpty(chooseUID))
        {
            ClearDetailPanel();
            return;
        }

        ItemBase item = InventoryManager.INSTANCE.GetItem(chooseUID);
        if (item == null)
        {
            ClearDetailPanel();
            return;
        }
        bool isWeapon = (_currentTabType == GameManager.GameConst.PackageTypeWeapon);

        if (isWeapon)
        {
            var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(item.itemID);
            if (weapon == null)
            {
                return;
            }
            if (weaponNameText != null)
            {
                weaponNameText.text = weapon.weaponName;
            }
            if (weaponDescText != null)
            {
                weaponDescText.text = weapon.weaponDescription;
            }
            Sprite icon = InventoryManager.INSTANCE.GetIcon(item.itemID);
            if (weaponIcon != null)
                weaponIcon.sprite = icon;
            RefreshDetailStars(weaponStars, weapon.Stars);
        }
        else
        {
            var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(item.itemID);
            if (food == null)
            {
                return;
            }
            if (foodNameText != null)
            {
                foodNameText.text = food.foodName;
            }
            if (foodDescText != null)
            {
                foodDescText.text = food.description;
            }
            Sprite icon = InventoryManager.INSTANCE.GetIcon(item.itemID);
            if (foodIcon != null)
            {
                foodIcon.sprite = icon;
            }
            //TODO:食物没有星星
        }
    }

    /// <summary>
    /// 刷新详细面板的星级显示
    /// </summary>
    /// <param name="starContainer">星星的父物体</param>
    /// <param name="starCount">星级</param>
    private void RefreshDetailStars(Transform starContainer, int starCount)
    {
        if (starContainer == null) return;
        for (int i = 0; i < starContainer.childCount; i++)
        {
            var star = starContainer.GetChild(i);
            if (star != null)
            {
                star.gameObject.SetActive(i < starCount);
            }
        }
    }

    /// <summary>
    /// 清空详细面板(当没有选中物品时)
    /// </summary>
    private void ClearDetailPanel()
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = "";
        }
        if (weaponDescText != null)
        {
            weaponDescText.text = "";
        }
        if (weaponIcon != null)
        {
            weaponIcon.sprite = null;
        }
        if (foodNameText != null)
        {
            foodNameText.text = "";
        }
        if (foodDescText != null)
        {
            foodDescText.text = "";
        }
        if (foodIcon != null)
        {
            foodIcon.sprite = null;
        }
        // 清空星级（可隐藏所有星星）
        if (weaponStars != null)
        {
            foreach (Transform star in weaponStars)
                star.gameObject.SetActive(false);
        }
        if (foodStars != null)
        {
            foreach (Transform star in foodStars)
            {
                star.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 删除状态刷新
    /// </summary>
    public void RefreshDeletePanel()
    {
        //只刷新当前激活的标签页
        List<PackageCell> activeList = (_currentTabType == GameManager.GameConst.PackageTypeWeapon) ? activeCellsWeapon : activeCellsFood;
        foreach (var cell in activeList)
        {
            if (cell != null)
            {
                cell.RefreshDeleteState();
            }
        }
        // 更新提示文本（动态总数）
        int totalCount = (_currentTabType == GameManager.GameConst.PackageTypeWeapon)
        ? InventoryManager.INSTANCE._weaponIds.Count
        : InventoryManager.INSTANCE._foodIds.Count;
        if (deleteInfoText != null)
            deleteInfoText.text = $"已选择 {deleteChooseUid.Count}/{totalCount}";

    }

    /// <summary>
    /// 添加删除物品id
    /// </summary>
    /// <param name="uid"></param>
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
        //刷新格子删除状态
        RefreshDeletePanel();
    }

    /// <summary>
    /// 更新容量显示
    /// </summary>
    private void UpdateCapacityDisplay()
    {
        if (capacityText == null) return;

        //获取当前标签页的物品总数
        int currentCount = (InventoryManager.INSTANCE._weaponIds.Count + InventoryManager.INSTANCE._foodIds.Count);

        //最大容量520
        const int maxCapacity = 520;
        capacityText.text = $"{currentCount}/{maxCapacity}";
    }

    #endregion

    #region 根据 UID 在当前激活的格子中查找对应的PackageCell
    public PackageCell FindCellByUID(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return null;

        List<PackageCell> activeList = (_currentTabType == GameManager.GameConst.PackageTypeWeapon)
            ? activeCellsWeapon
            : activeCellsFood;

        foreach (var cell in activeList)
        {
            if (cell != null && cell.itemData != null && cell.itemData.instanceID == uid)
                return cell;
        }
        return null;
    }
    #endregion

    #region 按钮事件
    private void OnClickClose()
    {
        ClosePanel();
        UIManager.ExitUIBlock();
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

    /// <summary>
    /// 删除确认
    /// </summary>
    private void OnDeleteConfirm()
    {
        if (deleteChooseUid == null || deleteChooseUid.Count == 0)
        {
            return;
        }
        //执行删除
        GameManager.INSTANCE.DeletePackageItems(deleteChooseUid);
        //清空删除列表
        deleteChooseUid.Clear();
        //退出删除模式
        currentMode = PackageMode.normal;
        if (UIDeletePanel != null)
        {
            UIDeletePanel.gameObject.SetActive(false);
        }
        //刷新背包列表
        RefreshScroll(_currentTabType);
        //清空选中状态
        chooseUID = null;
        UpdateCapacityDisplay();
    }
    #endregion

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }
}
