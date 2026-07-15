using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//强制让一个自定义的 ScriptableObject 资源以二进制格式进行序列化（保存），而忽略项目整体的序列化设置
[PreferBinarySerialization]
[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Game/PlayerData")]
public class PlayerData_SO : ScriptableObject
{
    public List<Character> characterList;
    //缓存字典，用于快速查找角色信息
    private Dictionary<int, Character> _cacheDict;

    /// <summary>
    /// 根据ID获取角色信息
    /// </summary>
    /// <param name="id">角色ID</param>
    /// <returns></returns>
    public Character GetCharacterByID(int id)
    {
        if (_cacheDict == null || _cacheDict.Count == 0)
        {
            _cacheDict = new Dictionary<int, Character>();
            foreach (var character in characterList)
            {
                if (!_cacheDict.ContainsKey(character.characterID))
                {
                    _cacheDict.Add(character.characterID, character);
                }
            }
        }
        _cacheDict.TryGetValue(id, out Character result);
        return result;
    }
}
