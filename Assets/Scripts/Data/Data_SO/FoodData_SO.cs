using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodData", menuName = "Game/FoodData")]
public class FoodData_SO : ScriptableObject
{
    public List<Food> foofList;
    private Dictionary<int, Food> _cacheDict;

    public Food GetFoodByID(int id)
    {
        if (_cacheDict == null || _cacheDict.Count == 0)
        {
            _cacheDict = new Dictionary<int, Food>();
            foreach (var f in foofList)
            {
                if (!_cacheDict.ContainsKey(f.foodID))
                {
                    _cacheDict.Add(f.foodID, f);
                }
            }
        }
        _cacheDict.TryGetValue(id, out Food result);
        return result;
    }
}
