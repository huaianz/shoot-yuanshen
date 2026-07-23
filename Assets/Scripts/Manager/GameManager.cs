using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingleMonoBase<GameManager>
{
    public PlayerModel[] playerModels;

    private WeaponData_SO weaponData;
    private FoodData_SO foodData;

    /// <summary>
    /// 批量删除物品
    /// </summary>
    /// <param name="uids"></param>
    public void DeletePackageItem(List<string> uids)
    {
        foreach (string uid in uids)
        {
            //通过uid批量删除物品
            DeletePackageItem(uid, false);
        }
        PackageLocalData.INSTANCE.SavePackage();
    }

    /// <summary>
    /// 删除单个物品
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="isSave"></param>
    public void DeletePackageItem(string uid, bool isSave = true)
    {
        //通过uid查找本地物品
        PackageLocalItem packageLocalItem = GetPackageLocalItemByUid(uid);
        if (packageLocalItem == null)
        {
            return;
        }
        PackageLocalData.INSTANCE.items.Remove(packageLocalItem);
        if (isSave)
        {
            PackageLocalData.INSTANCE.SavePackage();
        }
    }

    /// <summary>
    /// 返回物品的本地配置表
    /// </summary>
    /// <returns></returns>
    public GetPackageData()
    {
        if (packageData == null)
        {
            packageData = Resources.Load<PackageData_SO>("DataSO/PackageData");
        }
        return packageData;
    }

    /// <summary>
    /// 根据物品类型筛选配置表数据
    /// </summary>
    /// <param name="type">武器为1，食物为2</param>
    /// <returns></returns>
    public List<PackageItem> GetPackageItemByType(int type)
    {
        List<PackageItem> packageItems = new List<PackageItem>();
        foreach (PackageItem packageItem in GetPackageData().packageList)
        {
            if (packageItem.type == type)
            {
                packageItems.Add(packageItem);
            }
        }
        return packageItems;
    }

    /// <summary>
    /// 检查武器是否是新武器
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool CheckWeaponIsNew(int id)
    {
        foreach (PackageLocalItem packageLocalItem in GetPackageLocalData())
        {
            if (packageLocalItem.id == id)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 获取本地物品数据并返回
    /// </summary>
    /// <returns></returns>
    public List<PackageLocalItem> GetPackageLocalData()
    {
        return PackageLocalData.INSTANCE.LoadPackage();
    }

    /// <summary>
    /// 根据id获取物品
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public PackageItem GetPackageItemById(int id)
    {
        List<PackageItem> packageDataList = GetPackageData().packageList;
        foreach (PackageItem item in packageDataList)
        {
            if (item.id == id)
            {
                return item;
            }
        }
        return null;
    }


    /// <summary>
    /// 根据uid获取本地物品
    /// </summary>
    /// <param name="uid"></param>
    /// <returns></returns>
    public PackageLocalItem GetPackageLocalItemByUid(string uid)
    {
        List<PackageLocalItem> packageDataList = GetPackageLocalData();
        foreach (PackageLocalItem item in packageDataList)
        {
            if (item.uid == uid)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取排序后的本地物品
    /// </summary>
    /// <returns></returns>
    public List<PackageLocalItem> GetSortPackageLocalData()
    {
        List<PackageLocalItem> localItems = PackageLocalData.INSTANCE.LoadPackage();
        localItems.Sort(new PackageItemComparer());
        return localItems;
    }

    /// <summary>
    /// 实现IComparer<PackageLocalItem> 接口，用于排序
    /// </summary>
    public class PackageItemComparer : IComparer<PackageLocalItem>
    {
        public int Compare(PackageLocalItem a, PackageLocalItem b)
        {
            PackageItem x = GameManager.INSTANCE.GetPackageItemById(a.id);
            PackageItem y = GameManager.INSTANCE.GetPackageItemById(b.id);

            int starComparison = y.star.CompareTo(x.star);
            if (starComparison == 0)
            {
                int idComparison = y.id.CompareTo(x.id);
                if (idComparison == 0)
                {
                    return b.level.CompareTo(a.level);
                }
                return idComparison;
            }
            return starComparison;
        }
    }

    public class GameConst
    {
        public const int PackageTypeWeapon = 1;
        public const int PackageTypeFood = 2;
    }

}
