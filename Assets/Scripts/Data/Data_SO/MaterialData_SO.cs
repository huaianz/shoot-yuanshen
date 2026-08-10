using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterialData", menuName = "Game/MaterialData")]
public class MaterialData_SO : ScriptableObject
{
    public List<MaterialData> materialList;

    private Dictionary<int, MaterialData> _cacheDict;

    public MaterialData GetMaterialByID(int id)
    {
        if (_cacheDict == null || _cacheDict.Count == 0)
        {
            _cacheDict = new Dictionary<int, MaterialData>();
            foreach (var m in materialList)
            {
                if (!_cacheDict.ContainsKey(m.materialID))
                {
                    _cacheDict.Add(m.materialID, m);
                }
            }
        }
        _cacheDict.TryGetValue(id, out MaterialData result);
        return result;
    }
}