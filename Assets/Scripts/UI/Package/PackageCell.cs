using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
/// <summary>
/// 单个物品
/// </summary>
public class PackageCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region 物品UI子对象
    private Transform UIIcon;
    private Transform UIHead;
    private Transform UINew;
    private Transform UISelect;
    private Transform UIStars;
    private Transform UIDeleteSelect;
    #endregion

    #region 动画相关的
    private Transform UISelectAni;
    private Transform UIMouseOverAni;
    #endregion

    #region 数据相关
    private PackageLocalItem packageLocalItem;
    private PackageItem packageItem;
    private PackagePanel uiParent;
    #endregion

    private void Awake()
    {
        //找到UI子对象
        InitUIName();
    }

    /// <summary>
    /// 根据相对路径查找一些UI子物体
    /// </summary>
    private void InitUIName()
    {
        UIIcon = transform.Find("Top/icon");
        UIHead = transform.Find("Top/Head");
        UINew = transform.Find("Top/New");
        UIStars = transform.Find("Bottom/Stars");
        UISelect = transform.Find("Select");
        UIDeleteSelect = transform.Find("DeleteSelect");
        UIMouseOverAni = transform.Find("MouseOverAni");
        UISelectAni = transform.Find("SelectAni");
        UIDeleteSelect.gameObject.SetActive(false);
        UIMouseOverAni.gameObject.SetActive(false);
        UISelectAni.gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新格子显示
    /// </summary>
    /// <param name="packageLocalData"></param>
    /// <param name="uiParent"></param>
    public void Refresh(PackageLocalItem packageLocalItem, PackagePanel uiParent)
    {
        //数据初始化，本地和列表的数据
        this.packageLocalItem = packageLocalItem;
        this.packageItem = GameManager.INSTANCE.GetPackageItemById(packageLocalItem.id);
        this.uiParent = uiParent;
        UINew.gameObject.SetActive(this.packageLocalItem.isNew);
        //获取物品的图片
        Texture2D t = (Texture2D)Resources.Load(this.packageItem.imagePath);
        Sprite temp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
        UIIcon.GetComponent<Image>().sprite = temp;
        //刷新星级
        RefreshStars();
    }

    /// <summary>
    /// 刷新星级
    /// </summary>
    public void RefreshStars()
    {
        for (int i = 0; i < UIStars.childCount; i++)
        {
            Transform star = UIStars.GetChild(i);
            if (this.packageItem.star > i)
            {
                star.gameObject.SetActive(true);
            }
            else
            {
                star.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 刷新删除状态模式下的选中状态
    /// </summary>
    public void RefreshDeleteState()
    {
        if (this.uiParent.currentMode == PackageMode.delete)
        {
            //将选中的加入到删除列表
            this.uiParent.AddChooseDeleteUid(this.packageLocalItem.uid);
        }
        else
        {
            this.UIDeleteSelect.gameObject.SetActive(false);
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (this.uiParent.currentMode == PackageMode.delete)
        {
            this.uiParent.AddChooseDeleteUid(this.packageLocalItem.uid);
        }
        if (this.uiParent.chooseUID == this.packageLocalItem.uid)
        {
            return;
        }
        //触发动画
        this.uiParent.chooseUID = this.packageLocalItem.uid;
        UISelectAni.gameObject.SetActive(true);
        UISelectAni.GetComponent<Animator>().SetTrigger("In");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIMouseOverAni.gameObject.SetActive(true);
        UIMouseOverAni.GetComponent<Animator>().SetTrigger("In");
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        UIMouseOverAni.GetComponent<Animator>().SetTrigger("Out");
    }

    /// <summary>
    /// 关动画
    /// </summary>
    public void OnSelectAniInCb()
    {
        UISelectAni.gameObject.SetActive(false);
    }

    public void OnMouseOverAniOutCb()
    {
        UIMouseOverAni.gameObject.SetActive(false);
    }
}
