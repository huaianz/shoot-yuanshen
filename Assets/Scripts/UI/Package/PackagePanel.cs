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
    private Button menuMaterialBtn;
    private Image selectMaterial;
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
    [Header("素材区")]
    private Transform centerMaterial;
    private ScrollRect scrollRectMaterial;
    private RectTransform scrollContentMaterial;
    //详细信息
    private TextMeshProUGUI materialNameText;
    private TextMeshProUGUI materialDescText;
    private Image materialIcon;
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
    #region 素材对象池
    private List<PackageCell> cellPoolMaterial = new List<PackageCell>();
    private List<PackageCell> activeCellsMaterial = new List<PackageCell>();
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
        var menuMaterialTrans = transform.Find("TopCenter/Menus/Material");
        if (menuMaterialTrans != null)
        {
            menuMaterialBtn = menuMaterialTrans.GetComponent<Button>();
            var select = menuMaterialTrans.Find("Select");
            selectMaterial = select.GetComponent<Image>();
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
        //素材区
        centerMaterial = transform.Find("Center-Material");
        if (centerMaterial != null)
        {
            var scrollView = centerMaterial.Find("Scroll View");
            if (scrollView != null)
            {
                scrollRectMaterial = scrollView.GetComponent<ScrollRect>();
                scrollContentMaterial = scrollRectMaterial?.content;
            }
            var Center = centerMaterial.Find("DetailPanel-Material/Center");
            if (Center != null)
            {
                var nameMat = Center.Find("name");
                if (nameMat != null)
                {
                    materialNameText = nameMat.GetComponent<TextMeshProUGUI>();
                }
                var iconMat = Center.Find("icon");
                if (iconMat != null)
                {
                    materialIcon = iconMat.GetComponent<Image>();
                }
            }
            var Bottom = centerMaterial.Find("DetailPanel-Material/Bottom");
            if (Bottom != null)
            {
                var description = Bottom.Find("description");
                if (description == null)
                {
                    description = Bottom.Find("Description");
                }
                if (description != null)
                {
                    materialDescText = description.GetComponent<TextMeshProUGUI>();
                }
            }
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
            // 把原来的"详情"按钮改成"食用"按钮(改标签文本)
            var label = detailBtnTrans.Find("Text");
            if (label != null)
            {
                TextMeshProUGUI labelTmp = label.GetComponent<TextMeshProUGUI>();
                if (labelTmp != null)
                {
                    labelTmp.text = "食用";
                }
                else
                {
                    Text labelLegacy = label.GetComponent<Text>();
                    if (labelLegacy != null) labelLegacy.text = "食用";
                }
            }
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
        if (menuMaterialBtn != null)
        {
            menuMaterialBtn.onClick.AddListener(() => OnClickTab(GameManager.GameConst.PackageTypeMaterial));
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
            detailBtn.onClick.AddListener(OnEatFood);
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
        bool isFood = (tabType == GameManager.GameConst.PackageTypeFood);

        //显示和隐藏对应的Center
        if (centerWeapon != null) centerWeapon.gameObject.SetActive(isWeapon);
        if (centerFood != null) centerFood.gameObject.SetActive(isFood);
        if (centerMaterial != null) centerMaterial.gameObject.SetActive(!isWeapon && !isFood);

        //更新Tab名称
        if (tabNameText != null)
        {
            tabNameText.text = isWeapon ? "武器" : (isFood ? "食物" : "素材");
        }
        //更新Tab图标
        if (selectWeapon != null) selectWeapon.gameObject.SetActive(isWeapon);
        if (selectFood != null) selectFood.gameObject.SetActive(isFood);
        if (selectMaterial != null) selectMaterial.gameObject.SetActive(!isWeapon && !isFood);

        // 切换标签页后刷新底部"食用"按钮显隐
        RefreshBottomMenuState();
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
        bool isFood = (tabType == GameManager.GameConst.PackageTypeFood);

        RectTransform content = isWeapon ? scrollContentWeapon : (isFood ? scrollContentFood : scrollContentMaterial);
        if (content == null || PackageUIItemPrefab == null)
        {
            return;
        }

        //获取对应类型数据
        List<ItemBase> dataList;
        if (isWeapon)
        {
            dataList = GameManager.INSTANCE.GetWeaponItems();
        }
        else if (isFood)
        {
            dataList = GameManager.INSTANCE.GetFoodItems();
        }
        else
        {
            dataList = GameManager.INSTANCE.GetMaterialItems();
        }

        //选择对应的池子
        List<PackageCell> pool = isWeapon ? cellPoolWeapon : (isFood ? cellPoolFood : cellPoolMaterial);
        List<PackageCell> activeList = isWeapon ? activeCellsWeapon : (isFood ? activeCellsFood : activeCellsMaterial);

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
        ScrollRect scrollRect = isWeapon ? scrollRectWeapon : (isFood ? scrollRectFood : scrollRectMaterial);
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
            RefreshBottomMenuState();
            return;
        }

        ItemBase item = InventoryManager.INSTANCE.GetItem(chooseUID);
        if (item == null)
        {
            ClearDetailPanel();
            RefreshBottomMenuState();
            return;
        }

        // 更新底部"食用"按钮显隐(只有选中食物时才显示)
        RefreshBottomMenuState();

        if (_currentTabType == GameManager.GameConst.PackageTypeWeapon)
        {
            var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(item.itemID);
            if (weapon == null)
            {
                return;
            }
            if (weaponNameText != null) weaponNameText.text = weapon.weaponName;
            if (weaponDescText != null) weaponDescText.text = weapon.weaponDescription;
            Sprite icon = InventoryManager.INSTANCE.GetIcon(item.itemID);
            if (weaponIcon != null) weaponIcon.sprite = icon;
            RefreshDetailStars(weaponStars, weapon.Stars);
        }
        else if (_currentTabType == GameManager.GameConst.PackageTypeFood)
        {
            var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(item.itemID);
            if (food == null)
            {
                return;
            }
            if (foodNameText != null) foodNameText.text = food.foodName;
            if (foodDescText != null) foodDescText.text = BuildFoodDescription(food);
            Sprite icon = InventoryManager.INSTANCE.GetIcon(item.itemID);
            if (foodIcon != null) foodIcon.sprite = icon;
        }
        else
        {
            var material = InventoryManager.INSTANCE.materialData?.GetMaterialByID(item.itemID);
            if (material == null)
            {
                return;
            }
            if (materialNameText != null) materialNameText.text = material.materialName;
            if (materialDescText != null) materialDescText.text = material.description;
            Sprite icon = InventoryManager.INSTANCE.GetIcon(item.itemID);
            if (materialIcon != null) materialIcon.sprite = icon;
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

        if (materialNameText != null)
        {
            materialNameText.text = "";
        }
        if (materialDescText != null)
        {
            materialDescText.text = "";
        }
        if (materialIcon != null)
        {
            materialIcon.sprite = null;
        }
    }

    /// <summary>
    /// 删除状态刷新
    /// </summary>
    public void RefreshDeletePanel()
    {
        //只刷新当前激活的标签页
        List<PackageCell> activeList;
        int totalCount;
        if (_currentTabType == GameManager.GameConst.PackageTypeWeapon)
        {
            activeList = activeCellsWeapon;
            totalCount = InventoryManager.INSTANCE._weaponIds.Count;
        }
        else if (_currentTabType == GameManager.GameConst.PackageTypeFood)
        {
            activeList = activeCellsFood;
            totalCount = InventoryManager.INSTANCE._foodIds.Count;
        }
        else
        {
            activeList = activeCellsMaterial;
            totalCount = InventoryManager.INSTANCE._materialIds.Count;
        }

        foreach (var cell in activeList)
        {
            if (cell != null)
            {
                cell.RefreshDeleteState();
            }
        }
        // 更新提示文本（动态总数）
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
        int currentCount = (InventoryManager.INSTANCE._weaponIds.Count + InventoryManager.INSTANCE._foodIds.Count + InventoryManager.INSTANCE._materialIds.Count);
        //最大容量520
        const int maxCapacity = 520;
        capacityText.text = $"{currentCount}/{maxCapacity}";
    }

    #endregion

    #region 根据 UID 在当前激活的格子中查找对应的PackageCell
    public PackageCell FindCellByUID(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return null;

        List<PackageCell> activeList;
        if (_currentTabType == GameManager.GameConst.PackageTypeWeapon)
        {
            activeList = activeCellsWeapon;
        }
        else if (_currentTabType == GameManager.GameConst.PackageTypeFood)
        {
            activeList = activeCellsFood;
        }
        else
        {
            activeList = activeCellsMaterial;
        }

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

    /// <summary>
    /// 食用当前选中的食物
    /// </summary>
    private void OnEatFood()
    {
        // 只在食物标签页生效
        if (_currentTabType != GameManager.GameConst.PackageTypeFood) return;
        if (string.IsNullOrEmpty(chooseUID)) return;

        ItemBase item = InventoryManager.INSTANCE.GetItem(chooseUID);
        if (item == null || !(item is FoodItem foodItem)) return;
        Food food = InventoryManager.INSTANCE.foodData?.GetFoodByID(item.itemID);
        if (food == null) return;

        // 应用回血效果
        if (food.healType == FoodHealType.OverTime && food.tickInterval > 0f)
        {
            float duration = food.overTimeDuration > 0f ? food.overTimeDuration : 60f;
            GameManager.INSTANCE.HealActiveRoleOverTime(duration, food.tickInterval, food.healAmount);
            int ticks = Mathf.CeilToInt(duration / food.tickInterval);
            ToastUI.ShowMessage($"食用 {food.foodName}：{duration:F0} 秒内每 {food.tickInterval:F0} 秒恢复 {food.healAmount} 生命", new Color(0.4f, 1f, 0.5f));
        }
        else
        {
            GameManager.INSTANCE.HealActiveRole(food.healAmount);
            ToastUI.ShowMessage($"食用 {food.foodName}：恢复 {food.healAmount} 生命", new Color(0.4f, 1f, 0.5f));
        }

        // 消耗 1 个食物
        InventoryManager.INSTANCE.ConsumeFood(item.instanceID, 1);

        // 刷新列表和详情
        RefreshScroll(_currentTabType);
        if (InventoryManager.INSTANCE.GetItem(chooseUID) == null)
        {
            chooseUID = null;
        }
        else
        {
            RefreshDetail();
        }
        UpdateCapacityDisplay();
    }

    /// <summary>
    /// 底部"食用"按钮显隐: 只有选中食物时才显示
    /// </summary>
    private void RefreshBottomMenuState()
    {
        bool canEat = _currentTabType == GameManager.GameConst.PackageTypeFood
                      && !string.IsNullOrEmpty(chooseUID)
                      && InventoryManager.INSTANCE.GetItem(chooseUID) is FoodItem;
        if (detailBtn != null)
        {
            detailBtn.gameObject.SetActive(canEat);
        }
    }

    /// <summary>
    /// 生成食物描述: 原描述 + 自动追加回血说明
    /// </summary>
    private string BuildFoodDescription(Food food)
    {
        string desc = string.IsNullOrEmpty(food.description) ? "" : food.description.Trim();
        if (food.healType == FoodHealType.OverTime && food.tickInterval > 0f)
        {
            float duration = food.overTimeDuration > 0f ? food.overTimeDuration : 60f;
            desc += $"\n\n{duration:F0} 秒内每 {food.tickInterval:F0} 秒恢复 {food.healAmount} 点生命";
        }
        else
        {
            desc += $"\n\n立即恢复 {food.healAmount} 点生命";
        }
        return desc;
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
