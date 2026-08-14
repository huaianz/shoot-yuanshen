using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 单个物品
/// </summary>
public class PackageCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region 物品UI子对象
    private Image iconImage;
    private Image newMark;
    private GameObject deleteSelectMark;
    private Transform UISelect;
    //存放星级的容器
    private Transform starContainer;
    //存放星级的GameObject数组
    private GameObject[] starObjects;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI countText; // 右下角数量

    #endregion

    #region 动画相关的
    private Animator selectAnimator;
    private Animator mouseOverAnimator;
    #endregion

    #region 数据相关
    //当前的格子数据
    public ItemBase itemData;
    //对应的物品模版
    private object templateData;
    private PackagePanel uiParent;
    #endregion

    private void Awake()
    {
        //存放所有UI组件
        CacheUIReferences();
        //缓存星级子物体
        CacheStarChildren();
        //确保数量文本存在(预制体没有就运行时创建)
        EnsureCountText();
        //初始化隐藏动画和删除标记
        if (selectAnimator != null)
        {
            selectAnimator.gameObject.SetActive(false);
        }
        if (mouseOverAnimator != null)
        {
            mouseOverAnimator.gameObject.SetActive(false);
        }
        if (deleteSelectMark != null)
        {
            deleteSelectMark.SetActive(false);
        }
        if (UISelect != null)
        {
            UISelect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 缓存UI组件
    /// </summary>
    private void CacheUIReferences()
    {
        //图标
        var iconTrans = transform.Find("Top/icon");
        if (iconTrans != null)
        {
            iconImage = iconTrans.GetComponent<Image>();
        }
        //new
        var newTrans = transform.Find("Top/New");
        if (newTrans != null)
        {
            newMark = newTrans.GetComponent<Image>();
        }
        //选中
        UISelect = transform.Find("Select");
        //星级容器
        starContainer = transform.Find("Bottom/Stars");
        //删除选择框
        var deleteTrans = transform.Find("DeleteSelect");
        if (deleteTrans != null)
        {
            deleteSelectMark = deleteTrans.gameObject;
        }

        //动画组件
        var selectAniTrans = transform.Find("SelectAni");
        if (selectAniTrans != null)
        {
            selectAnimator = selectAniTrans.GetComponent<Animator>();
        }

        var mouseOverAniTrans = transform.Find("MouseOverAni");
        if (mouseOverAniTrans != null)
        {
            mouseOverAnimator = mouseOverAniTrans.GetComponent<Animator>();
        }
        var Text = transform.Find("Bottom/nameText");
        if (Text != null)
        {
            nameText = Text.GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// 确保数量文本存在: 预制体有 Top/Count 就用它, 没有就在图标右下角创建一个
    /// </summary>
    private void EnsureCountText()
    {
        var countTrans = transform.Find("Bottom/CountText");
        if (countTrans != null)
        {
            countText = countTrans.GetComponent<TextMeshProUGUI>();
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(false); // 默认隐藏, Refresh时按物品类型显示
        }
    }

    /// <summary>
    /// 缓存星星子物体
    /// </summary>
    private void CacheStarChildren()
    {
        if (starContainer == null)
        {
            return;
        }
        int count = starContainer.childCount;
        starObjects = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            var child = starContainer.GetChild(i);
            if (child != null)
            {
                starObjects[i] = child.gameObject;
            }
        }
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="uiPrent"></param>
    public void Refresh(ItemBase itemData, PackagePanel uiPrent)
    {
        //重置选中状态
        deleteSelect();
        this.itemData = itemData;
        this.uiParent = uiPrent;

        //根据类型获取模版数据
        if (itemData is WeaponItem weapon)
        {
            templateData = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weapon.itemID);
        }
        else if (itemData is FoodItem food)
        {
            templateData = InventoryManager.INSTANCE.foodData?.GetFoodByID(food.itemID);
        }
        else
        {
            templateData = null;
        }

        if (newMark != null)
        {
            newMark.gameObject.SetActive(itemData.isNew);
        }

        Sprite icon = InventoryManager.INSTANCE.GetIcon(itemData.itemID);
        if (iconImage != null)
        {
            iconImage.sprite = icon;
        }
        if (nameText != null)
        {
            nameText.text = InventoryManager.INSTANCE.GetItemName(itemData.itemID);
        }
        //数量显示(食物/素材有数量, 武器没有)
        if (countText != null)
        {
            if (itemData is FoodItem food)
            {
                countText.gameObject.SetActive(true);
                countText.text = food.count.ToString();
            }
            else if (itemData is MaterialItem material)
            {
                countText.gameObject.SetActive(true);
                countText.text = material.count.ToString();
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }
        RefreshStars();
    }

    /// <summary>
    /// 刷新星级
    /// </summary>
    public void RefreshStars()
    {
        if (starObjects == null || starObjects.Length == 0)
        {
            return;
        }
        //先获取星级数值
        int starCount = 0;
        if (templateData is Weapon weapon)
        {
            starCount = weapon.Stars;
        }
        else if (templateData is Food food)
        {
            //DOTO:食物模版中暂时没有添加星级
        }

        //用缓存数组一次性设置
        for (int i = 0; i < starObjects.Length; i++)
        {
            if (starObjects[i] != null)
            {
                starObjects[i].SetActive(i < starCount);
            }
        }
    }



    /// <summary>
    /// 刷新删除状态模式下的选中状态
    /// </summary>
    public void RefreshDeleteState()
    {
        if (uiParent == null || deleteSelectMark == null)
        {
            return;
        }
        bool shouldShow = (uiParent.currentMode == PackageMode.delete) && uiParent.deleteChooseUid.Contains(itemData.instanceID);
        deleteSelectMark.SetActive(shouldShow);
    }

    /// <summary>
    /// 选中
    /// </summary>
    public void Select()
    {
        if (UISelect != null)
        {
            UISelect.gameObject.SetActive(true);
            if (selectAnimator != null)
            {
                selectAnimator.gameObject.SetActive(true);
                selectAnimator.SetTrigger("In");
            }
        }
    }

    /// <summary>
    /// 取消选中
    /// </summary>
    private void deleteSelect()
    {
        if (UISelect != null)
        {
            UISelect.gameObject.SetActive(false);
            if (selectAnimator != null)
            {
                selectAnimator.gameObject.SetActive(false);
            }
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiParent == null) return; // 安全: 没绑定背包面板的格子不响应点击
        if (uiParent.currentMode == PackageMode.delete)
        {
            uiParent.AddChooseDeleteUid(itemData.instanceID);
            return;
        }
        string currentUID = uiParent.chooseUID;
        if (currentUID == itemData.instanceID)
        {
            deleteSelect();
            uiParent.chooseUID = null;
            return;
        }
        if (!string.IsNullOrEmpty(currentUID))
        {
            PackageCell oldCell = uiParent.FindCellByUID(currentUID);
            if (oldCell != null)
            {
                oldCell.deleteSelect();
            }
        }
        //选中当前物品时，消除新的标志
        if (itemData.isNew)
        {
            InventoryManager.INSTANCE.MarkAsViewed(itemData.instanceID);
            //刷新当前格子显示
            if (newMark != null)
            {
                newMark.gameObject.SetActive(false);
            }
            itemData.isNew = false;
        }
        //触发选择动画
        Select();
        uiParent.chooseUID = itemData.instanceID;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mouseOverAnimator != null)
        {
            mouseOverAnimator.gameObject.SetActive(true);
            mouseOverAnimator.SetTrigger("In");
        }
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        if (mouseOverAnimator != null)
        {
            mouseOverAnimator.SetTrigger("Out");
        }
    }

    /// <summary>
    /// 关动画
    /// </summary>
    public void OnSelectAniInCb()
    {
        if (selectAnimator != null)
        {
            selectAnimator.gameObject.SetActive(false);
        }
    }

    public void OnMouseOverAniOutCb()
    {
        if (mouseOverAnimator != null)
        {
            mouseOverAnimator.gameObject.SetActive(false);
        }
    }
}
