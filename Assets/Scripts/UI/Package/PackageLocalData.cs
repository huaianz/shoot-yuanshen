// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class PackageLocalData : SingleMonoBase<PackageLocalData>
// {
//     public List<PackageLocalItem> items;

//     /// <summary>
//     /// 保存本地数据
//     /// </summary>
//     public void SavePackage()
//     {
//         string inventoryJson = JsonUtility.ToJson(this);
//         PlayerPrefs.SetString("PackageLocalData", inventoryJson);
//         PlayerPrefs.Save();
//     }

//     /// <summary>
//     /// 加载本地数据
//     /// </summary>
//     /// <returns></returns>
//     public List<PackageLocalItem> LoadPackage()
//     {
//         if (items != null)
//         {
//             return items;
//         }
//         if (PlayerPrefs.HasKey("PackageLocalData"))
//         {
//             string inventoryJson = PlayerPrefs.GetString("PackageLocalData");
//             PackageLocalData inventoryData = JsonUtility.FromJson<PackageLocalData>(inventoryJson);
//             return inventoryData.items;
//         }
//         else
//         {
//             items = new List<PackageLocalItem>();
//             return items;
//         }

//     }
// }
