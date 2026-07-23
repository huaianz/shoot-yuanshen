using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[PreferBinarySerialization]//将资产编译成二进制格式
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/WeaponData")]
public class WeaponData_SO : ScriptableObject
{
    public List<Weapon> weaponList;

    private Dictionary<int, Weapon> _cacheDict;

    public Weapon GetWeaponByID(int id)
    {
        if (_cacheDict == null || _cacheDict.Count == 0)
        {
            _cacheDict = new Dictionary<int, Weapon>();
            foreach (var weapon in weaponList)
            {
                if (!_cacheDict.ContainsKey(weapon.weaponID))
                {
                    _cacheDict.Add(weapon.weaponID, weapon);
                }
            }
        }
        _cacheDict.TryGetValue(id, out Weapon result);
        return result;
    }
}
