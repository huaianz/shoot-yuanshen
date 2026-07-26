using System.Collections;
using System.Collections.Generic;
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
    private GameObject newMark;
    private GameObject deleteSelectMark;
    //存放星级的容器
    private Transform starContainer;
    //存放星级的GameObject数组
    private GameObject[] starObjects;

    #endregion

    #region 动画相关的
    private Animator selectAnimator;
    private Animator mouseOverAnimator;
    #endregion

    #region 数据相关
    //当前的格子数据
    private ItemBase itemData;
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
            newMark = newTrans.gameObject;
        }
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
            newMark.SetActive(itemData.isNew);
        }

        Sprite icon = InventoryManager.INSTANCE.GetIcon(itemData.itemID);
        if (iconImage != null)
        {
            iconImage.sprite = icon;
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


    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiParent.currentMode == PackageMode.delete)
        {
            uiParent.AddChooseDeleteUid(itemData.instanceID);
            return;
        }
        //如果已经被选择，则返回
        if (uiParent.chooseUID == itemData.instanceID)
        {
            return;
        }
        //触发选择动画
        uiParent.chooseUID = itemData.instanceID;
        if (selectAnimator != null)
        {
            selectAnimator.gameObject.SetActive(true);
            selectAnimator.SetTrigger("In");
        }

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
